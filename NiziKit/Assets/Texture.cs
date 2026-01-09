using GpuTexture = DenOfIz.Texture;

namespace NiziKit.Assets;

public enum TextureFormat
{
    RGBA8,
    BC1,
    BC3,
    BC5,
    BC7
}

public class Texture : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint MipLevels { get; set; }
    public TextureFormat Format { get; set; }
    public GpuTexture GpuTexture { get; set; }

    internal uint Index { get; set; }
    public Graphics.Batching.TextureId Id => new(Index, 0);

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GpuTexture.Dispose();
    }
}
