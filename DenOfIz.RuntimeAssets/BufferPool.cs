using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace RuntimeAssets;

public sealed class BufferPool : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly uint _usages;
    private readonly ulong _blockSize;
    private readonly List<BufferBlock> _blocks = [];
    private bool _disposed;

    public BufferPool(LogicalDevice device, uint usages, ulong blockSize = 64 * 1024 * 1024)
    {
        _device = device;
        _usages = usages;
        _blockSize = blockSize;
    }

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

        var newBlockSize = Math.Max(_blockSize, size);
        var newBlock = new BufferBlock(_device, _usages, newBlockSize);
        _blocks.Add(newBlock);

        if (newBlock.TryAllocate(size, alignment, out var newView))
        {
            return newView;
        }

        return BufferView.Invalid;
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

    private sealed class BufferBlock : IDisposable
    {
        private readonly Buffer _buffer;
        private readonly ulong _size;
        private ulong _offset;

        public BufferBlock(LogicalDevice device, uint usages, ulong size)
        {
            _size = size;
            _buffer = device.CreateBuffer(new BufferDesc
            {
                NumBytes = size,
                Usages = usages,
                HeapType = HeapType.Gpu
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAllocate(ulong size, ulong alignment, out BufferView view)
        {
            var alignedOffset = AlignUp(_offset, alignment);

            if (alignedOffset + size > _size)
            {
                view = BufferView.Invalid;
                return false;
            }

            view = new BufferView(_buffer, alignedOffset, size);
            _offset = alignedOffset + size;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AlignUp(ulong value, ulong alignment)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }
}
