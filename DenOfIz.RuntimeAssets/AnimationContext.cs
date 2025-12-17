using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;

namespace RuntimeAssets;

public sealed class AnimationContext : IContext, IDisposable
{
    private readonly RuntimeSkeletonStore _skeletonStore = new();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _skeletonStore.Dispose();
    }

    public RuntimeSkeletonHandle LoadSkeleton(string ozzSkeletonPath)
    {
        var resolvedPath = AssetPaths.ResolveSkeleton(ozzSkeletonPath);
        return _skeletonStore.AddSkeleton(resolvedPath);
    }

    public RuntimeSkeletonHandle LoadSkeletonFromData(byte[] skeletonData)
    {
        return _skeletonStore.AddSkeletonFromData(skeletonData);
    }

    public RuntimeAnimationHandle LoadAnimation(RuntimeSkeletonHandle skeleton, string ozzAnimationPath)
    {
        var resolvedPath = AssetPaths.ResolveAnimation(ozzAnimationPath);
        return _skeletonStore.AddAnimation(skeleton, resolvedPath);
    }

    public RuntimeAnimationHandle LoadAnimationFromData(RuntimeSkeletonHandle skeleton, byte[] animationData)
    {
        return _skeletonStore.AddAnimationFromData(skeleton, animationData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSkeleton(RuntimeSkeletonHandle handle, out RuntimeSkeleton skeleton)
    {
        return _skeletonStore.TryGetSkeleton(handle, out skeleton);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeSkeleton GetSkeletonRef(RuntimeSkeletonHandle handle)
    {
        return ref _skeletonStore.GetSkeletonRef(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetAnimation(RuntimeAnimationHandle handle, out RuntimeAnimationClip clip)
    {
        return _skeletonStore.TryGetAnimation(handle, out clip);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeAnimationClip GetAnimationRef(RuntimeAnimationHandle handle)
    {
        return ref _skeletonStore.GetAnimationRef(handle);
    }

    public void RemoveSkeleton(RuntimeSkeletonHandle handle)
    {
        _skeletonStore.RemoveSkeleton(handle);
    }

    public void RemoveAnimation(RuntimeAnimationHandle handle)
    {
        _skeletonStore.RemoveAnimation(handle);
    }

    public bool SampleAnimation(
        RuntimeSkeletonHandle skeletonHandle,
        RuntimeAnimationHandle animationHandle,
        float normalizedTime,
        Span<Matrix4x4> outModelTransforms)
    {
        if (!TryGetSkeleton(skeletonHandle, out var skeleton) ||
            !TryGetAnimation(animationHandle, out var clip))
        {
            return false;
        }

        var numJoints = skeleton.NumJoints;
        if (outModelTransforms.Length < numJoints)
        {
            return false;
        }

        using var transformsArray = Float4x4Array.Create(new Float4x4[numJoints]);

        var samplingDesc = new SamplingJobDesc
        {
            Context = clip.Context,
            Ratio = normalizedTime,
            OutTransforms = transformsArray
        };

        if (!skeleton.Animation.RunSamplingJob(in samplingDesc))
        {
            return false;
        }

        for (var i = 0; i < numJoints; i++)
        {
            outModelTransforms[i] = ConvertFloat4X4ToMatrix4X4(transformsArray.Value.AsSpan()[i]);
        }

        return true;
    }

    private static Matrix4x4 ConvertFloat4X4ToMatrix4X4(Float4x4 f)
    {
        return new Matrix4x4(
            f._11, f._12, f._13, f._14,
            f._21, f._22, f._23, f._24,
            f._31, f._32, f._33, f._34,
            f._41, f._42, f._43, f._44
        );
    }
}
