using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public class GPUMesh
{
    public IndexType IndexType = IndexType.Uint32;
    public GpuBufferView VertexBuffer;
    public GpuBufferView IndexBuffer;
    public uint VertexStride;
    public uint NumVertices;
    public uint NumIndices;
}