using NiziKit.Graphics;

namespace NiziKit.Assets;

public interface IAsset : IDisposable
{
    public void Load(GraphicsContext context, string path)
    {
    }

    public void Load(GraphicsContext context, byte[] bytes)
    {

    }
}
