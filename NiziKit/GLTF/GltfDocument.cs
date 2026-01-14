using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public sealed class GltfDocument
{
    public GltfRoot Root { get; init; } = new();
    public byte[][] Buffers { get; init; } = [];
    public string? BasePath { get; init; }

    public ReadOnlySpan<byte> GetBufferData(int bufferViewIndex)
    {
        if (Root.BufferViews == null || bufferViewIndex >= Root.BufferViews.Count)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        var view = Root.BufferViews[bufferViewIndex];
        if (view.Buffer >= Buffers.Length)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        var buffer = Buffers[view.Buffer];
        return buffer.AsSpan(view.ByteOffset, view.ByteLength);
    }

    public ReadOnlySpan<byte> GetAccessorData(int accessorIndex)
    {
        if (Root.Accessors == null || accessorIndex >= Root.Accessors.Count)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        var accessor = Root.Accessors[accessorIndex];
        if (!accessor.BufferView.HasValue)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        var viewData = GetBufferData(accessor.BufferView.Value);
        if (viewData.IsEmpty)
        {
            return viewData;
        }

        var componentSize = GltfComponentType.GetSize(accessor.ComponentType);
        var componentCount = GltfAccessorType.GetComponentCount(accessor.Type);
        var elementSize = componentSize * componentCount;

        var view = Root.BufferViews![accessor.BufferView.Value];
        var stride = Math.Max(view.ByteStride ?? 0, elementSize);

        int totalSize;
        if (accessor.Count == 0)
        {
            totalSize = 0;
        }
        else if (stride == elementSize)
        {
            totalSize = accessor.Count * elementSize;
        }
        else
        {
            totalSize = (accessor.Count - 1) * stride + elementSize;
        }

        var availableBytes = viewData.Length - accessor.ByteOffset;
        if (totalSize > availableBytes)
        {
            totalSize = availableBytes;
        }

        if (totalSize <= 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return viewData.Slice(accessor.ByteOffset, totalSize);
    }

    public int GetAccessorStride(int accessorIndex)
    {
        if (Root.Accessors == null || accessorIndex >= Root.Accessors.Count)
        {
            return 0;
        }

        var accessor = Root.Accessors[accessorIndex];
        if (!accessor.BufferView.HasValue || Root.BufferViews == null)
        {
            return 0;
        }

        var view = Root.BufferViews[accessor.BufferView.Value];
        var componentSize = GltfComponentType.GetSize(accessor.ComponentType);
        var componentCount = GltfAccessorType.GetComponentCount(accessor.Type);
        var elementSize = componentSize * componentCount;

        return Math.Max(view.ByteStride ?? 0, elementSize);
    }
}
