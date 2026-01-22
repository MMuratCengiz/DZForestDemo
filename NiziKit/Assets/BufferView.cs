using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Graphics.Binding;

namespace NiziKit.Assets;


[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct VertexBufferView(GpuBufferView view, uint stride, uint count)
{
    public readonly GpuBufferView View = view;
    public readonly uint Stride = stride;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.Buffer != 0 && View.NumBytes != 0;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct IndexBufferView(GpuBufferView view, IndexType indexType, uint count)
{
    public readonly GpuBufferView View = view;
    public readonly IndexType IndexType = indexType;
    public readonly uint Count = count;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => View.Buffer != 0 && View.NumBytes != 0;
    }
}
