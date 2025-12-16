using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics;

public struct RingBufferDesc
{
    public LogicalDevice LogicalDevice;
    public nuint DataNumBytes;
    public nuint NumEntries;
    public nuint MaxChunkNumBytes;
}

public struct GPUBufferView
{
    public Buffer Buffer;
    public ulong NumBytes;
    public ulong Offset;
}

public sealed class RingBuffer : IDisposable
{
    private const nuint Alignment = 256;

    private readonly List<ChunkInfo> _chunks = [];
    private readonly nuint _dataNumBytes;
    private readonly nuint _numEntries;
    private bool _disposed;
    private bool _memoryMapped;

    public RingBuffer(RingBufferDesc desc)
    {
        _dataNumBytes = desc.DataNumBytes;
        _numEntries = desc.NumEntries;
        AlignedNumBytes = AlignUp(desc.DataNumBytes, Alignment);
        TotalNumBytes = AlignedNumBytes * desc.NumEntries;

        var entriesPerChunk = desc.MaxChunkNumBytes / AlignedNumBytes;
        if (entriesPerChunk == 0)
        {
            throw new ArgumentException(
                $"Single entry size {AlignedNumBytes} exceeds MaxChunkNumBytes {desc.MaxChunkNumBytes}");
        }

        var remainingEntries = desc.NumEntries;
        nuint currentIndex = 0;

        while (remainingEntries > 0)
        {
            var entriesInThisChunk = Math.Min(entriesPerChunk, remainingEntries);
            var chunkNumBytes = entriesInThisChunk * AlignedNumBytes;

            var buffer = desc.LogicalDevice.CreateBuffer(new BufferDesc
            {
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                NumBytes = chunkNumBytes,
                HeapType = HeapType.CpuGpu,
                DebugName = StringView.Create("RingBuffer_Chunk")
            });

            _chunks.Add(new ChunkInfo
            {
                Buffer = buffer,
                StartIndex = currentIndex,
                EndIndex = currentIndex + entriesInThisChunk,
                NumBytes = chunkNumBytes
            });

            currentIndex += entriesInThisChunk;
            remainingEntries -= entriesInThisChunk;
        }
    }

    public nuint AlignedNumBytes { get; }

    public nuint TotalNumBytes { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_memoryMapped)
        {
            foreach (var chunk in _chunks)
            {
                chunk.Buffer.UnmapMemory();
            }
        }

        foreach (var chunk in _chunks)
        {
            chunk.Buffer.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public GPUBufferView GetBufferView(nuint index)
    {
        if (index >= _numEntries)
        {
            return default;
        }

        var chunkIndex = GetChunkIndexForEntry(index);
        if (chunkIndex >= _chunks.Count)
        {
            return default;
        }

        var chunk = _chunks[chunkIndex];
        var indexWithinChunk = index - chunk.StartIndex;
        var offsetWithinChunk = indexWithinChunk * AlignedNumBytes;

        return new GPUBufferView
        {
            Buffer = chunk.Buffer,
            NumBytes = _dataNumBytes,
            Offset = offsetWithinChunk
        };
    }

    public IntPtr GetMappedMemory(nuint index)
    {
        if (index >= _numEntries)
        {
            return IntPtr.Zero;
        }

        MapMemory();

        var chunkIndex = GetChunkIndexForEntry(index);
        var chunk = _chunks[chunkIndex];
        var indexWithinChunk = index - chunk.StartIndex;
        var offsetWithinChunk = indexWithinChunk * AlignedNumBytes;

        return chunk.MappedMemory + (nint)offsetWithinChunk;
    }

    private void MapMemory()
    {
        if (_memoryMapped)
        {
            return;
        }

        _memoryMapped = true;
        for (var i = 0; i < _chunks.Count; i++)
        {
            var chunk = _chunks[i];
            chunk.MappedMemory = chunk.Buffer.MapMemory();
            _chunks[i] = chunk;
        }
    }

    private static nuint AlignUp(nuint value, nuint alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private int GetChunkIndexForEntry(nuint entryIndex)
    {
        var left = 0;
        var right = _chunks.Count - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var chunk = _chunks[mid];

            if (entryIndex >= chunk.StartIndex && entryIndex < chunk.EndIndex)
            {
                return mid;
            }

            if (entryIndex < chunk.StartIndex)
            {
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }

        return 0;
    }

    private struct ChunkInfo
    {
        public Buffer Buffer;
        public IntPtr MappedMemory;
        public nuint StartIndex;
        public nuint EndIndex;
        public nuint NumBytes;
    }
}