using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Binding;

public sealed class UniformBufferArena : IDisposable
{
    public struct Slot
    {
        public int Chunk;
        public int Offset;
    }

    private struct Chunk
    {
        public Buffer Buffer;
        public IntPtr Data;
        public int Offset;
    }
    
    private const uint BufferChunkSize = 65536;
    private const int ConstantBufferAlignment = 256;
    private readonly List<Chunk> _chunks = [];
    private int _currentChunk = 0;
    private readonly LogicalDevice _device;

    public UniformBufferArena(LogicalDevice device)
    {
        _device = device;
        AddChunk();
    }

    public GpuBufferView Request(int size)
    {
        ref var chunk = ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_chunks)[_currentChunk];

        var alignedOffset = AlignUp(chunk.Offset, ConstantBufferAlignment);

        if (alignedOffset + size >= BufferChunkSize)
        {
            AddChunk();
            chunk = ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_chunks)[_currentChunk];
            alignedOffset = 0;
        }

        var offset = (uint)alignedOffset;
        chunk.Offset = alignedOffset + size;
        return new GpuBufferView{ Buffer = chunk.Buffer, NumBytes = (uint)size, Offset = offset};
    }

    private static int AlignUp(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private void AddChunk()
    {
        var desc = new BufferDesc
        {
            Usage = (uint)BufferUsageFlagBits.Uniform,
            NumBytes = BufferChunkSize,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Create("Uniform Buffer Arena Chunk #" + _chunks.Count)
        };

        var chunk = new Chunk
        {
            Buffer = _device.CreateBuffer(desc)
        };
        chunk.Data = chunk.Buffer.MapMemory();
        chunk.Offset = 0;
        _chunks.Add(chunk);
        _currentChunk = _chunks.Count - 1;
    }

    public void Dispose()
    {
        foreach (var chunk in _chunks)
        {
            chunk.Buffer.UnmapMemory();
            chunk.Buffer.Dispose();
        }
    }
}