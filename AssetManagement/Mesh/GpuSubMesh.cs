namespace AssetManagement.Mesh;

public class GpuSubMesh
{
    private bool _isLoaded = false;
    private SubMeshData? _subMeshData = null;
    private readonly BufferRegion _vertexBufferRegion;
    private readonly BufferRegion _indexBufferRegion;

    public GpuSubMesh(BufferRegion vertexBufferRegion, BufferRegion indexBufferRegion)
    {
        _vertexBufferRegion = vertexBufferRegion;
        _indexBufferRegion = indexBufferRegion;
        _isLoaded = false;
    }

    public void Load(BatchResourceCopy resourceCopy, SubMeshData subMeshData, BinaryReader reader)
    {
        var loadDesc = _vertexBufferRegion.ToLoadDesc();
        loadDesc.Stream = subMeshData.VertexStream;
        loadDesc.Reader = reader;

        resourceCopy.LoadAssetStreamToBuffer(loadDesc);

        loadDesc.DstBuffer = _indexBufferRegion.BufferResource;
        loadDesc.DstBufferOffset = _indexBufferRegion.Offset;
        loadDesc.Stream = subMeshData.IndexStream;

        resourceCopy.LoadAssetStreamToBuffer(loadDesc);
        _isLoaded = true;
    }
    
    
}