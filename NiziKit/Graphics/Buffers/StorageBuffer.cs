using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class StorageBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[] _buffers;
    private readonly bool _cycled;

    public StorageBuffer(uint elementCount, bool cycled = true, string? debugName = null)
    {
        _cycled = cycled;
        Count = elementCount;
        var numBuffers = cycled ? (int)GraphicsContext.NumFrames : 1;
        _buffers = new Buffer[numBuffers];

        for (var i = 0; i < numBuffers; i++)
        {
            _buffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = (uint)Marshal.SizeOf<T>() * elementCount,
                HeapType = HeapType.Gpu,
                Usage = (uint)BufferUsageFlagBits.Storage,
                DebugName = debugName != null ? StringView.Create($"{debugName}_{i}") : null
            });
        }
    }

    public Buffer Buffer => _buffers[Index];
    public uint Count { get; }

    public uint Size => (uint)Marshal.SizeOf<T>() * Count;

    private int Index => _cycled ? GraphicsContext.FrameIndex : 0;

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            buffer.Dispose();
        }
    }
}
