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
    private DenOfIzRenderTimer? _renderTimer;

    public bool UsesSharedContext => true;

    public IPlatformGraphicsContext GetSharedContext()
        => new DenOfIzGraphicsContext();

    public IPlatformGraphicsContext CreateContext()
        => GetSharedContext();

    public bool UsesContexts => true;

    internal void TriggerRenderTick(TimeSpan elapsed)
    {
        _renderTimer?.TriggerTick(elapsed);
    }

    internal void SetRenderTimer(DenOfIzRenderTimer timer)
    {
        _renderTimer = timer;
    }
}

/// <summary>
/// Graphics context wrapper for Avalonia.
/// </summary>
internal sealed class DenOfIzGraphicsContext : IPlatformGraphicsContext
{
    public bool IsLost => false;

    public IDisposable EnsureCurrent()
        => new EmptyDisposable();

    public object? TryGetFeature(Type featureType)
    {
        if (featureType == typeof(ISkiaGpu))
        {
            return new DenOfIzSkiaGpu();
        }
        return null;
    }

    public void Dispose() { }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Skia GPU interface for Avalonia integration.
/// </summary>
internal sealed class DenOfIzSkiaGpu : ISkiaGpu
{
    public bool IsLost => false;

    public IDisposable EnsureCurrent() => new EmptyDisposable();

    public object? TryGetFeature(Type featureType) => null;

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

    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession session)
    {
        return null;
    }

    public void Dispose() { }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Render target for Avalonia to render into a DenOfIz texture.
/// </summary>
internal sealed class DenOfIzSkiaGpuRenderTarget : ISkiaGpuRenderTarget
{
    private readonly DenOfIzSkiaSurface _surface;

    public DenOfIzSkiaGpuRenderTarget(DenOfIzSkiaSurface surface)
    {
        _surface = surface;
    }

    public bool IsCorrupted => false;

    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        return new DenOfIzSkiaGpuRenderSession(_surface);
    }

    public void Dispose() { }
}

/// <summary>
/// Render session for a single frame.
/// </summary>
internal sealed class DenOfIzSkiaGpuRenderSession : ISkiaGpuRenderSession
{
    private readonly DenOfIzSkiaSurface _surface;

    public DenOfIzSkiaGpuRenderSession(DenOfIzSkiaSurface surface)
    {
        _surface = surface;
    }

    public GRContext GrContext => SkiaContext.GRContext;

    public SKSurface SkSurface => _surface.RenderTarget.Surface;

    public double ScaleFactor => _surface.Scaling;

    public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;

    public PixelSize Size => new(_surface.RenderTarget.Width, _surface.RenderTarget.Height);

    public bool IsYFlipped => false;

    public void Dispose()
    {
        _surface.RenderTarget.Flush();
    }
}
