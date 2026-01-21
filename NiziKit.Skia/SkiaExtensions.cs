using DenOfIz;
using SkiaSharp;

namespace NiziKit.Skia;

/// <summary>
/// Extension methods for Skia and DenOfIz interop.
/// </summary>
public static class SkiaExtensions
{
    /// <summary>
    /// Converts a DenOfIz Format to the corresponding Skia SKColorType.
    /// </summary>
    public static SKColorType ToSkiaColorType(this Format format)
    {
        return format switch
        {
            Format.B8G8R8A8Unorm => SKColorType.Bgra8888,
            Format.R8G8B8A8Unorm => SKColorType.Rgba8888,
            Format.R8Unorm => SKColorType.Gray8,
            Format.R16G16B16A16Float => SKColorType.RgbaF16,
            Format.R32G32B32A32Float => SKColorType.RgbaF32,
            _ => throw new ArgumentException($"Format {format} is not supported for Skia conversion", nameof(format))
        };
    }

    /// <summary>
    /// Converts a Skia SKColorType to the corresponding DenOfIz Format.
    /// </summary>
    public static Format ToDenOfIzFormat(this SKColorType colorType)
    {
        return colorType switch
        {
            SKColorType.Bgra8888 => Format.B8G8R8A8Unorm,
            SKColorType.Rgba8888 => Format.R8G8B8A8Unorm,
            SKColorType.Gray8 => Format.R8Unorm,
            SKColorType.RgbaF16 => Format.R16G16B16A16Float,
            SKColorType.RgbaF32 => Format.R32G32B32A32Float,
            _ => throw new ArgumentException($"SKColorType {colorType} is not supported for DenOfIz conversion", nameof(colorType))
        };
    }

    /// <summary>
    /// Creates a DenOfIz texture from an SKImage.
    /// </summary>
    public static Texture ToDenOfIzTexture(
        this SKImage image,
        LogicalDevice device,
        ResourceTracking resourceTracking,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        using var pixmap = image.PeekPixels()
            ?? throw new InvalidOperationException("Failed to get pixel data from SKImage");

        var format = pixmap.ColorType.ToDenOfIzFormat();

        var texture = device.CreateTexture(new TextureDesc
        {
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(debugName ?? $"SkiaImage_{image.Width}x{image.Height}")
        });

        resourceTracking.TrackTexture(texture, QueueType.Graphics);

        // Copy pixel data to byte array
        var pixelData = CopyPixmapToByteArray(pixmap);

        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });

        batchCopy.Begin();
        batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
        {
            Data = ByteArrayView.Create(pixelData),
            DstTexture = texture,
            AutoAlign = true,
            Width = (uint)image.Width,
            Height = (uint)image.Height,
            MipLevel = 0,
            ArrayLayer = 0
        });
        batchCopy.Submit(null);

        return texture;
    }

    /// <summary>
    /// Creates a DenOfIz texture from an SKBitmap.
    /// </summary>
    public static Texture ToDenOfIzTexture(
        this SKBitmap bitmap,
        LogicalDevice device,
        ResourceTracking resourceTracking,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        var format = bitmap.ColorType.ToDenOfIzFormat();

        var texture = device.CreateTexture(new TextureDesc
        {
            Width = (uint)bitmap.Width,
            Height = (uint)bitmap.Height,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(debugName ?? $"SkiaBitmap_{bitmap.Width}x{bitmap.Height}")
        });

        resourceTracking.TrackTexture(texture, QueueType.Graphics);

        // Copy pixel data to byte array
        var totalBytes = bitmap.RowBytes * bitmap.Height;
        var pixelData = new byte[totalBytes];
        unsafe
        {
            var srcPtr = (byte*)bitmap.GetPixels().ToPointer();
            fixed (byte* dstPtr = pixelData)
            {
                System.Buffer.MemoryCopy(srcPtr, dstPtr, totalBytes, totalBytes);
            }
        }

        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });

        batchCopy.Begin();
        batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
        {
            Data = ByteArrayView.Create(pixelData),
            DstTexture = texture,
            AutoAlign = true,
            Width = (uint)bitmap.Width,
            Height = (uint)bitmap.Height,
            MipLevel = 0,
            ArrayLayer = 0
        });
        batchCopy.Submit(null);

        return texture;
    }

    /// <summary>
    /// Creates a DenOfIz texture from a Skia surface snapshot.
    /// </summary>
    public static Texture ToDenOfIzTexture(
        this SKSurface surface,
        LogicalDevice device,
        ResourceTracking resourceTracking,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(surface);

        surface.Flush();
        using var image = surface.Snapshot();
        return image.ToDenOfIzTexture(device, resourceTracking, debugName);
    }

    /// <summary>
    /// Loads an image file using Skia and creates a DenOfIz texture.
    /// </summary>
    public static Texture LoadTextureFromFile(
        LogicalDevice device,
        string filePath,
        ResourceTracking resourceTracking)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        using var bitmap = SKBitmap.Decode(filePath)
            ?? throw new InvalidOperationException($"Failed to decode image file: {filePath}");

        return bitmap.ToDenOfIzTexture(device, resourceTracking, Path.GetFileName(filePath));
    }

    /// <summary>
    /// Loads an image from a stream using Skia and creates a DenOfIz texture.
    /// </summary>
    public static Texture LoadTextureFromStream(
        LogicalDevice device,
        Stream stream,
        ResourceTracking resourceTracking,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        using var bitmap = SKBitmap.Decode(stream)
            ?? throw new InvalidOperationException("Failed to decode image from stream");

        return bitmap.ToDenOfIzTexture(device, resourceTracking, debugName);
    }

    private static byte[] CopyPixmapToByteArray(SKPixmap pixmap)
    {
        var rowBytes = pixmap.RowBytes;
        var totalBytes = rowBytes * pixmap.Height;
        var pixelData = new byte[totalBytes];

        unsafe
        {
            var srcPtr = (byte*)pixmap.GetPixels().ToPointer();
            fixed (byte* dstPtr = pixelData)
            {
                System.Buffer.MemoryCopy(srcPtr, dstPtr, totalBytes, totalBytes);
            }
        }

        return pixelData;
    }
}
