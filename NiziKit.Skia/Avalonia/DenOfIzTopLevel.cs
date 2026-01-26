using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Skia.Helpers;
using Avalonia.VisualTree;
using DenOfIz;
using SkiaSharp;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

public sealed class DenOfIzTopLevel : EmbeddableControlRoot
{
    private readonly DenOfIzTopLevelImpl _impl;
    private DenOfIzSkiaSurface? _surface;
    private double _scaling = 1.0;

    public Texture? Texture => _surface?.Texture;

    public DenOfIzSkiaSurface? Surface => _surface;

    public int PixelWidth => _surface?.Width ?? 0;

    public int PixelHeight => _surface?.Height ?? 0;

    public event Action<bool>? TextInputActiveChanged
    {
        add => _impl.TextInputActiveChanged += value;
        remove => _impl.TextInputActiveChanged -= value;
    }

    public DenOfIzTopLevel(int width, int height, double scaling = 1.0)
        : this(new DenOfIzTopLevelImpl())
    {
        _scaling = scaling;
        SetRenderSize(width, height, scaling);

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
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None];
    }

    private void SetRenderSize(int width, int height, double scaling)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _scaling = scaling;

        if (_surface == null)
        {
            _surface = new DenOfIzSkiaSurface(width, height, scaling);
            _impl.SetSurface(_surface);
        }
        else if (_surface.Width != width || _surface.Height != height)
        {
            _surface.Resize(width, height, scaling);
        }

        _impl.SetClientSize(new Size(width, height), scaling);
    }

    public void Resize(int width, int height, double scaling = 1.0)
    {
        SetRenderSize(width, height, scaling);

        InvalidateMeasure();
        InvalidateArrange();
    }

    public void Render()
    {
        _impl.TriggerPaint();
        if (_surface == null)
        {
            return;
        }
        var size = new Size(_surface.Width / _scaling, _surface.Height / _scaling);
        if (!IsMeasureValid || !IsArrangeValid)
        {
            Measure(size);
            Arrange(new Rect(size));
        }

        var canvas = _surface.RenderTarget.Canvas;
        canvas.DrawColor(SKColors.Transparent, SKBlendMode.Src);

        DrawingContextHelper.RenderAsync(canvas, this).GetAwaiter().GetResult();
        _surface.RenderTarget.Flush();
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

    public bool HitTest(double x, double y)
    {
        var point = new Point(x, y);
        var hit = this.InputHitTest(point);

        if (hit == null || hit == this)
        {
            return false;
        }

        var current = hit as Visual;
        while (current != null && current != this)
        {
            if (current is Button or TextBox or ComboBox or ListBox or TreeView
                or ScrollViewer or MenuItem or Menu or ToggleButton or CheckBox
                or Slider or ScrollBar)
            {
                return true;
            }

            if (current is Border border && border.Background != null)
            {
                return true;
            }

            if (current is Panel panel && panel.Background != null)
            {
                return true;
            }

            current = current.GetVisualParent() as Visual;
        }

        return false;
    }
}
