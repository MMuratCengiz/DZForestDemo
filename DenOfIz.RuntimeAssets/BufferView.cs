using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace RuntimeAssets;

public readonly struct BufferView
{
    public readonly Buffer Buffer;
    public readonly ulong Offset;
    public readonly ulong Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferView(Buffer buffer, ulong offset, ulong size)
    {
        Buffer = buffer;
        Offset = offset;
        Size = size;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ulong)Buffer != 0 && Size > 0;
    }

    public static BufferView Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }
}

public readonly struct VertexBufferView
{
    public readonly BufferView View;
    public readonly uint Stride;
    public readonly uint Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VertexBufferView(BufferView view, uint stride, uint count)
    {
        View = view;
        Stride = stride;
        Count = count;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.IsValid;
    }
}

public readonly struct IndexBufferView
{
    public readonly BufferView View;
    public readonly IndexType IndexType;
    public readonly uint Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndexBufferView(BufferView view, IndexType indexType, uint count)
    {
        View = view;
        IndexType = indexType;
        Count = count;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.IsValid;
    }
}
