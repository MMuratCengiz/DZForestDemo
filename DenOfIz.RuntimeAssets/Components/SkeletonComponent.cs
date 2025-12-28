using System.Numerics;

namespace RuntimeAssets.Components;

public struct SkeletonComponent(RuntimeSkeletonHandle skeleton)
{
    public RuntimeSkeletonHandle Skeleton = skeleton;

    public bool IsValid => Skeleton.IsValid;
}

public struct AnimatorComponent(RuntimeSkeletonHandle skeleton)
{
    public RuntimeSkeletonHandle Skeleton = skeleton;
    public RuntimeAnimationHandle CurrentAnimation = RuntimeAnimationHandle.Invalid;
    public float PlaybackSpeed = 1.0f;
    public float CurrentTime = 0.0f;
    public bool IsPlaying = false;
    public bool Loop = true;

    public bool IsValid => Skeleton.IsValid;

    public void Play(RuntimeAnimationHandle animation)
    {
        CurrentAnimation = animation;
        CurrentTime = 0.0f;
        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        CurrentTime = 0.0f;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Resume()
    {
        IsPlaying = true;
    }
}

public sealed class BoneMatricesData
{
    public const int MaxBones = 128;

    public readonly Matrix4x4[] LocalTransforms;
    public readonly Matrix4x4[] ModelTransforms;
    public readonly Matrix4x4[] FinalBoneMatrices;
    public readonly Matrix4x4[] InverseBindMatrices;
    public readonly Matrix4x4 SkeletonRootTransform;
    public readonly int NumBones;
    public bool IsDirty = true;

    public BoneMatricesData(int numBones, IReadOnlyList<Matrix4x4>? inverseBindMatrices = null, Matrix4x4? skeletonRootTransform = null)
    {
        NumBones = Math.Min(numBones, MaxBones);
        SkeletonRootTransform = skeletonRootTransform ?? Matrix4x4.Identity;
        LocalTransforms = new Matrix4x4[MaxBones];
        ModelTransforms = new Matrix4x4[MaxBones];
        FinalBoneMatrices = new Matrix4x4[MaxBones];
        InverseBindMatrices = new Matrix4x4[MaxBones];

        for (var i = 0; i < MaxBones; i++)
        {
            LocalTransforms[i] = Matrix4x4.Identity;
            ModelTransforms[i] = Matrix4x4.Identity;
            FinalBoneMatrices[i] = Matrix4x4.Identity;
            InverseBindMatrices[i] = Matrix4x4.Identity;
        }

        if (inverseBindMatrices != null)
        {
            var count = Math.Min(inverseBindMatrices.Count, MaxBones);
            for (var i = 0; i < count; i++)
            {
                InverseBindMatrices[i] = inverseBindMatrices[i];
            }
        }
    }

    public void ComputeFinalMatrices()
    {
        for (var i = 0; i < NumBones; i++)
        {
            FinalBoneMatrices[i] = InverseBindMatrices[i] * ModelTransforms[i];
            FinalBoneMatrices[i] = Matrix4x4.Identity;
        }
        IsDirty = false;
    }
}

public struct BoneMatricesComponent(int numBones, IReadOnlyList<Matrix4x4>? inverseBindMatrices = null, Matrix4x4? skeletonRootTransform = null)
{
    public readonly BoneMatricesData Data = new(numBones, inverseBindMatrices, skeletonRootTransform);

    public readonly bool IsValid => Data != null;
}