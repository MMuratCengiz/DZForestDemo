using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace RuntimeAssets;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct BufferView(Buffer buffer, ulong offset, ulong size)
{
    public readonly Buffer Buffer = buffer;
    public readonly ulong Offset = offset;
    public readonly ulong Size = size;

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

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct VertexBufferView(BufferView view, uint stride, uint count)
{
    public readonly BufferView View = view;
    public readonly uint Stride = stride;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.IsValid;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct IndexBufferView(BufferView view, IndexType indexType, uint count)
{
    public readonly BufferView View = view;
    public readonly IndexType IndexType = indexType;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.IsValid;
    }
}