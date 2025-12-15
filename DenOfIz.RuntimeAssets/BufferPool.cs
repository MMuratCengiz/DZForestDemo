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

    public BufferView Allocate(ulong size, ulong alignment = 16)
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

        return newBlock.TryAllocate(size, alignment, out var newView) ? newView : BufferView.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong AlignUp(ulong value, ulong alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

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

    private sealed class BufferBlock(LogicalDevice device, uint usages, ulong size) : IDisposable
    {
        private readonly Buffer _buffer = device.CreateBuffer(new BufferDesc
        {
            NumBytes = size,
            Usages = usages,
            HeapType = HeapType.Gpu
        });

        private ulong _offset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAllocate(ulong size1, ulong alignment, out BufferView view)
        {
            var alignedOffset = AlignUp(_offset, alignment);

            if (alignedOffset + size1 > size)
            {
                view = BufferView.Invalid;
                return false;
            }

            view = new BufferView(_buffer, alignedOffset, size1);
            _offset = alignedOffset + size1;
            return true;
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
