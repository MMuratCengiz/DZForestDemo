using System.Numerics;
using DenOfIz;

namespace NiziKit.Graphics.Resources;

public partial class CycledTexture : IDisposable
{
    private TextureDesc _textureDesc;
    private readonly List<Texture> _textures = [];
    private readonly bool _isScreenSized;
    private bool _disposed;

    public CycledTexture(TextureDesc textureDesc, bool isScreenSized = false)
    {
        _textureDesc = textureDesc;
        _isScreenSized = isScreenSized;
        CreateTextures();

        if (_isScreenSized)
        {
            GraphicsContext.OnResize += HandleResize;
        }
    }

    public Texture this[int index] => _textures[index];
    public uint Width => _textureDesc.Width;
    public uint Height => _textureDesc.Height;
    public Format Format => _textureDesc.Format;
    public Vector4 ClearColor => _textureDesc.ClearColorHint;
    public Vector2 ClearDepthStencil => _textureDesc.ClearDepthStencilHint;

    private void CreateTextures()
    {
        for (var i = 0; i < GraphicsContext.NumFrames; ++i)
        {
            var texture = GraphicsContext.Device.CreateTexture(_textureDesc);
            _textures.Add(texture);
            GraphicsContext.ResourceTracking.TrackTexture(texture, QueueType.Graphics);
        }
    }

    private void DestroyTextures()
    {
        foreach (var texture in _textures)
        {
            GraphicsContext.ResourceTracking.UntrackTexture(texture);
            texture.Dispose();
        }
        _textures.Clear();
    }

    private void HandleResize(uint width, uint height)
    {
        if (_disposed)
        {
            return;
        }

        if (_textureDesc.Width == width && _textureDesc.Height == height)
        {
            return;
        }

        DestroyTextures();
        _textureDesc.Width = width;
        _textureDesc.Height = height;
        CreateTextures();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_isScreenSized)
        {
            GraphicsContext.OnResize -= HandleResize;
        }

        DestroyTextures();
    }
}
