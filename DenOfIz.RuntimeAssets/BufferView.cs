using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace RuntimeAssets;


[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct VertexBufferView(GPUBufferView view, uint stride, uint count)
{
    public readonly GPUBufferView View = view;
    public readonly uint Stride = stride;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.Buffer != 0 && View.NumBytes != 0;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct IndexBufferView(GPUBufferView view, IndexType indexType, uint count)
{
    public readonly GPUBufferView View = view;
    public readonly IndexType IndexType = indexType;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.Buffer != 0 && View.NumBytes != 0;
    }
}