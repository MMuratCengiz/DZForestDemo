using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class StructuredBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[] _buffers;
    private readonly bool _cycled;

    public StructuredBuffer(uint numElements, bool cycled = true, string? debugName = null)
    {
        _cycled = cycled;
        Count = numElements;
        var stride = (ulong)Marshal.SizeOf<T>();
        var numBuffers = cycled ? (int)GraphicsContext.NumFrames : 1;
        _buffers = new Buffer[numBuffers];

        for (var i = 0; i < numBuffers; i++)
        {
            _buffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = (uint)(stride * numElements),
                HeapType = HeapType.CpuGpu,
                StructureDesc = new StructuredBufferDesc
                {
                    NumElements = numElements,
                    Stride = stride
                },
                DebugName = debugName != null ? StringView.Create($"{debugName}_{i}") : null
            });
        }
    }

    public Buffer this[int index] => _buffers[index];

    public Buffer Buffer => _buffers[Index];
    public uint Count { get; }

    public uint Stride => (uint)Marshal.SizeOf<T>();
    public uint Size => Stride * Count;

    private int Index => _cycled ? GraphicsContext.FrameIndex : 0;

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            buffer.Dispose();
        }
    }
}
