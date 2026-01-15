using DenOfIz;

namespace NiziKit.Graphics.Resources;

public partial class CycledTexture
{
    private readonly List<Texture> _textures = [];
    
    public CycledTexture(TextureDesc textureDesc)
    {
        for (var i = 0; i < GraphicsContext.NumFrames; ++i)
        {
            _textures.Add(GraphicsContext.Device.CreateTexture(textureDesc));
            GraphicsContext.ResourceTracking.TrackTexture(_textures[i], QueueType.Graphics);
        }
    }
    
    public Texture this[int index]  => _textures[index];
}