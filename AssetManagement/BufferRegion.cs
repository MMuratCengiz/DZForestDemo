namespace AssetManagement;

// This class doesn't initialize or create buffers, it simply references a part of a buffer(potentially all of it) 
public record BufferRegion(
    IBufferResource BufferResource,
    ulong Offset,
    ulong NumBytes
)
{
    public LoadAssetStreamToBufferDesc ToLoadDesc()
    {
        LoadAssetStreamToBufferDesc loadDesc = new();
        loadDesc.DstBuffer = BufferResource;
        loadDesc.DstBufferOffset = Offset;
        return loadDesc;
    }
}