using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.Graphics.Binding;

public sealed class ColorTexture : IDisposable
{
    public Texture Texture { get; }

    public ColorTexture(LogicalDevice device, byte r, byte g, byte b, byte a, string debugName)
    {
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
        UploadPixelData(device, [r, g, b, a], debugName);
    }

    private void UploadPixelData(LogicalDevice device, byte[] pixelData, string debugName)
    {
        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });
        batchCopy.Begin();
        batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
        {
            Data = ByteArrayView.Create(pixelData),
            DstTexture = Texture,
            AutoAlign = true,
            Width = 1,
            Height = 1,
            MipLevel = 0,
            ArrayLayer = 0
        });
        batchCopy.Submit(null);
    }

    public void Dispose()
    {
        Texture.Dispose();
    }
}