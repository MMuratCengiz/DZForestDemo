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
        var chunk = _chunks[_currentChunk];
        if (chunk.Offset + size >= BufferChunkSize)
        {
            AddChunk();
            chunk = _chunks[_currentChunk];
        }
        var offset = (uint)chunk.Offset;
        chunk.Offset += size;
        return new GpuBufferView{ Buffer = chunk.Buffer, NumBytes = (uint)size, Offset = offset};
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