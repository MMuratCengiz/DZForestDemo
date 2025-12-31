using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using RuntimeAssets.Components;

namespace RuntimeAssets;

public sealed class AnimationManager : IDisposable
{
    private readonly AnimationResource _resource = new();
    private readonly List<AnimatorInstance> _instances = new();
    private bool _disposed;

    public RuntimeSkeletonHandle LoadSkeleton(string ozzSkeletonPath) => _resource.LoadSkeleton(ozzSkeletonPath);
    public RuntimeAnimationHandle LoadAnimation(RuntimeSkeletonHandle skeleton, string ozzAnimationPath) => _resource.LoadAnimation(skeleton, ozzAnimationPath);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSkeleton(RuntimeSkeletonHandle handle, out RuntimeSkeleton skeleton) => _resource.TryGetSkeleton(handle, out skeleton);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetAnimation(RuntimeAnimationHandle handle, out RuntimeAnimationClip clip) => _resource.TryGetAnimation(handle, out clip);

    public AnimatorInstance CreateAnimator(RuntimeSkeletonHandle skeleton, Matrix4x4[]? inverseBindMatrices = null, Matrix4x4 skeletonRootTransform = default)
    {
        if (!_resource.TryGetSkeleton(skeleton, out var skeletonData))
        {
            throw new InvalidOperationException("Invalid skeleton handle");
        }

        var instance = new AnimatorInstance(skeleton, skeletonData.NumJoints, inverseBindMatrices, skeletonRootTransform);
        _instances.Add(instance);
        return instance;
    }

    public void RemoveAnimator(AnimatorInstance instance)
    {
        _instances.Remove(instance);
        instance.Dispose();
    }

    public void Update(float deltaTime)
    {
        foreach (var instance in _instances)
        {
            if (!instance.IsPlaying || !instance.CurrentAnimation.IsValid)
            {
                continue;
            }

            if (!_resource.TryGetSkeleton(instance.Skeleton, out var skeleton) ||
                !_resource.TryGetAnimation(instance.CurrentAnimation, out var clip))
            {
                continue;
            }

            instance.CurrentTime += deltaTime * instance.PlaybackSpeed;

            if (instance.Loop)
            {
                while (instance.CurrentTime >= clip.Duration)
                {
                    instance.CurrentTime -= clip.Duration;
                }
                while (instance.CurrentTime < 0)
                {
                    instance.CurrentTime += clip.Duration;
                }
            }
            else
            {
                if (instance.CurrentTime >= clip.Duration)
                {
                    instance.CurrentTime = clip.Duration;
                    instance.IsPlaying = false;
                }
                else if (instance.CurrentTime < 0)
                {
                    instance.CurrentTime = 0;
                    instance.IsPlaying = false;
                }
            }

            var ratio = clip.Duration > 0 ? instance.CurrentTime / clip.Duration : 0;
            SampleAnimation(skeleton, clip, ratio, instance.BoneMatrices);
        }
    }

    private static void SampleAnimation(RuntimeSkeleton skeleton, RuntimeAnimationClip clip, float ratio, BoneMatricesData boneMatrices)
    {
        var numJoints = skeleton.NumJoints;
        using var transformsArray = Float4x4Array.Create(new Matrix4x4[numJoints]);

        var samplingDesc = new SamplingJobDesc
        {
            Context = clip.Context,
            Ratio = ratio,
            OutTransforms = transformsArray
        };

        if (!skeleton.Animation.RunSamplingJob(in samplingDesc))
        {
            return;
        }

        for (var i = 0; i < Math.Min(numJoints, boneMatrices.NumBones); i++)
        {
            boneMatrices.ModelTransforms[i] = transformsArray.Value.AsSpan()[i];
        }

        boneMatrices.ComputeFinalMatrices();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var instance in _instances)
        {
            instance.Dispose();
        }
        _instances.Clear();

        _resource.Dispose();
    }
}

public sealed class AnimatorInstance : IDisposable
{
    public RuntimeSkeletonHandle Skeleton { get; }
    public RuntimeAnimationHandle CurrentAnimation { get; set; }
    public float CurrentTime { get; set; }
    public float PlaybackSpeed { get; set; } = 1.0f;
    public bool IsPlaying { get; set; } = true;
    public bool Loop { get; set; } = true;
    public BoneMatricesData BoneMatrices { get; }

    internal AnimatorInstance(RuntimeSkeletonHandle skeleton, int numJoints, Matrix4x4[]? inverseBindMatrices, Matrix4x4 skeletonRootTransform)
    {
        Skeleton = skeleton;
        BoneMatrices = new BoneMatricesData(numJoints, inverseBindMatrices ?? [], skeletonRootTransform);
    }

    public ReadOnlySpan<Matrix4x4> GetFinalBoneMatrices()
    {
        return BoneMatrices.FinalBoneMatrices.AsSpan(0, BoneMatrices.NumBones);
    }

    public void Dispose()
    {
    }
}
