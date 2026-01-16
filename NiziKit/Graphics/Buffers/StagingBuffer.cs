using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class StagingBuffer : IDisposable
{
    public StagingBuffer(uint size, string? debugName = null)
    {
        Size = size;
        Buffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
        {
            NumBytes = size,
            HeapType = HeapType.CpuGpu,
            Usage = (uint)BufferUsageFlagBits.CopySrc,
            DebugName = debugName != null ? StringView.Create(debugName) : null
        });
        MappedPtr = Buffer.MapMemory();
    }

    public Buffer Buffer { get; }

    public uint Size { get; }

    public IntPtr MappedPtr { get; }

    public void Write(ReadOnlySpan<byte> data)
    {
        unsafe
        {
            data.CopyTo(new Span<byte>((void*)MappedPtr, (int)Size));
        }
    }

    public void Write<T>(in T data) where T : unmanaged
    {
        unsafe
        {
            fixed (T* ptr = &data)
            {
                new Span<byte>(ptr, sizeof(T)).CopyTo(new Span<byte>((void*)MappedPtr, (int)Size));
            }
        }
    }

    public void Write<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        unsafe
        {
            var bytes = MemoryMarshal.AsBytes(data);
            bytes.CopyTo(new Span<byte>((void*)MappedPtr, (int)Size));
        }
    }

    public void Read(Span<byte> destination)
    {
        unsafe
        {
            new Span<byte>((void*)MappedPtr, (int)Size).CopyTo(destination);
        }
    }

    public T Read<T>() where T : unmanaged
    {
        unsafe
        {
            return *(T*)MappedPtr;
        }
    }

    public void Dispose()
    {
        Buffer.UnmapMemory();
        Buffer.Dispose();
    }
}
