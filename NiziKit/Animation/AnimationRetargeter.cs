using System.Numerics;
using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Assets;
using NiziKit.Core;

namespace NiziKit.Animation;

/// <summary>
/// Retargets animations from a source skeleton to a destination skeleton using the
/// WickedEngine-style algorithm. Computes per-bone relative transform matrices at setup
/// time, then applies them at runtime to convert source animation poses to destination space.
/// </summary>
public sealed class AnimationRetargeter : IDisposable
{
    private static readonly ILogger Logger = Log.Get<AnimationRetargeter>();

    private struct RetargetBoneData
    {
        public int SourceJointIndex;
        public int DestJointIndex;
        public Matrix4x4 DstRelativeMatrix;
        public Matrix4x4 SrcRelativeParentMatrix;
    }

    private Skeleton? _sourceSkeleton;
    private RetargetBoneData[] _bones = [];
    private int _boneCount;
    private int[] _processOrder = [];
    private Matrix4x4[] _destRestPose = [];
    private Float4x4Array.Pinned? _sourceModelTransforms;
    private OzzJointTransformArray.Pinned? _sourceLocalTransforms;
    private Float4x4Array.Pinned? _sourceBlendModelTransforms;
    private OzzJointTransformArray.Pinned? _sourceBlendLocalTransforms;

    public bool IsValid { get; private set; }
    public Skeleton? SourceSkeleton => _sourceSkeleton;

    /// <summary>
    /// Sets up retargeting from a source skeleton to a destination skeleton.
    /// </summary>
    /// <param name="sourceSkeleton">The skeleton that owns the animations to retarget.</param>
    /// <param name="destSkeleton">The skeleton that will receive the retargeted animation.</param>
    /// <param name="srcTPoseModelMatrices">T-pose model-space matrices for source skeleton joints.</param>
    /// <param name="dstTPoseModelMatrices">T-pose model-space matrices for destination skeleton joints.</param>
    public void Setup(
        Skeleton sourceSkeleton,
        Skeleton destSkeleton,
        Matrix4x4[] srcTPoseModelMatrices,
        Matrix4x4[] dstTPoseModelMatrices)
    {
        Dispose();
        _sourceSkeleton = sourceSkeleton;

        var srcHumanoidMap = HumanoidBoneMapper.MapSkeleton(sourceSkeleton.Joints);
        var dstHumanoidMap = HumanoidBoneMapper.MapSkeleton(destSkeleton.Joints);

        if (!HumanoidBoneMapper.IsValidHumanoid(srcHumanoidMap))
        {
            Logger.LogWarning("Source skeleton '{Name}' does not have required humanoid bones for retargeting",
                sourceSkeleton.Name);
            return;
        }

        if (!HumanoidBoneMapper.IsValidHumanoid(dstHumanoidMap))
        {
            Logger.LogWarning("Destination skeleton '{Name}' does not have required humanoid bones for retargeting",
                destSkeleton.Name);
            return;
        }

        var bones = new List<RetargetBoneData>();
        for (var humanoidBone = 0; humanoidBone < (int)HumanoidBone.Count; humanoidBone++)
        {
            var srcIdx = srcHumanoidMap[humanoidBone];
            var dstIdx = dstHumanoidMap[humanoidBone];
            if (srcIdx < 0 || dstIdx < 0)
            {
                continue;
            }

            if (srcIdx >= srcTPoseModelMatrices.Length || dstIdx >= dstTPoseModelMatrices.Length)
            {
                continue;
            }

            var srcWorld = srcTPoseModelMatrices[srcIdx];
            var dstWorld = dstTPoseModelMatrices[dstIdx];

            var srcParentIdx = sourceSkeleton.Joints[srcIdx].ParentIndex;
            var dstParentIdx = destSkeleton.Joints[dstIdx].ParentIndex;

            var srcParentWorld = srcParentIdx >= 0 && srcParentIdx < srcTPoseModelMatrices.Length
                ? srcTPoseModelMatrices[srcParentIdx]
                : Matrix4x4.Identity;
            var dstParentWorld = dstParentIdx >= 0 && dstParentIdx < dstTPoseModelMatrices.Length
                ? dstTPoseModelMatrices[dstParentIdx]
                : Matrix4x4.Identity;

            if (!Matrix4x4.Invert(srcWorld, out var inverseSrcWorld))
            {
                continue;
            }

            if (!Matrix4x4.Invert(dstParentWorld, out var inverseDstParentWorld))
            {
                continue;
            }

            bones.Add(new RetargetBoneData
            {
                SourceJointIndex = srcIdx,
                DestJointIndex = dstIdx,
                DstRelativeMatrix = dstWorld * inverseSrcWorld,
                SrcRelativeParentMatrix = srcParentWorld * inverseDstParentWorld,
            });
        }

        _bones = bones.ToArray();
        _boneCount = bones.Count;

        if (_boneCount == 0)
        {
            Logger.LogWarning("No matching humanoid bones found between source and destination skeletons");
            return;
        }

        _processOrder = new int[_boneCount];
        for (var i = 0; i < _boneCount; i++)
        {
            _processOrder[i] = i;
        }

        Array.Sort(_processOrder, (a, b) => _bones[a].DestJointIndex.CompareTo(_bones[b].DestJointIndex));

        _destRestPose = new Matrix4x4[destSkeleton.JointCount];
        Array.Copy(dstTPoseModelMatrices, _destRestPose,
            Math.Min(dstTPoseModelMatrices.Length, _destRestPose.Length));

        var srcJointCount = sourceSkeleton.JointCount;
        _sourceModelTransforms = Float4x4Array.Create(new Matrix4x4[srcJointCount]);
        _sourceLocalTransforms = OzzJointTransformArray.Create(new OzzJointTransform[srcJointCount]);
        _sourceBlendModelTransforms = Float4x4Array.Create(new Matrix4x4[srcJointCount]);
        _sourceBlendLocalTransforms = OzzJointTransformArray.Create(new OzzJointTransform[srcJointCount]);

        IsValid = true;

        Logger.LogInformation(
            "Retargeting setup: {BoneCount} matched bones between '{Src}' and '{Dst}'",
            _boneCount, sourceSkeleton.Name, destSkeleton.Name);
    }

    /// <summary>
    /// Samples a single animation on the source skeleton and writes retargeted model-space
    /// matrices into the destination output span.
    /// </summary>
    public void SampleAndRetarget(
        Assets.Animation anim,
        float normalizedTime,
        Span<Matrix4x4> destModelMatrices,
        IReadOnlyList<Joint> destJoints)
    {
        if (!IsValid || _sourceSkeleton == null)
        {
            return;
        }

        SampleSourceLocal(anim, normalizedTime, _sourceLocalTransforms!.Value);
        ConvertSourceToModel(_sourceLocalTransforms!.Value, _sourceModelTransforms!.Value);
        ApplyRetarget(_sourceModelTransforms!.Value.AsSpan(), destModelMatrices, destJoints);
    }

    /// <summary>
    /// Samples two animations for crossfade blending on the source skeleton and writes
    /// the blended, retargeted model-space matrices into the destination output span.
    /// </summary>
    public void SampleBlendedAndRetarget(
        Assets.Animation prevAnim,
        float prevNormalizedTime,
        Assets.Animation currAnim,
        float currNormalizedTime,
        float blendWeight,
        Span<Matrix4x4> destModelMatrices,
        IReadOnlyList<Joint> destJoints)
    {
        if (!IsValid || _sourceSkeleton == null)
        {
            return;
        }

        SampleSourceLocal(prevAnim, prevNormalizedTime, _sourceBlendLocalTransforms!.Value);
        ConvertSourceToModel(_sourceBlendLocalTransforms!.Value, _sourceBlendModelTransforms!.Value);

        SampleSourceLocal(currAnim, currNormalizedTime, _sourceLocalTransforms!.Value);
        ConvertSourceToModel(_sourceLocalTransforms!.Value, _sourceModelTransforms!.Value);

        var prevSpan = _sourceBlendModelTransforms!.Value.AsSpan();
        var currSpan = _sourceModelTransforms!.Value.AsSpan();
        var srcJointCount = _sourceSkeleton.JointCount;
        for (var i = 0; i < srcJointCount; i++)
        {
            currSpan[i] = Matrix4x4.Lerp(prevSpan[i], currSpan[i], blendWeight);
        }

        ApplyRetarget(currSpan, destModelMatrices, destJoints);
    }

    private void SampleSourceLocal(Assets.Animation anim, float normalizedTime, OzzJointTransformArray outLocal)
    {
        _sourceSkeleton!.OzzSkeleton.RunSamplingJobLocal(new SamplingJobLocalDesc
        {
            Context = anim.OzzContext,
            Ratio = Math.Clamp(normalizedTime, 0f, 1f),
            OutTransforms = outLocal
        });
    }

    private void ConvertSourceToModel(OzzJointTransformArray localTransforms, Float4x4Array outModel)
    {
        _sourceSkeleton!.OzzSkeleton.RunLocalToModelFromTRS(new LocalToModelFromTRSDesc
        {
            LocalTransforms = localTransforms,
            OutTransforms = outModel
        });
    }

    private void ApplyRetarget(
        ReadOnlySpan<Matrix4x4> srcModelMatrices,
        Span<Matrix4x4> destModelMatrices,
        IReadOnlyList<Joint> destJoints)
    {
        var destCount = Math.Min(destModelMatrices.Length, _destRestPose.Length);
        for (var i = 0; i < destCount; i++)
        {
            destModelMatrices[i] = _destRestPose[i];
        }

        for (var i = 0; i < _boneCount; i++)
        {
            ref var bone = ref _bones[_processOrder[i]];

            var srcModel = srcModelMatrices[bone.SourceJointIndex];
            var srcParentIdx = _sourceSkeleton!.Joints[bone.SourceJointIndex].ParentIndex;
            var srcParentModel = srcParentIdx >= 0
                ? srcModelMatrices[srcParentIdx]
                : Matrix4x4.Identity;

            if (!Matrix4x4.Invert(srcParentModel, out var inverseSrcParent))
            {
                continue;
            }

            var srcLocal = srcModel * inverseSrcParent;

            var dstLocal = bone.DstRelativeMatrix * srcLocal * bone.SrcRelativeParentMatrix;

            var dstParentIdx = destJoints[bone.DestJointIndex].ParentIndex;
            var dstParentModel = dstParentIdx >= 0
                ? destModelMatrices[dstParentIdx]
                : Matrix4x4.Identity;

            destModelMatrices[bone.DestJointIndex] = dstLocal * dstParentModel;
        }
    }

    public void Dispose()
    {
        _sourceModelTransforms?.Dispose();
        _sourceLocalTransforms?.Dispose();
        _sourceBlendModelTransforms?.Dispose();
        _sourceBlendLocalTransforms?.Dispose();
        _sourceModelTransforms = null;
        _sourceLocalTransforms = null;
        _sourceBlendModelTransforms = null;
        _sourceBlendLocalTransforms = null;
        _bones = [];
        _boneCount = 0;
        _processOrder = [];
        _destRestPose = [];
        _sourceSkeleton = null;
        IsValid = false;
    }
}
