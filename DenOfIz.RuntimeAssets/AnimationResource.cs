using System.Runtime.CompilerServices;
using RuntimeAssets.Store;

namespace RuntimeAssets;

public sealed class AnimationResource : IDisposable
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
}
