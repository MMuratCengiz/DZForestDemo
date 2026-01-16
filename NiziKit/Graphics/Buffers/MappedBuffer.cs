using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class MappedBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[] _buffers;
    private readonly IntPtr[] _mappedPtrs;
    private readonly bool _cycled;

    public MappedBuffer(bool cycled = true, string? debugName = null)
    {
        _cycled = cycled;
        var count = cycled ? (int)GraphicsContext.NumFrames : 1;
        _buffers = new Buffer[count];
        _mappedPtrs = new IntPtr[count];

        for (var i = 0; i < count; i++)
        {
            _buffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = (uint)Marshal.SizeOf<T>(),
                HeapType = HeapType.CpuGpu,
                Usage = (uint)BufferUsageFlagBits.Uniform,
                DebugName = debugName != null ? StringView.Create($"{debugName}_{i}") : null
            });
            _mappedPtrs[i] = _buffers[i].MapMemory();
        }
    }

    public Buffer Buffer => _buffers[Index];
    public uint Size => (uint)Marshal.SizeOf<T>();

    private int Index => _cycled ? GraphicsContext.FrameIndex : 0;

    public Buffer this[int index] => _buffers[_cycled ? index : 0];

    public void Write(in T data)
    {
        Marshal.StructureToPtr(data, _mappedPtrs[Index], false);
    }

    public T Read()
    {
        return Marshal.PtrToStructure<T>(_mappedPtrs[Index]);
    }

    public void Dispose()
    {
        foreach (var buffer in _buffers)
        {
            buffer.UnmapMemory();
            buffer.Dispose();
        }
    }
}
