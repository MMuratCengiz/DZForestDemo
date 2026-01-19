using DenOfIz;
using NiziKit.Graphics;

namespace DZForestDemo.Graphics;

public sealed class GridTexture : IDisposable
{
    public Texture Texture { get; }

    public GridTexture(
        int cellSize,
        int gridSize,
        byte bgR, byte bgG, byte bgB,
        byte lineR, byte lineG, byte lineB,
        int lineWidth = 1,
        string debugName = "GridTexture")
    {
        var size = cellSize * gridSize;
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var idx = (y * size + x) * 4;
                var onLineX = (x % cellSize) < lineWidth;
                var onLineY = (y % cellSize) < lineWidth;

                if (onLineX || onLineY)
                {
                    pixels[idx + 0] = lineR;
                    pixels[idx + 1] = lineG;
                    pixels[idx + 2] = lineB;
                    pixels[idx + 3] = 255;
                }
                else
                {
                    pixels[idx + 0] = bgR;
                    pixels[idx + 1] = bgG;
                    pixels[idx + 2] = bgB;
                    pixels[idx + 3] = 255;
                }
            }
        }

        var device = GraphicsContext.Device;
        Texture = device.CreateTexture(new TextureDesc
        {
            Width = (uint)size,
            Height = (uint)size,
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
            Data = ByteArrayView.Create(pixels),
            DstTexture = Texture,
            AutoAlign = false,
            Width = (uint)size,
            Height = (uint)size,
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