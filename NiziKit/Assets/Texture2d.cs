using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;
using BinaryReader = DenOfIz.BinaryReader;

namespace NiziKit.Assets;

public class Texture2d
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
        var bytes = Content.ReadBytes(path);
        LoadFromBytes(path, bytes);
    }

    public async Task LoadAsync(string path, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync(path, ct);
        LoadFromBytes(path, bytes);
    }

    public void LoadFromBytes(string path, byte[] bytes)
    {
        if (IsDzTex(path))
        {
            LoadFromDzTex(bytes, path);
        }
        else
        {
            LoadFromBytes(GetTextureExtension(path), bytes, path);
        }
    }

    private static bool IsDzTex(string path)
    {
        return Path.GetExtension(path).Equals(".dztex", StringComparison.OrdinalIgnoreCase);
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

        lock (GraphicsContext.GpuLock)
        {
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
    }

    private void LoadFromDzTex(byte[] bytes, string name)
    {
        var device = GraphicsContext.Device;

        using var reader = BinaryReader.CreateFromData(
            ByteArrayView.Create(bytes), new BinaryReaderDesc { NumBytes = 0 });
        using var assetReader = new TextureAssetReader(new TextureAssetReaderDesc { Reader = reader });
        using var textureAsset = assetReader.Read();

        var width = textureAsset.Width();
        var height = textureAsset.Height();
        var depth = Math.Max(1u, textureAsset.Depth());
        var mipLevels = textureAsset.MipLevels();
        var arraySize = textureAsset.ArraySize();
        var format = textureAsset.GetFormat();

        lock (GraphicsContext.GpuLock)
        {
            var gpuTexture = device.CreateTexture(new TextureDesc
            {
                Width = width,
                Height = height,
                Depth = depth,
                MipLevels = mipLevels,
                ArraySize = arraySize,
                Format = format,
                Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
                DebugName = StringView.Create(Path.GetFileNameWithoutExtension(name))
            });

            using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = device,
                IssueBarriers = true
            });
            batchCopy.Begin();

            var mips = textureAsset.Mips().ToArray();
            for (var i = 0; i < mips.Length; i++)
            {
                var mip = mips[i];
                var mipData = assetReader.ReadRaw(mip.MipIndex, mip.ArrayIndex);
                var mipBytes = new byte[mipData.NumElements];
                Marshal.Copy(mipData.Elements, mipBytes, 0, (int)mipData.NumElements);

                batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
                {
                    Data = ByteArrayView.Create(mipBytes),
                    DstTexture = gpuTexture,
                    Width = mip.Width,
                    Height = mip.Height,
                    MipLevel = mip.MipIndex,
                    ArrayLayer = mip.ArrayIndex,
                    AutoAlign = true
                });
            }

            batchCopy.Submit(null);

            GraphicsContext.ResourceTracking.TrackTexture(gpuTexture, QueueType.Graphics);
            Name = Path.GetFileNameWithoutExtension(name);
            SourcePath = name;
            Width = width;
            Height = height;
            MipLevels = mipLevels;
            Format = format;
            GpuTexture = gpuTexture;
        }
    }

    public void Dispose()
    {
        GpuTexture.Dispose();
    }
}
