using DenOfIz;

namespace NiziKit.Assets;

public class Texture2d : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint MipLevels { get; set; }
    public Format Format { get; set; }
    public Texture GpuTexture { get; set; }

    internal uint Index { get; set; }
    public Graphics.Batching.TextureId Id => new(Index, 0);
    

    public void Dispose()
    {
        GpuTexture.Dispose();
    }
}
