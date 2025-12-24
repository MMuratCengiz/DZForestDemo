using System.Numerics;
using DenOfIz;
using Flecs.NET.Core;
using RuntimeAssets.Components;

namespace RuntimeAssets;

/// <summary>
/// Time resource interface for animation timing.
/// </summary>
public interface ITimeResource
{
    float DeltaTime { get; }
}

/// <summary>
/// Registers animation systems.
/// </summary>
public static class AnimationSystems
{
    /// <summary>
    /// Register the animation update system.
    /// </summary>
    public static void Register(World world)
    {
        world.System<AnimatorComponent, BoneMatricesComponent>("AnimationSystem")
            .Kind(Ecs.OnUpdate)
            .Each((Entity entity, ref AnimatorComponent animator, ref BoneMatricesComponent boneMatricesComponent) =>
            {
                if (!boneMatricesComponent.IsValid)
                {
                    return;
                }

                var boneMatrices = boneMatricesComponent.Data;
                if (!animator.IsPlaying || !animator.CurrentAnimation.IsValid)
                {
                    return;
                }

                ref var animation = ref world.GetMut<AnimationResource>();
                ref var time = ref world.GetMut<ITimeResource>();

                if (!animation.TryGetSkeleton(animator.Skeleton, out var skeleton) ||
                    !animation.TryGetAnimation(animator.CurrentAnimation, out var clip))
                {
                    return;
                }

                animator.CurrentTime += time.DeltaTime * animator.PlaybackSpeed;

                if (animator.Loop)
                {
                    while (animator.CurrentTime >= clip.Duration)
                        animator.CurrentTime -= clip.Duration;
                    while (animator.CurrentTime < 0)
                        animator.CurrentTime += clip.Duration;
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
            });
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

    /// <summary>
    /// Converts ozz Float4x4 to System.Numerics.Matrix4x4.
    /// </summary>
    private static Matrix4x4 ConvertFloat4x4ToMatrix4x4(Float4x4 f)
    {
        return new Matrix4x4(
            f._11, f._12, f._13, f._14,
            f._21, f._22, f._23, f._24,
            f._31, f._32, f._33, f._34,
            f._41, f._42, f._43, f._44
        );
    }
}
