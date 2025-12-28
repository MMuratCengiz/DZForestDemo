using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Binding;

public sealed class CpuVisibleBufferAllocator : IDisposable
{
    private const ulong DefaultChunkSize = 64 * 1024; // 64KB max for CBV
    private const ulong CbvAlignment = 256;

    private readonly LogicalDevice _logicalDevice;
    private readonly ulong _chunkSize;
    private readonly List<BufferChunk> _chunks = [];
    private readonly Dictionary<AllocationKey, CpuVisibleBufferView> _allocations = [];
    private readonly List<CpuVisibleBufferView> _freeList = [];
    private ulong _nextOffset;
    private bool _disposed;

    public CpuVisibleBufferAllocator(LogicalDevice logicalDevice, ulong chunkSize = DefaultChunkSize)
    {
        _logicalDevice = logicalDevice;
        _chunkSize = chunkSize;
        AllocateNewChunk();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CpuVisibleBufferView GetOrAllocate(object owner, string name, ulong size)
    {
        var key = new AllocationKey(owner, name);
        if (_allocations.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var view = Allocate(size);
        _allocations[key] = view;
        return view;
    }

    public void Release(object owner)
    {
        var keysToRemove = new List<AllocationKey>();
        foreach (var kvp in _allocations)
        {
            if (ReferenceEquals(kvp.Key.Owner, owner))
            {
                keysToRemove.Add(kvp.Key);
                _freeList.Add(kvp.Value);
            }
        }

        foreach (var key in keysToRemove)
        {
            _allocations.Remove(key);
        }
    }

    private CpuVisibleBufferView Allocate(ulong size)
    {
        var alignedSize = AlignUp(size, CbvAlignment);

        for (var i = _freeList.Count - 1; i >= 0; i--)
        {
            if (_freeList[i].NumBytes < alignedSize)
            {
                continue;
            }

            var result = _freeList[i];
            _freeList.RemoveAt(i);
            return result;
        }

        var currentChunkIndex = (int)(_nextOffset / _chunkSize);
        var offsetWithinChunk = _nextOffset % _chunkSize;

        if (currentChunkIndex >= _chunks.Count)
        {
            AllocateNewChunk();
        }

        var chunk = _chunks[currentChunkIndex];
        var remainingInChunk = _chunkSize - offsetWithinChunk;

        if (alignedSize > remainingInChunk)
        {
            currentChunkIndex++;
            _nextOffset = (ulong)currentChunkIndex * _chunkSize;
            offsetWithinChunk = 0;

            if (currentChunkIndex >= _chunks.Count)
            {
                AllocateNewChunk();
            }
            chunk = _chunks[currentChunkIndex];
        }

        var mappedMemory = chunk.MappedPtr + (nint)offsetWithinChunk;
        var view = new CpuVisibleBufferView(
            mappedMemory,
            chunk.Buffer,
            offsetWithinChunk,
            alignedSize
        );

        _nextOffset += alignedSize;
        return view;
    }

    private void AllocateNewChunk()
    {
        var buffer = _logicalDevice.CreateBuffer(new BufferDesc
        {
            Usage = (uint)BufferUsageFlagBits.Uniform,
            HeapType = HeapType.CpuGpu,
            NumBytes = _chunkSize,
            DebugName = StringView.Create($"CpuVisibleChunk_{_chunks.Count}")
        });

        var mappedPtr = buffer.MapMemory();
        _chunks.Add(new BufferChunk(buffer, mappedPtr));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var chunk in _chunks)
        {
            chunk.Buffer.UnmapMemory();
            chunk.Buffer.Dispose();
        }
        _chunks.Clear();
        _allocations.Clear();
        _freeList.Clear();
    }

    private readonly struct BufferChunk(Buffer buffer, IntPtr mappedPtr)
    {
        public readonly Buffer Buffer = buffer;
        public readonly IntPtr MappedPtr = mappedPtr;
    }

    private readonly struct AllocationKey(object owner, string name) : IEquatable<AllocationKey>
    {
        public readonly object Owner = owner;
        public readonly string Name = name;

        public bool Equals(AllocationKey other) => ReferenceEquals(Owner, other.Owner) && Name == other.Name;
        public override bool Equals(object? obj) => obj is AllocationKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Owner), Name);
    }
}
