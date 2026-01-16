using System.Numerics;
using DenOfIz;

namespace NiziKit.Graphics.Resources;

public partial class CycledTexture : IDisposable
{
    private TextureDesc _textureDesc;
    private readonly List<Texture> _textures = [];
    
    public CycledTexture(TextureDesc textureDesc)
    {
        _textureDesc = textureDesc;
        for (var i = 0; i < GraphicsContext.NumFrames; ++i)
        {
            _textures.Add(GraphicsContext.Device.CreateTexture(textureDesc));
            GraphicsContext.ResourceTracking.TrackTexture(_textures[i], QueueType.Graphics);
        }
    }
    
    public Texture this[int index]  => _textures[index];
    public Vector4 ClearColor => _textureDesc.ClearColorHint;
    public Vector2 ClearDepthStencil => _textureDesc.ClearDepthStencilHint;
    public void Dispose()
    {
        foreach (var texture in _textures)
        {
            GraphicsContext.ResourceTracking.UntrackTexture(texture);
            texture.Dispose();
        }
        _textures.Clear();
    }
}