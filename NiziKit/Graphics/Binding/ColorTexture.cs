using DenOfIz;
using NiziKit.Core;

namespace NiziKit.Graphics.Binding;

public sealed class ColorTexture : IDisposable
{
    private static readonly Lazy<ColorTexture> _empty = new(() =>
    {
        var texture = new ColorTexture(0, 0, 0, 0, "EmptyTexture");
        Disposer.Register(texture);
        return texture;
    });

    private static readonly Lazy<ColorTexture> _missing = new(() =>
    {
        var texture = new ColorTexture(255, 0, 255, 255, "MissingTexture");
        Disposer.Register(texture);
        return texture;
    });

    public static ColorTexture Empty => _empty.Value;
    public static ColorTexture Missing => _missing.Value;

    public Texture Texture { get; }

    public ColorTexture(byte r, byte g, byte b, byte a, string debugName)
    {
        var device = GraphicsContext.Device;
        Texture = device.CreateTexture(new TextureDesc
        {
            Width = 1,
            Height = 1,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8Unorm,
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(debugName)
        });

        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });
        batchCopy.Begin();
        batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
        {
            Data = ByteArrayView.Create([r, g, b, a]),
            DstTexture = Texture,
            AutoAlign = false,
            Width = 1,
            Height = 1,
            MipLevel = 0,
            ArrayLayer = 0
        });
        batchCopy.Submit(null);

        GraphicsContext.ResourceTracking.TrackTexture(Texture, QueueType.Graphics);
    }

    public void Dispose()
    {
        Texture.Dispose();
    }
}
