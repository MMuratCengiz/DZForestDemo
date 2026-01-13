using NiziKit.Graphics;

namespace NiziKit.Assets;

public abstract class CachedAsset<T> : IAsset where T : CachedAsset<T>
{
    public void Load(GraphicsContext context, string path)
    {
    }

    public void Load(GraphicsContext context, byte[] bytes)
    {
    }

    public virtual void Dispose()
    {
        
    }
}