using DenOfIz;
using NiziKit.Graphics;
using SkiaSharp;

namespace NiziKit.Skia;

/// <summary>
/// A render target that allows Skia to render directly to a DenOfIz texture.
/// DenOfIz owns the texture, Skia renders to it via native handle sharing.
/// </summary>
public sealed class SkiaRenderTarget : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly ResourceTracking _resourceTracking;
    private readonly GRContext _grContext;

    private Texture _texture;
    private GRBackendRenderTarget _backendRenderTarget;
    private SKSurface _surface;
    private bool _disposed;

    /// <summary>
    /// The DenOfIz texture that Skia renders to.
    /// Can be used directly in DenOfIz rendering pipelines.
    /// </summary>
    public Texture Texture => _texture;

    /// <summary>
    /// The Skia surface for drawing operations.
    /// </summary>
    public SKSurface Surface => _surface;

    /// <summary>
    /// The Skia canvas for drawing.
    /// </summary>
    public SKCanvas Canvas => _surface.Canvas;

    /// <summary>
    /// Width in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Creates a new SkiaRenderTarget with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="sampleCount">MSAA sample count. Defaults to 1.</param>
    public SkiaRenderTarget(int width, int height, int sampleCount = 1)
        : this(width, height, SkiaContext.GRContext, GraphicsContext.Device, GraphicsContext.ResourceTracking, sampleCount)
    {
    }

    /// <summary>
    /// Creates a new SkiaRenderTarget with explicit dependencies.
    /// </summary>
    public SkiaRenderTarget(
        int width,
        int height,
        GRContext grContext,
        LogicalDevice device,
        ResourceTracking resourceTracking,
        int sampleCount = 1)
    {
        ArgumentNullException.ThrowIfNull(grContext);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        _grContext = grContext;
        _device = device;
        _resourceTracking = resourceTracking;
        Width = width;
        Height = height;

        CreateResources(sampleCount);
    }

    private void CreateResources(int sampleCount)
    {
        // 1. Create DenOfIz texture with RenderAttachment usage
        _texture = _device.CreateTexture(new TextureDesc
        {
            Width = (uint)Width,
            Height = (uint)Height,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8Unorm,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create($"SkiaRenderTarget_{Width}x{Height}")
        });

        _resourceTracking.TrackTexture(_texture, QueueType.Graphics);

        // 2. Get native texture handle
        var nativeHandles = NativeInterop.GetNativeTextureHandles(_texture);

        // 3. Create Skia backend render target from native handle
        _backendRenderTarget = CreateBackendRenderTarget(nativeHandles, sampleCount);

        // 4. Create Skia surface that renders to the backend render target
        _surface = SKSurface.Create(
            _grContext,
            _backendRenderTarget,
            GRSurfaceOrigin.TopLeft,
            SKColorType.Bgra8888);

        if (_surface == null)
        {
            throw new InvalidOperationException(
                $"Failed to create Skia surface for render target ({Width}x{Height}). " +
                "Ensure the GRContext backend matches the texture backend.");
        }
    }

    private GRBackendRenderTarget CreateBackendRenderTarget(NativeTextureHandles handles, int sampleCount)
    {
        return handles.Backend switch
        {
            GraphicsBackendType.Metal => CreateMetalBackendRenderTarget(handles, sampleCount),
            GraphicsBackendType.Vulkan => CreateVulkanBackendRenderTarget(handles, sampleCount),
            _ => throw new NotSupportedException($"Backend {handles.Backend} is not supported for Skia render targets")
        };
    }

    private GRBackendRenderTarget CreateMetalBackendRenderTarget(NativeTextureHandles handles, int sampleCount)
    {
        if (handles.MTLTexture == IntPtr.Zero)
        {
            throw new InvalidOperationException("Metal texture handle is null");
        }

        var mtlTextureInfo = new GRMtlTextureInfo(handles.MTLTexture);
        // Metal constructor: (width, height, mtlTextureInfo) - no sample count parameter
        return new GRBackendRenderTarget(Width, Height, mtlTextureInfo);
    }

    private GRBackendRenderTarget CreateVulkanBackendRenderTarget(NativeTextureHandles handles, int sampleCount)
    {
        if (handles.VkImage == IntPtr.Zero)
        {
            throw new InvalidOperationException("Vulkan image handle is null");
        }

        var vkImageInfo = new GRVkImageInfo
        {
            Image = (ulong)handles.VkImage,
            ImageLayout = handles.VkImageLayout,
            Format = handles.VkFormat,
            LevelCount = 1,
            CurrentQueueFamily = 0, // VK_QUEUE_FAMILY_IGNORED
            ImageTiling = 0, // VK_IMAGE_TILING_OPTIMAL
            Protected = false
        };

        return new GRBackendRenderTarget(Width, Height, vkImageInfo);
    }

    /// <summary>
    /// Flushes pending Skia operations to the texture.
    /// Call this after drawing before using the texture in DenOfIz.
    /// </summary>
    public void Flush()
    {
        ThrowIfDisposed();
        _surface.Flush();
        _grContext.Flush();
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    public void Clear(SKColor color)
    {
        ThrowIfDisposed();
        Canvas.Clear(color);
    }

    /// <summary>
    /// Resizes the render target. Previous content is lost.
    /// </summary>
    public void Resize(int newWidth, int newHeight, int sampleCount = 1)
    {
        ThrowIfDisposed();

        if (newWidth == Width && newHeight == Height)
        {
            return;
        }

        // Dispose old resources
        _surface.Dispose();
        _backendRenderTarget.Dispose();
        _resourceTracking.UntrackTexture(_texture);
        _texture.Dispose();

        Width = newWidth;
        Height = newHeight;

        CreateResources(sampleCount);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _surface.Dispose();
        _backendRenderTarget.Dispose();
        _resourceTracking.UntrackTexture(_texture);
        _texture.Dispose();
    }
}
