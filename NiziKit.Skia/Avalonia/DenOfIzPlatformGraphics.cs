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
    private readonly DenOfIzSkiaGpu _skiaGpu;

    public DenOfIzPlatformGraphics()
    {
        _skiaGpu = new DenOfIzSkiaGpu();
    }

    public bool UsesSharedContext => true;

    public IPlatformGraphicsContext GetSharedContext()
    {
        Console.WriteLine("[DEBUG] GetSharedContext called");
        return _skiaGpu;
    }

    public IPlatformGraphicsContext CreateContext()
    {
        Console.WriteLine("[DEBUG] CreateContext called");
        return _skiaGpu;
    }

    public bool UsesContexts => true;

    /// <summary>
    /// Gets the shared ISkiaGpu instance.
    /// </summary>
    public DenOfIzSkiaGpu SkiaGpu => _skiaGpu;
}

/// <summary>
/// Skia GPU implementation for DenOfIz - serves as both IPlatformGraphicsContext and ISkiaGpu.
/// </summary>
public sealed class DenOfIzSkiaGpu : IPlatformGraphicsContext, ISkiaGpu
{
    private bool _isDisposed;

    // IPlatformGraphicsContext implementation
    public bool IsLost => _isDisposed;

    public IDisposable EnsureCurrent() => EmptyDisposable.Instance;

    public object? TryGetFeature(Type featureType)
    {
        Console.WriteLine($"[DEBUG] TryGetFeature called for: {featureType.Name}");
        if (featureType == typeof(ISkiaGpu))
        {
            Console.WriteLine("[DEBUG] Returning ISkiaGpu (this)");
            return this;
        }
        return null;
    }

    // ISkiaGpu implementation
    public ISkiaGpuRenderTarget? TryCreateRenderTarget(IEnumerable<object> surfaces)
    {
        Console.WriteLine("[DEBUG] TryCreateRenderTarget called");
        foreach (var surface in surfaces)
        {
            Console.WriteLine($"[DEBUG] Surface type: {surface.GetType().Name}");
            if (surface is DenOfIzSkiaSurface denOfIzSurface)
            {
                Console.WriteLine("[DEBUG] Found DenOfIzSkiaSurface, creating render target");
                return new DenOfIzSkiaGpuRenderTarget(denOfIzSurface);
            }
        }
        Console.WriteLine("[DEBUG] No suitable surface found");
        return null;
    }

    public ISkiaSurface? TryCreateSurface(PixelSize size, ISkiaGpuRenderSession? session)
    {
        return null;
    }

    /// <summary>
    /// Creates a surface for rendering. Called by TopLevelImpl.
    /// </summary>
    public DenOfIzSkiaSurface CreateSurface(PixelSize size, double scaling)
    {
        Console.WriteLine($"[DEBUG] CreateSurface called: {size.Width}x{size.Height} @ {scaling}x");
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
        Console.WriteLine("[DEBUG] BeginRenderingSession called");
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
