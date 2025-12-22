using DenOfIz;

namespace Graphics.RenderGraph;

public class GPUMesh
{
    public IndexType IndexType = IndexType.Uint32;
    public GPUBufferView VertexBuffer;
    public GPUBufferView IndexBuffer;
    public uint NumVertices;
    public uint NumIndices;
}