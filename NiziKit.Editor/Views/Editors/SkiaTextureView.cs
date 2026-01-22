using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Graphics.Resources;
using NiziKit.Skia;
using SkiaSharp;

namespace NiziKit.Editor.Views.Editors;

public class SkiaTextureView : Control
{
    private CycledTexture? _sourceTexture;

    public static readonly StyledProperty<CycledTexture?> SourceTextureProperty =
        AvaloniaProperty.Register<SkiaTextureView, CycledTexture?>(nameof(SourceTexture));

    public CycledTexture? SourceTexture
    {
        get => GetValue(SourceTextureProperty);
        set => SetValue(SourceTextureProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceTextureProperty)
        {
            _sourceTexture = change.GetNewValue<CycledTexture?>();
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        if (_sourceTexture == null)
        {
            return;
        }

        var width = (int)_sourceTexture.Width;
        var height = (int)_sourceTexture.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var frameIndex = GraphicsContext.FrameIndex;
        var texture = _sourceTexture[frameIndex];

        context.Custom(new TextureDrawOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            texture,
            width,
            height));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_sourceTexture != null)
        {
            return new Size(_sourceTexture.Width, _sourceTexture.Height);
        }
        return base.MeasureOverride(availableSize);
    }

    private class TextureDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly Texture _texture;
        private readonly int _width;
        private readonly int _height;

        public TextureDrawOperation(Rect bounds, Texture texture, int width, int height)
        {
            _bounds = bounds;
            _texture = texture;
            _width = width;
            _height = height;
        }

        public Rect Bounds => _bounds;

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => _bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
            {
                return;
            }

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            if (canvas == null)
            {
                return;
            }

            var backendTexture = CreateBackendTexture(_texture, _width, _height);
            if (backendTexture == null)
            {
                return;
            }

            var grContext = lease.GrContext ?? SkiaContext.GRContext;

            using (backendTexture)
            {
                using var skImage = SKImage.FromTexture(
                    grContext,
                    backendTexture,
                    GRSurfaceOrigin.TopLeft,
                    SKColorType.Rgba8888,
                    SKAlphaType.Premul);

                if (skImage == null)
                {
                    return;
                }

                var srcRect = new SKRect(0, 0, _width, _height);
                var destRect = new SKRect(
                    (float)_bounds.X,
                    (float)_bounds.Y,
                    (float)(_bounds.X + _bounds.Width),
                    (float)(_bounds.Y + _bounds.Height));

                canvas.DrawImage(skImage, srcRect, destRect);
            }
        }

        private static GRBackendTexture? CreateBackendTexture(Texture texture, int width, int height)
        {
            var nativeHandles = NativeInterop.GetNativeTextureHandles(texture);

            return nativeHandles.Backend switch
            {
                GraphicsBackendType.Metal => CreateMetalBackendTexture(nativeHandles, width, height),
                GraphicsBackendType.Vulkan => CreateVulkanBackendTexture(nativeHandles, width, height),
                GraphicsBackendType.Directx12 => CreateD3D12BackendTexture(nativeHandles, width, height),
                _ => null
            };
        }

        private static GRBackendTexture CreateMetalBackendTexture(NativeTextureHandles handles, int width, int height)
        {
            var mtlTextureInfo = new GRMtlTextureInfo(handles.MTLTexture);
            return new GRBackendTexture(width, height, false, mtlTextureInfo);
        }

        private static GRBackendTexture CreateVulkanBackendTexture(NativeTextureHandles handles, int width, int height)
        {
            var vkFormat = handles.VkFormat != 0 ? handles.VkFormat : VulkanInterop.FormatToVulkan(Format.R8G8B8A8Unorm);

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
                SampleCount = 1,
                LevelCount = 1,
                CurrentQueueFamily = VulkanInterop.VK_QUEUE_FAMILY_IGNORED,
                SharingMode = VulkanInterop.VK_SHARING_MODE_EXCLUSIVE,
                Protected = false
            };

            return new GRBackendTexture(width, height, vkImageInfo);
        }

        private static GRBackendTexture CreateD3D12BackendTexture(NativeTextureHandles handles, int width, int height)
        {
            var d3dTextureInfo = new GRD3DTextureResourceInfo
            {
                Resource = handles.DX12Resource,
                ResourceState = Direct3DInterop.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
                Format = Direct3DInterop.FormatToDxgi(Format.R8G8B8A8Unorm),
                SampleCount = 1,
                LevelCount = 1,
                SampleQualityPattern = 0,
                Protected = false
            };

            return new GRBackendTexture(width, height, d3dTextureInfo);
        }
    }
}
