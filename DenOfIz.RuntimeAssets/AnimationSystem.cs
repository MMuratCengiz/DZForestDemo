using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;
using ECS.Components;

namespace RuntimeAssets;

public interface ITimeResource : IContext
{
    float DeltaTime { get; }
}

public sealed class AnimationSystem : ISystem
{
    private AnimationContext _animation = null!;
    private ITimeResource _time = null!;
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
        _animation = world.GetContext<AnimationContext>();
        _time = world.GetContext<ITimeResource>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        foreach (var item in _world.Query<AnimatorComponent, BoneMatricesComponent>())
        {
            ref var animator = ref item.Component1;
            ref readonly var boneMatricesComponent = ref item.Component2;

            if (!boneMatricesComponent.IsValid)
            {
                continue;
            }

            var boneMatrices = boneMatricesComponent.Data;

            if (!animator.IsPlaying || !animator.CurrentAnimation.IsValid)
            {
                continue;
            }

            if (!_animation.TryGetSkeleton(animator.Skeleton, out var skeleton) ||
                !_animation.TryGetAnimation(animator.CurrentAnimation, out var clip))
            {
                continue;
            }

            animator.CurrentTime += _time.DeltaTime * animator.PlaybackSpeed;

            if (animator.Loop)
            {
                while (animator.CurrentTime >= clip.Duration)
                {
                    animator.CurrentTime -= clip.Duration;
                }
                while (animator.CurrentTime < 0)
                {
                    animator.CurrentTime += clip.Duration;
                }
            }
            else
            {
                if (animator.CurrentTime >= clip.Duration)
                {
                    animator.CurrentTime = clip.Duration;
                    animator.IsPlaying = false;
                }
                else if (animator.CurrentTime < 0)
                {
                    animator.CurrentTime = 0;
                    animator.IsPlaying = false;
                }
            }

            var ratio = clip.Duration > 0 ? animator.CurrentTime / clip.Duration : 0;
            SampleAnimation(skeleton, clip, ratio, boneMatrices);
        }
    }

    private static void SampleAnimation(RuntimeSkeleton skeleton, RuntimeAnimationClip clip, float ratio, BoneMatricesData boneMatrices)
    {
        var numJoints = skeleton.NumJoints;
        using var transformsArray = Float4x4Array.Create(new Float4x4[numJoints]);

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
            boneMatrices.ModelTransforms[i] = ConvertFloat4x4ToMatrix4x4(transformsArray.Value.AsSpan()[i]);
        }

        boneMatrices.ComputeFinalMatrices();
    }

    private static Matrix4x4 ConvertFloat4x4ToMatrix4x4(Float4x4 f)
    {
        return new Matrix4x4(
            f._11, f._12, f._13, f._14,
            f._21, f._22, f._23, f._24,
            f._31, f._32, f._33, f._34,
            f._41, f._42, f._43, f._44
        );
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
