using System.Numerics;
using DenOfIz.World.Graphics.Binding.Data;

namespace DenOfIz.World.Assets;

public sealed class AnimationManager : IDisposable
{
    private readonly List<AnimatorInstance> _instances = [];
    private bool _disposed;

    public AnimatorInstance CreateAnimator(Skeleton skeleton, Matrix4x4[]? inverseBindMatrices = null, Matrix4x4 skeletonRootTransform = default)
    {
        var instance = new AnimatorInstance(skeleton, inverseBindMatrices, skeletonRootTransform);
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
            if (!instance.IsPlaying || instance.CurrentAnimation == null)
            {
                continue;
            }

            instance.CurrentTime += deltaTime * instance.PlaybackSpeed;
            var duration = instance.CurrentAnimation.Duration;

            if (instance.Loop)
            {
                while (instance.CurrentTime >= duration)
                {
                    instance.CurrentTime -= duration;
                }
                while (instance.CurrentTime < 0)
                {
                    instance.CurrentTime += duration;
                }
            }
            else
            {
                if (instance.CurrentTime >= duration)
                {
                    instance.CurrentTime = duration;
                    instance.IsPlaying = false;
                }
                else if (instance.CurrentTime < 0)
                {
                    instance.CurrentTime = 0;
                    instance.IsPlaying = false;
                }
            }

            var ratio = duration > 0 ? instance.CurrentTime / duration : 0;
            SampleAnimation(instance.Skeleton, instance.CurrentAnimation, ratio, instance.BoneMatrices);
        }
    }

    private static void SampleAnimation(Skeleton skeleton, Animation animation, float ratio, GpuBoneMatricesData boneMatrices)
    {
        var numJoints = skeleton.JointCount;
        using var transformsArray = Float4x4Array.Create(new Matrix4x4[numJoints]);

        var samplingDesc = new SamplingJobDesc
        {
            Context = animation.OzzContext,
            Ratio = ratio,
            OutTransforms = transformsArray
        };

        if (!skeleton.OzzSkeleton.RunSamplingJob(in samplingDesc))
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
    }
}

public sealed class AnimatorInstance : IDisposable
{
    public Skeleton Skeleton { get; }
    public Animation? CurrentAnimation { get; set; }
    public float CurrentTime { get; set; }
    public float PlaybackSpeed { get; set; } = 1.0f;
    public bool IsPlaying { get; set; } = true;
    public bool Loop { get; set; } = true;
    public GpuBoneMatricesData BoneMatrices { get; }

    internal AnimatorInstance(Skeleton skeleton, Matrix4x4[]? inverseBindMatrices, Matrix4x4 skeletonRootTransform)
    {
        Skeleton = skeleton;
        BoneMatrices = new GpuBoneMatricesData(skeleton.JointCount, inverseBindMatrices ?? [], skeletonRootTransform);
    }

    public ReadOnlySpan<Matrix4x4> GetFinalBoneMatrices()
    {
        return BoneMatrices.FinalBoneMatrices.AsSpan(0, BoneMatrices.NumBones);
    }

    public void Dispose()
    {
    }
}
