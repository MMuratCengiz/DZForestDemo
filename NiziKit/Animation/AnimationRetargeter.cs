using System.Numerics;
using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Assets;
using NiziKit.Core;

namespace NiziKit.Animation;

/// <summary>
/// Retargets animations from a source skeleton to a destination skeleton.
/// Computes per-bone relative transform matrices at setup time,
/// then applies them at runtime to convert source animation poses to destination space.
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
    private Matrix4x4[] _destRestLocalTransforms = [];
    private bool[] _isMatchedBone = [];
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

        // Log all joint names for both skeletons
        Logger.LogInformation("Source skeleton '{Name}' joints ({Count}):", sourceSkeleton.Name,
            sourceSkeleton.JointCount);
        for (var i = 0; i < sourceSkeleton.Joints.Count; i++)
        {
            var j = sourceSkeleton.Joints[i];
            Logger.LogInformation("  src[{Index}] '{Name}' parent={Parent}", i, j.Name, j.ParentIndex);
        }

        Logger.LogInformation("Dest skeleton '{Name}' joints ({Count}):", destSkeleton.Name, destSkeleton.JointCount);
        for (var i = 0; i < destSkeleton.Joints.Count; i++)
        {
            var j = destSkeleton.Joints[i];
            Logger.LogInformation("  dst[{Index}] '{Name}' parent={Parent}", i, j.Name, j.ParentIndex);
        }

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

        // Log humanoid bone mapping for diagnostics
        for (var humanoidBone = 0; humanoidBone < (int)HumanoidBone.Count; humanoidBone++)
        {
            var boneName = (HumanoidBone)humanoidBone;
            var srcIdx = srcHumanoidMap[humanoidBone];
            var dstIdx = dstHumanoidMap[humanoidBone];
            var srcName = srcIdx >= 0 && srcIdx < sourceSkeleton.Joints.Count
                ? sourceSkeleton.Joints[srcIdx].Name
                : "MISSING";
            var dstName = dstIdx >= 0 && dstIdx < destSkeleton.Joints.Count
                ? destSkeleton.Joints[dstIdx].Name
                : "MISSING";

            if (srcIdx < 0 || dstIdx < 0)
            {
                Logger.LogWarning(
                    "Retarget bone {Bone}: src=[{SrcIdx}] '{SrcName}', dst=[{DstIdx}] '{DstName}' - SKIPPED",
                    boneName, srcIdx, srcName, dstIdx, dstName);
            }
            else
            {
                Logger.LogInformation("Retarget bone {Bone}: src=[{SrcIdx}] '{SrcName}' -> dst=[{DstIdx}] '{DstName}'",
                    boneName, srcIdx, srcName, dstIdx, dstName);
            }
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

        // Compute rest-pose LOCAL transforms for each dest joint so that unmatched bones
        // can follow their parent during animation instead of staying stuck at T-pose position.
        _destRestLocalTransforms = new Matrix4x4[destSkeleton.JointCount];
        for (var i = 0; i < destSkeleton.JointCount; i++)
        {
            var parentIdx = destSkeleton.Joints[i].ParentIndex;
            if (parentIdx >= 0 && parentIdx < dstTPoseModelMatrices.Length)
            {
                if (Matrix4x4.Invert(dstTPoseModelMatrices[parentIdx], out var invParent))
                {
                    _destRestLocalTransforms[i] = dstTPoseModelMatrices[i] * invParent;
                }
                else
                {
                    _destRestLocalTransforms[i] = dstTPoseModelMatrices[i];
                }
            }
            else
            {
                _destRestLocalTransforms[i] = dstTPoseModelMatrices[i];
            }
        }

        // Track which dest bones are matched so unmatched ones can inherit from parents.
        _isMatchedBone = new bool[destSkeleton.JointCount];
        for (var i = 0; i < _boneCount; i++)
        {
            _isMatchedBone[_bones[i].DestJointIndex] = true;
        }

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

        // Retarget matched humanoid bones
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

        // Propagate retargeted parent transforms to unmatched child bones.
        // Without this, unmatched bones (e.g. extra finger phalanges, thumb distal)
        // would stay stuck at their T-pose model-space position while the hand moves.
        // Joint indices are parent-before-child so forward iteration is safe.
        for (var i = 0; i < destCount; i++)
        {
            if (_isMatchedBone.Length > i && _isMatchedBone[i])
            {
                continue;
            }

            var parentIdx = destJoints[i].ParentIndex;
            if (parentIdx >= 0 && parentIdx < destCount &&
                _destRestLocalTransforms.Length > i)
            {
                destModelMatrices[i] = _destRestLocalTransforms[i] * destModelMatrices[parentIdx];
            }
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
        _destRestLocalTransforms = [];
        _isMatchedBone = [];
        _sourceSkeleton = null;
        IsValid = false;
    }
}
