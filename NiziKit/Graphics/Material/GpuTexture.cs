using DenOfIz;

namespace NiziKit.Graphics.Material;

public class GpuTexture
{
    private Texture _texture;
    
    public Texture Texture => _texture;
    
    public GpuTexture(BatchResourceCopy batchResourceCopy, string path)
    {
    }

    public GpuTexture(BatchResourceCopy batchResourceCopy, byte[] data)
    {
    }
    
    public void Dispose()
    {
        _texture.Dispose();
    }
}