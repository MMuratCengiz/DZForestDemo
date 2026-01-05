using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using BinaryWriter = DenOfIz.BinaryWriter;

namespace RuntimeAssets.Store;

public readonly struct RuntimeSkeleton
{
    public readonly OzzAnimation Animation;
    public readonly int NumJoints;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeSkeleton(OzzAnimation animation, int numJoints)
    {
        Animation = animation;
        NumJoints = numJoints;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ulong)Animation != 0;
    }
}

public readonly struct RuntimeAnimationClip
{
    public readonly OzzContext Context;
    public readonly float Duration;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeAnimationClip(OzzContext context, float duration)
    {
        Context = context;
        Duration = duration;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ulong)Context != 0;
    }
}

public sealed class RuntimeSkeletonStore : IDisposable
{
    private readonly List<AnimationSlot> _animationSlots = [];
    private readonly Queue<uint> _freeAnimationIndices = new();
    private readonly Queue<uint> _freeSkeletonIndices = new();
    private readonly List<SkeletonSlot> _skeletonSlots = [];
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var slot in _skeletonSlots)
        {
            if (slot.IsOccupied)
            {
                slot.Skeleton.Animation.Dispose();
            }
        }

        _skeletonSlots.Clear();
        _animationSlots.Clear();
    }

    public RuntimeSkeletonHandle AddSkeleton(string ozzSkeletonPath)
    {
        var animation = new OzzAnimation(StringView.Create(ozzSkeletonPath));
        if (!animation.IsValid())
        {
            animation.Dispose();
            return RuntimeSkeletonHandle.Invalid;
        }

        var skeleton = new RuntimeSkeleton(animation, animation.GetNumJoints());
        return AllocateSkeletonSlot(skeleton);
    }

    public RuntimeSkeletonHandle AddSkeletonFromData(byte[] skeletonData)
    {
        using var container = new BinaryContainer();
        var writer = BinaryWriter.CreateFromContainer(container);

        var handle = GCHandle.Alloc(skeletonData, GCHandleType.Pinned);
        try
        {
            writer.WriteBytes(new ByteArrayView
            {
                Elements = handle.AddrOfPinnedObject(),
                NumElements = (ulong)skeletonData.Length
            });
        }
        finally
        {
            handle.Free();
            writer.Dispose();
        }

        var animation = OzzAnimation.CreateFromBinaryContainer(container);
        if (!animation.IsValid())
        {
            animation.Dispose();
            return RuntimeSkeletonHandle.Invalid;
        }

        var skeleton = new RuntimeSkeleton(animation, animation.GetNumJoints());
        return AllocateSkeletonSlot(skeleton);
    }

    public RuntimeAnimationHandle AddAnimation(RuntimeSkeletonHandle skeletonHandle, string ozzAnimationPath)
    {
        if (!TryGetSkeleton(skeletonHandle, out var skeleton))
        {
            return RuntimeAnimationHandle.Invalid;
        }

        var context = skeleton.Animation.NewContext();
        if (!skeleton.Animation.LoadAnimation(StringView.Create(ozzAnimationPath), context))
        {
            skeleton.Animation.DestroyContext(context);
            return RuntimeAnimationHandle.Invalid;
        }

        var duration = OzzAnimation.GetAnimationDuration(context);
        var clip = new RuntimeAnimationClip(context, duration);
        return AllocateAnimationSlot(clip, skeletonHandle);
    }

    public RuntimeAnimationHandle AddAnimationFromData(RuntimeSkeletonHandle skeletonHandle, byte[] animationData)
    {
        if (!TryGetSkeleton(skeletonHandle, out var skeleton))
        {
            return RuntimeAnimationHandle.Invalid;
        }

        using var container = new BinaryContainer();
        var writer = BinaryWriter.CreateFromContainer(container);

        var handle = GCHandle.Alloc(animationData, GCHandleType.Pinned);
        try
        {
            writer.WriteBytes(new ByteArrayView
            {
                Elements = handle.AddrOfPinnedObject(),
                NumElements = (ulong)animationData.Length
            });
        }
        finally
        {
            handle.Free();
            writer.Dispose();
        }

        var context = skeleton.Animation.NewContext();
        if (!skeleton.Animation.LoadAnimationFromBinaryContainer(container, context))
        {
            skeleton.Animation.DestroyContext(context);
            return RuntimeAnimationHandle.Invalid;
        }

        var duration = OzzAnimation.GetAnimationDuration(context);
        var clip = new RuntimeAnimationClip(context, duration);
        return AllocateAnimationSlot(clip, skeletonHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSkeleton(RuntimeSkeletonHandle handle, out RuntimeSkeleton skeleton)
    {
        var slots = CollectionsMarshal.AsSpan(_skeletonSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            skeleton = default;
            return false;
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            skeleton = default;
            return false;
        }

        skeleton = slot.Skeleton;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeSkeleton GetSkeletonRef(RuntimeSkeletonHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_skeletonSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return ref Unsafe.NullRef<RuntimeSkeleton>();
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return ref Unsafe.NullRef<RuntimeSkeleton>();
        }

        return ref slot.Skeleton;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeSkeleton GetSkeleton(RuntimeSkeletonHandle handle)
    {
        if (!TryGetSkeleton(handle, out var skeleton))
        {
            ThrowInvalidSkeletonHandle();
        }

        return skeleton;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidSkeletonHandle()
    {
        throw new InvalidOperationException("Invalid skeleton handle.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetAnimation(RuntimeAnimationHandle handle, out RuntimeAnimationClip clip)
    {
        var slots = CollectionsMarshal.AsSpan(_animationSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            clip = default;
            return false;
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            clip = default;
            return false;
        }

        clip = slot.Clip;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeAnimationClip GetAnimationRef(RuntimeAnimationHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_animationSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return ref Unsafe.NullRef<RuntimeAnimationClip>();
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return ref Unsafe.NullRef<RuntimeAnimationClip>();
        }

        return ref slot.Clip;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeAnimationClip GetAnimation(RuntimeAnimationHandle handle)
    {
        if (!TryGetAnimation(handle, out var clip))
        {
            ThrowInvalidAnimationHandle();
        }

        return clip;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidAnimationHandle()
    {
        throw new InvalidOperationException("Invalid animation handle.");
    }

    public void RemoveSkeleton(RuntimeSkeletonHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_skeletonSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return;
        }

        ref var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return;
        }

        slot.Skeleton.Animation.Dispose();
        slot = new SkeletonSlot(default, slot.Generation + 1, false);
        _freeSkeletonIndices.Enqueue(handle.Index);
    }

    public void RemoveAnimation(RuntimeAnimationHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_animationSlots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return;
        }

        ref var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return;
        }

        if (TryGetSkeleton(slot.SkeletonHandle, out var skeleton))
        {
            skeleton.Animation.DestroyContext(slot.Clip.Context);
        }

        slot = new AnimationSlot(default, default, slot.Generation + 1, false);
        _freeAnimationIndices.Enqueue(handle.Index);
    }

    private RuntimeSkeletonHandle AllocateSkeletonSlot(RuntimeSkeleton skeleton)
    {
        if (_freeSkeletonIndices.TryDequeue(out var freeIndex))
        {
            var slots = CollectionsMarshal.AsSpan(_skeletonSlots);
            ref var slot = ref slots[(int)freeIndex];
            var newGeneration = slot.Generation + 1;
            slot = new SkeletonSlot(skeleton, newGeneration, true);
            return new RuntimeSkeletonHandle(freeIndex, newGeneration);
        }

        var index = (uint)_skeletonSlots.Count;
        const uint initialGeneration = 1;
        _skeletonSlots.Add(new SkeletonSlot(skeleton, initialGeneration, true));
        return new RuntimeSkeletonHandle(index, initialGeneration);
    }

    private RuntimeAnimationHandle AllocateAnimationSlot(RuntimeAnimationClip clip,
        RuntimeSkeletonHandle skeletonHandle)
    {
        if (_freeAnimationIndices.TryDequeue(out var freeIndex))
        {
            var slots = CollectionsMarshal.AsSpan(_animationSlots);
            ref var slot = ref slots[(int)freeIndex];
            var newGeneration = slot.Generation + 1;
            slot = new AnimationSlot(clip, skeletonHandle, newGeneration, true);
            return new RuntimeAnimationHandle(freeIndex, newGeneration);
        }

        var index = (uint)_animationSlots.Count;
        const uint initialGeneration = 1;
        _animationSlots.Add(new AnimationSlot(clip, skeletonHandle, initialGeneration, true));
        return new RuntimeAnimationHandle(index, initialGeneration);
    }

    [StructLayout(LayoutKind.Sequential)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private struct SkeletonSlot(RuntimeSkeleton skeleton, uint generation, bool isOccupied)
    {
        public RuntimeSkeleton Skeleton = skeleton;
        public uint Generation = generation;
        public bool IsOccupied = isOccupied;
    }

    [StructLayout(LayoutKind.Sequential)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private struct AnimationSlot(
        RuntimeAnimationClip clip,
        RuntimeSkeletonHandle skeletonHandle,
        uint generation,
        bool isOccupied)
    {
        public RuntimeAnimationClip Clip = clip;
        public RuntimeSkeletonHandle SkeletonHandle = skeletonHandle;
        public uint Generation = generation;
        public bool IsOccupied = isOccupied;
    }
}