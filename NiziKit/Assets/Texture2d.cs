using DenOfIz;
using NiziKit.ContentPipeline;
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

    public Texture2d()
    {
    }

    public void Load(string path)
    {
        var bytes = Content.ReadBytes($"Textures/{path}");
        LoadFromBytes(GetTextureExtension(path), bytes, path);
    }

    public async Task LoadAsync(string path, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync($"Textures/{path}", ct);
        LoadFromBytes(GetTextureExtension(path), bytes, path);
    }

    private static TextureExtension GetTextureExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".dds" => TextureExtension.Dds,
            ".png" => TextureExtension.Png,
            ".jpg" or ".jpeg" => TextureExtension.Jpg,
            ".bmp" => TextureExtension.Bmp,
            ".tga" => TextureExtension.Tga,
            ".hdr" => TextureExtension.Hdr,
            ".gif" => TextureExtension.Gif,
            ".pic" => TextureExtension.Pic,
            _ => throw new ArgumentException($"Unsupported texture extension: {ext}", nameof(path))
        };
    }

    public void LoadFromBytes(TextureExtension extension, byte[] bytes, string name)
    {
        var device = GraphicsContext.Device;

        var textureDataDesc = new TextureCreateFromDataDesc
        {
            Data = ByteArrayView.Create(bytes),
            Extension = extension
        };
        using var textureData = TextureData.CreateFromData(textureDataDesc);

        var gpuTexture = device.CreateTexture(new TextureDesc
        {
            Width = textureData.GetWidth(),
            Height = textureData.GetHeight(),
            Depth = Math.Max(1u, textureData.GetDepth()),
            MipLevels = textureData.GetMipLevels(),
            ArraySize = textureData.GetArraySize(),
            Format = textureData.GetFormat(),
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(Path.GetFileNameWithoutExtension(name))
        });

        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });
        batchCopy.Begin();
        batchCopy.LoadTextureFromData(new LoadTextureFromDataDesc
        {
            TextureData = textureData,
            DstTexture = gpuTexture
        });
        batchCopy.Submit(null);

        GraphicsContext.ResourceTracking.TrackTexture(gpuTexture, QueueType.Graphics);
        Name = Path.GetFileNameWithoutExtension(name);
        SourcePath = name;
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
