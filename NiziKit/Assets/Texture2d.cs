using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class Texture2d : IAsset
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint MipLevels { get; set; }
    public Format Format { get; set; }
    public Texture GpuTexture { get; set; } = null!;

    internal Texture2d()
    {
    }

    public void Load(GraphicsContext context, string path)
    {
        var resolvedPath = AssetPaths.ResolveTexture(path);
        var device = context.LogicalDevice;

        var textureDataDesc = new TextureCreateFromPathDesc
        {
            Path = StringView.Create(resolvedPath)
        };
        using var textureData = TextureData.CreateFromPath(textureDataDesc);

        var gpuTexture = device.CreateTexture(new TextureDesc
        {
            Width = textureData.GetWidth(),
            Height = textureData.GetHeight(),
            Depth = Math.Max(1u, textureData.GetDepth()),
            MipLevels = textureData.GetMipLevels(),
            ArraySize = textureData.GetArraySize(),
            Format = textureData.GetFormat(),
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(Path.GetFileNameWithoutExtension(resolvedPath))
        });

        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = false
        });
        batchCopy.Begin();
        batchCopy.LoadTexture(new LoadTextureDesc
        {
            File = StringView.Create(resolvedPath),
            DstTexture = gpuTexture
        });
        batchCopy.Submit(null);

        Name = Path.GetFileNameWithoutExtension(resolvedPath);
        SourcePath = resolvedPath;
        Width = textureData.GetWidth();
        Height = textureData.GetHeight();
        MipLevels = textureData.GetMipLevels();
        Format = textureData.GetFormat();
        GpuTexture = gpuTexture;
    }

    public void Dispose()
    {
        GpuTexture.Dispose();
    }
}
