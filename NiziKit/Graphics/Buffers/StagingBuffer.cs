using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics.Renderer.Pass;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Buffers;

public sealed class StagingBuffer : IDisposable
{
    private readonly Buffer[] _buffers;
    private readonly IntPtr[] _mappedPtrs;
    private readonly bool _cycled;

    public StagingBuffer(uint numBytes, bool cycled = true, string? debugName = null)
    {
        _cycled = cycled;
        NumBytes = numBytes;
        var numBuffers = cycled ? (int)GraphicsContext.NumFrames : 1;
        _buffers = new Buffer[numBuffers];
        _mappedPtrs = new IntPtr[numBuffers];

        for (var i = 0; i < numBuffers; i++)
        {
            _buffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = numBytes,
                HeapType = HeapType.CpuGpu,
                Usage = (uint)BufferUsageFlagBits.CopySrc,
                DebugName = debugName != null ? StringView.Create($"{debugName}_{i}") : null
            });
            _mappedPtrs[i] = _buffers[i].MapMemory();
        }
    }

    public Buffer Buffer => _buffers[Index];
    public IntPtr MappedPtr => _mappedPtrs[Index];
    public uint NumBytes { get; }

    private int Index => _cycled ? GraphicsContext.FrameIndex : 0;

    public void Write(ReadOnlySpan<byte> data)
    {
        unsafe
        {
            data.CopyTo(new Span<byte>((void*)MappedPtr, (int)NumBytes));
        }
    }

    public void Write<T>(in T data) where T : unmanaged
    {
        unsafe
        {
            fixed (T* ptr = &data)
            {
                new Span<byte>(ptr, sizeof(T)).CopyTo(new Span<byte>((void*)MappedPtr, (int)NumBytes));
            }
        }
    }

    public void Write<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        unsafe
        {
            var bytes = MemoryMarshal.AsBytes(data);
            bytes.CopyTo(new Span<byte>((void*)MappedPtr, (int)NumBytes));
        }
    }

    public void Read(Span<byte> destination)
    {
        unsafe
        {
            new Span<byte>((void*)MappedPtr, (int)NumBytes).CopyTo(destination);
        }
    }

    public T Read<T>() where T : unmanaged
    {
        unsafe
        {
            return *(T*)MappedPtr;
        }
    }

    public void CopyTo(RenderPass pass, Buffer dstBuffer, ulong dstOffset = 0, ulong srcOffset = 0, ulong numBytes = 0)
    {
        pass.CopyBufferRegion(new CopyBufferRegionDesc
        {
            SrcBuffer = Buffer,
            SrcOffset = srcOffset,
            DstBuffer = dstBuffer,
            DstOffset = dstOffset,
            NumBytes = numBytes == 0 ? NumBytes : numBytes
        });
    }

    public void CopyTo<T>(RenderPass pass, StructuredBuffer<T> dst) where T : unmanaged
    {
        CopyTo(pass, dst.Buffer);
    }

    public void CopyTo<T>(RenderPass pass, StorageBuffer<T> dst) where T : unmanaged
    {
        CopyTo(pass, dst.Buffer);
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
