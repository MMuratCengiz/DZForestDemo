using Avalonia;
using DenOfIz;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Wraps a SkiaRenderTarget for use with Avalonia's rendering system.
/// This is passed to Avalonia as a surface to render into.
/// </summary>
public sealed class DenOfIzSkiaSurface : IDisposable
{
    private SkiaRenderTarget _renderTarget;
    private bool _disposed;

    /// <summary>
    /// The underlying SkiaRenderTarget that Avalonia renders to.
    /// </summary>
    public SkiaRenderTarget RenderTarget => _renderTarget;

    /// <summary>
    /// The DenOfIz texture containing the rendered Avalonia UI.
    /// Use this in your rendering pipeline to display the UI.
    /// </summary>
    public Texture Texture => _renderTarget.Texture;

    /// <summary>
    /// Width in pixels.
    /// </summary>
    public int Width => _renderTarget.Width;

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public int Height => _renderTarget.Height;

    /// <summary>
    /// The DPI scaling factor.
    /// </summary>
    public double Scaling { get; private set; }

    /// <summary>
    /// Creates a new DenOfIzSkiaSurface with the specified dimensions.
    /// </summary>
    public DenOfIzSkiaSurface(int width, int height, double scaling = 1.0)
    {
        _renderTarget = new SkiaRenderTarget(width, height);
        Scaling = scaling;
    }

    /// <summary>
    /// Creates a new DenOfIzSkiaSurface from a pixel size.
    /// </summary>
    public DenOfIzSkiaSurface(PixelSize size, double scaling = 1.0)
        : this(size.Width, size.Height, scaling)
    {
    }

    /// <summary>
    /// Resizes the surface. Previous content is lost.
    /// </summary>
    public void Resize(int width, int height, double scaling)
    {
        ThrowIfDisposed();

        if (width == Width && height == Height && Math.Abs(scaling - Scaling) < 0.001)
            return;

        _renderTarget.Resize(width, height);
        Scaling = scaling;
    }

    /// <summary>
    /// Resizes the surface using PixelSize.
    /// </summary>
    public void Resize(PixelSize size, double scaling)
        => Resize(size.Width, size.Height, scaling);

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _renderTarget.Dispose();
    }
}
