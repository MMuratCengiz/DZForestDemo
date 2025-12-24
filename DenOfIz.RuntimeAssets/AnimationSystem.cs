using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;
using ECS.Components;
using RuntimeAssets.Components;

namespace RuntimeAssets;

public interface ITimeResource : IResource
{
    float DeltaTime { get; }
}

public sealed class AnimationSystem : ISystem
{
    private AnimationResource _animation = null!;
    private ITimeResource _time = null!;
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
        _animation = world.GetResource<AnimationResource>();
        _time = world.GetResource<ITimeResource>();
    }

    private int _frameCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        _frameCount++;
        var entityCount = 0;

        foreach (var item in _world.Query<AnimatorComponent, BoneMatricesComponent>())
        {
            entityCount++;
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

            if (_frameCount % 300 != 1 || entityCount != 1)
            {
                continue;
            }

            var bone0 = boneMatrices.FinalBoneMatrices[0];
            var isIdentity = bone0 == Matrix4x4.Identity;
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

    /// <summary>
    /// Converts ozz Float4x4 to System.Numerics.Matrix4x4.
    /// ozz/DenOfIz Float4x4 stores data in column-major format (matching ozz's internal SIMD layout).
    /// For row-vector multiplication (mul(vec, mat) in HLSL), we need row-major matrices.
    /// The 1:1 field copy effectively treats columns as rows (implicit transpose).
    /// </summary>
    private static Matrix4x4 ConvertFloat4x4ToMatrix4x4(Float4x4 f)
    {
        // Float4x4 fields _11,_12,_13,_14 are column 0 (ozz cols[0].x,y,z,w)
        // For row-major row-vector convention, we want this column to become row 0.
        // The field mapping below achieves this (column 0 -> row 0, etc.)
        return new Matrix4x4(
            f._11, f._12, f._13, f._14,  // ozz col0 -> Matrix4x4 row0
            f._21, f._22, f._23, f._24,  // ozz col1 -> Matrix4x4 row1
            f._31, f._32, f._33, f._34,  // ozz col2 -> Matrix4x4 row2
            f._41, f._42, f._43, f._44   // ozz col3 -> Matrix4x4 row3
        );
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
