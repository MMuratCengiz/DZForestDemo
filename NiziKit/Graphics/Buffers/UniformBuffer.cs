using System.Runtime.InteropServices;
using NiziKit.Graphics.Binding;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class UniformBuffer<T> : IDisposable where T : unmanaged
{
    private readonly GpuBufferView[] _views;
    private readonly bool _cycled;

    public UniformBuffer(bool cycled = true)
    {
        _cycled = cycled;
        var count = cycled ? (int)GraphicsContext.NumFrames : 1;
        _views = new GpuBufferView[count];

        for (var i = 0; i < count; i++)
        {
            _views[i] = GraphicsContext.UniformBufferArena.Request(Marshal.SizeOf<T>());
        }
    }

    public Buffer Buffer => _views[Index].Buffer;
    public uint Offset => _views[Index].Offset;
    public uint Size => _views[Index].NumBytes;

    private int Index => _cycled ? GraphicsContext.FrameIndex : 0;

    public GpuBufferView this[int index] => _views[_cycled ? index : 0];

    public void Write(in T data)
    {
        var view = _views[Index];
        view.Buffer.WriteData(in data, view.Offset);
    }

    public void Dispose()
    {
    }
}
