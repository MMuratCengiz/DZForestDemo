using DenOfIz.World.Graphics.Batching;

namespace DenOfIz.World.Assets;

public class Mesh : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public int VertexCount { get; set; }
    public int IndexCount { get; set; }
    public BoundingBox Bounds { get; set; }
    public MeshType MeshType { get; set; }
    public int MaterialIndex { get; set; } = -1;
    public VertexBufferView VertexBuffer { get; set; }
    public IndexBufferView IndexBuffer { get; set; }

    internal uint Index { get; set; }
    public MeshId Id => new(Index, 0);

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
