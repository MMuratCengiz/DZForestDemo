using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Provides GPU graphics context for Avalonia using DenOfIz's graphics backend.
/// </summary>
public sealed class DenOfIzPlatformGraphics : IPlatformGraphics
{
    private readonly DenOfIzSkiaGpu _skiaGpu = new();

    public bool UsesSharedContext => true;

    public IPlatformGraphicsContext GetSharedContext()
    {
        return _skiaGpu;
    }

    public IPlatformGraphicsContext CreateContext()
    {
        return _skiaGpu;
    }

    public bool UsesContexts => true;
    public DenOfIzSkiaGpu SkiaGpu => _skiaGpu;
}

public sealed class DenOfIzSkiaGpu : IPlatformGraphicsContext, ISkiaGpu
{
    private bool _isDisposed;

    public bool IsLost => _isDisposed;

    public IDisposable EnsureCurrent() => EmptyDisposable.Instance;

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ISkiaGpu))
        {
            return this;
        }
        return null;
    }

    public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface is DenOfIzSkiaSurface denOfIzSurface)
            {
                return new DenOfIzSkiaGpuRenderTarget(denOfIzSurface);
            }
        }
        return null;
    }

    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session)
    {
        return null;
    }

    public DenOfIzSkiaSurface CreateSurface(PixelSize size, double scaling)
    {
        return new DenOfIzSkiaSurface(size, scaling);
    }

    public void Dispose()
    {
        _isDisposed = true;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Render target for Avalonia to render into a DenOfIz texture.
/// </summary>
internal sealed class DenOfIzSkiaGpuRenderTarget(DenOfIzSkiaSurface surface) : ISkiaGpuRenderTarget
{
    public bool IsCorrupted => false;

    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        return new DenOfIzSkiaGpuRenderSession(surface);
    }

    public void Dispose() { }
}

/// <summary>
/// Render session for a single frame.
/// </summary>
internal sealed class DenOfIzSkiaGpuRenderSession(DenOfIzSkiaSurface surface) : ISkiaGpuRenderSession
{
    public GRContext GrContext => SkiaContext.GRContext;

    public SKSurface SkSurface => surface.RenderTarget.Surface;

    public double ScaleFactor => surface.Scaling;

    public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;

    public PixelSize Size => new(surface.RenderTarget.Width, surface.RenderTarget.Height);

    public bool IsYFlipped => false;

    public void Dispose()
    {
        surface.RenderTarget.Flush();
    }
}
