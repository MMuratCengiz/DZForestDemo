using Avalonia;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using DenOfIz;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

public sealed class DenOfIzTopLevel : EmbeddableControlRoot
{
    private readonly DenOfIzTopLevelImpl _impl;

    public Texture? Texture => _impl.Surface?.Texture;

    public DenOfIzSkiaSurface? Surface => _impl.Surface;

    public int PixelWidth => _impl.Surface?.Width ?? 0;

    public int PixelHeight => _impl.Surface?.Height ?? 0;

    public DenOfIzTopLevel(int width, int height, double scaling = 1.0)
        : this(new DenOfIzTopLevelImpl())
    {
        _impl.SetRenderSize(width, height, scaling);

        Prepare();
        StartRendering();

        InvalidateMeasure();
        InvalidateArrange();
    }

    private DenOfIzTopLevel(DenOfIzTopLevelImpl impl)
        : base(impl)
    {
        _impl = impl;
        Background = null;
    }

    public void Resize(int width, int height, double scaling = 1.0)
    {
        _impl.SetRenderSize(width, height, scaling);
        InvalidateMeasure();
        InvalidateArrange();
    }

    public void Render()
    {
        _impl.TriggerPaint();
        _impl.Surface?.RenderTarget.Flush();
    }

    public void InjectMouseMove(double x, double y, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseMove(new Point(x, y), modifiers);
    }

    public void InjectMouseDown(double x, double y, AvaloniaMouseButton button = AvaloniaMouseButton.Left,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseDown(new Point(x, y), button, modifiers);
    }

    public void InjectMouseUp(double x, double y, AvaloniaMouseButton button = AvaloniaMouseButton.Left,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseUp(new Point(x, y), button, modifiers);
    }

    public void InjectMouseWheel(double x, double y, double deltaX, double deltaY,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseWheel(new Point(x, y), new Vector(deltaX, deltaY), modifiers);
    }

    public void InjectKeyDown(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectKeyDown(key, modifiers);
    }

    public void InjectKeyUp(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectKeyUp(key, modifiers);
    }

    public void InjectTextInput(string text)
    {
        _impl.InjectTextInput(text);
    }
}