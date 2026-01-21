using Avalonia;
using DenOfIz;

namespace NiziKit.Skia.Avalonia;

public sealed class DenOfIzSkiaSurface(int width, int height, double scaling = 1.0) : IDisposable
{
    private readonly SkiaRenderTarget _renderTarget = new(width, height);
    private bool _disposed;

    public SkiaRenderTarget RenderTarget => _renderTarget;

    public Texture Texture => _renderTarget.Texture;

    public int Width => _renderTarget.Width;

    public int Height => _renderTarget.Height;

    public double Scaling { get; private set; } = scaling;

    public DenOfIzSkiaSurface(PixelSize size, double scaling = 1.0)
        : this(size.Width, size.Height, scaling)
    {
    }

    public void Resize(int width, int height, double scaling)
    {
        ThrowIfDisposed();

        if (width == Width && height == Height && Math.Abs(scaling - Scaling) < 0.001)
        {
            return;
        }

        _renderTarget.Resize(width, height);
        Scaling = scaling;
    }

    public void Resize(PixelSize size, double scaling)
        => Resize(size.Width, size.Height, scaling);

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

        _renderTarget.Dispose();
    }
}
