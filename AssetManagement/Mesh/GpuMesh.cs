using AssetManagement.Mesh;

namespace AssetManagement;

public class GpuMesh
{
    public enum BufferManagementStrategy
    {
        SingleBuffer,
        PerSubMesh
    }

    private MeshAsset _meshAsset;
    private bool _isLoaded;
    private List<GpuSubMesh> _subMeshes = [];
    private List<IBufferResource> _bufferResources = [];
    private BufferManagementStrategy _bufferManagementStrategy;
    private ILogicalDevice _device;

    public GpuMesh(ILogicalDevice device,
        BufferManagementStrategy bufferManagementStrategy = BufferManagementStrategy.SingleBuffer)
    {
        _device = device;
        _isLoaded = false;
    }

    public void Load(BatchResourceCopy resourceCopy, BinaryReader binaryReader)
    {
        MeshAssetReaderDesc binaryReaderDesc = new();
        binaryReaderDesc.Reader = binaryReader;
        var reader = new MeshAssetReader(binaryReaderDesc);

        _meshAsset = reader.Read();

        ulong totalBytesVB = 0;
        ulong totalBytesIB = 0;
        if (_bufferManagementStrategy == BufferManagementStrategy.SingleBuffer)
        {
            for (uint i = 0; i < _meshAsset.SubMeshes.NumElements(); ++i)
            {
                var subMeshAsset = _meshAsset.SubMeshes.GetElement(i);

                // var gpuSubMesh = new GpuSubMesh();
            }
        }

        _isLoaded = true;
    }
}