using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;

namespace RuntimeAssets;

public readonly struct RuntimeTexture
{
    public readonly TextureResource Resource;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeTexture(TextureResource resource)
    {
        Resource = resource;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ulong)Resource != 0;
    }
}

public sealed class RuntimeTextureStore : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly List<TextureSlot> _slots = [];
    private readonly Queue<uint> _freeIndices = new();
    private bool _disposed;

    public RuntimeTextureStore(LogicalDevice device)
    {
        _device = device;
    }

    public RuntimeTextureHandle Add(string path, BatchResourceCopy batchCopy)
    {
        var texture = batchCopy.CreateAndLoadTexture(StringView.Create(path));
        return AllocateSlot(new RuntimeTexture(texture));
    }

    public RuntimeTextureHandle Add(TextureResource texture)
    {
        return AllocateSlot(new RuntimeTexture(texture));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(RuntimeTextureHandle handle, out RuntimeTexture texture)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            texture = default;
            return false;
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            texture = default;
            return false;
        }

        texture = slot.Texture;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeTexture GetRef(RuntimeTextureHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return ref Unsafe.NullRef<RuntimeTexture>();
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return ref Unsafe.NullRef<RuntimeTexture>();
        }

        return ref slot.Texture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeTexture Get(RuntimeTextureHandle handle)
    {
        if (!TryGet(handle, out var texture))
        {
            ThrowInvalidHandle();
        }
        return texture;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidHandle()
    {
        throw new InvalidOperationException("Invalid texture handle.");
    }

    public void Remove(RuntimeTextureHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
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

        slot.Texture.Resource.Dispose();
        slot = new TextureSlot(default, slot.Generation + 1, false);
        _freeIndices.Enqueue(handle.Index);
    }

    private RuntimeTextureHandle AllocateSlot(RuntimeTexture texture)
    {
        if (_freeIndices.TryDequeue(out var freeIndex))
        {
            var slots = CollectionsMarshal.AsSpan(_slots);
            ref var slot = ref slots[(int)freeIndex];
            var newGeneration = slot.Generation + 1;
            slot = new TextureSlot(texture, newGeneration, true);
            return new RuntimeTextureHandle(freeIndex, newGeneration);
        }

        var index = (uint)_slots.Count;
        const uint initialGeneration = 1;
        _slots.Add(new TextureSlot(texture, initialGeneration, true));
        return new RuntimeTextureHandle(index, initialGeneration);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var slot in _slots)
        {
            if (slot.IsOccupied)
            {
                slot.Texture.Resource.Dispose();
            }
        }
        _slots.Clear();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextureSlot
    {
        public RuntimeTexture Texture;
        public uint Generation;
        public bool IsOccupied;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureSlot(RuntimeTexture texture, uint generation, bool isOccupied)
        {
            Texture = texture;
            Generation = generation;
            IsOccupied = isOccupied;
        }
    }
}
