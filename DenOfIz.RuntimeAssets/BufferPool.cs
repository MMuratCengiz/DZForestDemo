using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace RuntimeAssets;

public sealed class BufferPool(LogicalDevice device, uint usages, ulong blockSize = 64 * 1024 * 1024)
    : IDisposable
{
    private readonly List<BufferBlock> _blocks = [];
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var block in _blocks)
        {
            block.Dispose();
        }

        _blocks.Clear();
    }

    public GPUBufferView Allocate(ulong size, ulong alignment = 16)
    {
        size = AlignUp(size, alignment);

        var blocks = CollectionsMarshal.AsSpan(_blocks);
        for (var i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].TryAllocate(size, alignment, out var view))
            {
                return view;
            }
        }

        var newBlockSize = Math.Max(blockSize, size);
        var newBlock = new BufferBlock(device, usages, newBlockSize);
        _blocks.Add(newBlock);

        return newBlock.TryAllocate(size, alignment, out var newView) ? newView : new GPUBufferView();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private sealed class BufferBlock(LogicalDevice device, uint usages, ulong size) : IDisposable
    {
        private readonly Buffer _buffer = device.CreateBuffer(new BufferDesc
        {
            NumBytes = size,
            Usage = usages,
            HeapType = HeapType.Gpu
        });

        private ulong _offset;

        public void Dispose()
        {
            _buffer.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAllocate(ulong numBytes, ulong alignment, out GPUBufferView view)
        {
            var alignedOffset = AlignUp(_offset, alignment);

            if (alignedOffset + numBytes > size)
            {
                view = new GPUBufferView();
                return false;
            }

            view = new GPUBufferView{ Buffer = _buffer, Offset = alignedOffset, NumBytes = numBytes};
            _offset = alignedOffset + numBytes;
            return true;
        }
    }
}