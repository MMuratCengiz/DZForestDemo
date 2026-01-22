using DenOfIz;
using NiziKit.Graphics;
using SkiaSharp;

namespace NiziKit.Skia;

public sealed class SkiaRenderTarget : IDisposable
{
    private const Format TextureFormat = Format.R8G8B8A8Unorm;

    private readonly LogicalDevice _device;
    private readonly ResourceTracking _resourceTracking;
    private readonly GRContext _grContext;

    private Texture _texture = null!;
    private GRBackendRenderTarget _backendRenderTarget = null!;
    private SKSurface _surface = null!;
    private bool _disposed;

    public Texture Texture => _texture;
    public SKSurface Surface => _surface;
    public SKCanvas Canvas => _surface.Canvas;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public SkiaRenderTarget(int width, int height, int sampleCount = 1)
        : this(width, height, SkiaContext.GRContext, GraphicsContext.Device, GraphicsContext.ResourceTracking,
            sampleCount)
    {
    }

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
        _texture = _device.CreateTexture(new TextureDesc
        {
            Width = (uint)Width,
            Height = (uint)Height,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = TextureFormat,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment |
                           TextureUsageFlagBits.TextureBinding |
                           TextureUsageFlagBits.CopySrc |
                           TextureUsageFlagBits.CopyDst),
            DebugName = StringView.Create($"SkiaRenderTarget_{Width}x{Height}")
        });

        _resourceTracking.TrackTexture(_texture, QueueType.Graphics);
        SkiaContext.Instance.TransitionTextureForRendering(_texture, _resourceTracking);

        var nativeHandles = NativeInterop.GetNativeTextureHandles(_texture);
        _backendRenderTarget = CreateBackendRenderTarget(nativeHandles, TextureFormat, sampleCount);

        _surface = SKSurface.Create(
            _grContext,
            _backendRenderTarget,
            GRSurfaceOrigin.TopLeft,
            SKColorType.Rgba8888,
            new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

        if (_surface == null)
        {
            throw new InvalidOperationException(
                $"Failed to create Skia surface for render target ({Width}x{Height}). " +
                $"GRContext backend: {_grContext.Backend}, BackendRenderTarget backend: {_backendRenderTarget.Backend}, " +
                $"BackendRenderTarget valid: {_backendRenderTarget.IsValid}");
        }

        _surface.Canvas.Clear(SKColors.Transparent);
        _surface.Flush();
        _grContext.Flush();
    }

    private GRBackendRenderTarget CreateBackendRenderTarget(NativeTextureHandles handles, Format format,
        int sampleCount)
    {
        return handles.Backend switch
        {
            GraphicsBackendType.Metal => CreateMetalBackendRenderTarget(handles),
            GraphicsBackendType.Vulkan => CreateVulkanBackendRenderTarget(handles, format, sampleCount),
            GraphicsBackendType.Directx12 => CreateDirect3DBackendRenderTarget(handles, format, sampleCount),
            _ => throw new NotSupportedException($"Backend {handles.Backend} is not supported for Skia render targets. " +
                "Supported backends: Metal, Vulkan, D3D12.")
        };
    }

    private GRBackendRenderTarget CreateMetalBackendRenderTarget(NativeTextureHandles handles)
    {
        if (handles.MTLTexture == IntPtr.Zero)
        {
            throw new InvalidOperationException("Metal texture handle is null");
        }

        var mtlTextureInfo = new GRMtlTextureInfo(handles.MTLTexture);
        return new GRBackendRenderTarget(Width, Height, mtlTextureInfo);
    }

    private GRBackendRenderTarget CreateVulkanBackendRenderTarget(NativeTextureHandles handles, Format format,
        int sampleCount)
    {
        if (handles.VkImage == IntPtr.Zero)
        {
            throw new InvalidOperationException("Vulkan image handle is null");
        }

        var vkFormat = handles.VkFormat != 0 ? handles.VkFormat : VulkanInterop.FormatToVulkan(format);

        var vkImageInfo = new GRVkImageInfo
        {
            Image = (ulong)handles.VkImage,
            Format = vkFormat,
            ImageLayout = handles.VkImageLayout,
            ImageTiling = VulkanInterop.VK_IMAGE_TILING_OPTIMAL,
            ImageUsageFlags = VulkanInterop.VK_IMAGE_USAGE_SAMPLED_BIT |
                              VulkanInterop.VK_IMAGE_USAGE_TRANSFER_SRC_BIT |
                              VulkanInterop.VK_IMAGE_USAGE_TRANSFER_DST_BIT |
                              VulkanInterop.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT,
            SampleCount = (uint)sampleCount,
            LevelCount = 1,
            CurrentQueueFamily = VulkanInterop.VK_QUEUE_FAMILY_IGNORED,
            SharingMode = VulkanInterop.VK_SHARING_MODE_EXCLUSIVE,
            Protected = false
        };

        return new GRBackendRenderTarget(Width, Height, vkImageInfo);
    }

    private GRBackendRenderTarget CreateDirect3DBackendRenderTarget(NativeTextureHandles handles, Format format,
        int sampleCount)
    {
        if (handles.DX12Resource == IntPtr.Zero)
        {
            throw new InvalidOperationException("D3D12 resource handle is null");
        }

        var d3dTextureInfo = new GRD3DTextureResourceInfo
        {
            Resource = handles.DX12Resource,
            ResourceState = Direct3DInterop.D3D12_RESOURCE_STATE_RENDER_TARGET,
            Format = Direct3DInterop.FormatToDxgi(format),
            SampleCount = (uint)sampleCount,
            LevelCount = 1,
            SampleQualityPattern = 0,
            Protected = false
        };

        return new GRBackendRenderTarget(Width, Height, d3dTextureInfo);
    }

    public void Flush()
    {
        ThrowIfDisposed();
        _surface.Flush();
        _grContext.Flush();
    }

    public void Clear(SKColor color)
    {
        ThrowIfDisposed();
        Canvas.Clear(color);
    }

    public void Resize(int newWidth, int newHeight, int sampleCount = 1)
    {
        ThrowIfDisposed();

        if (newWidth == Width && newHeight == Height)
        {
            return;
        }

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
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _surface.Dispose();
        _backendRenderTarget.Dispose();
        _resourceTracking.UntrackTexture(_texture);
        _texture.Dispose();
    }
}
