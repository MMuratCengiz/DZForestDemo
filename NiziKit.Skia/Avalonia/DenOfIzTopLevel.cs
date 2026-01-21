using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Skia.Helpers;
using DenOfIz;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// A top-level Avalonia control that renders to a DenOfIz texture.
/// Use this to embed Avalonia UI in your DenOfIz application.
/// </summary>
public sealed class DenOfIzTopLevel : EmbeddableControlRoot
{
    private readonly DenOfIzTopLevelImpl _impl;
    private DenOfIzSkiaSurface? _surface;
    private double _scaling = 1.0;

    /// <summary>
    /// The DenOfIz texture containing the rendered Avalonia UI.
    /// Display this texture in your DenOfIz rendering pipeline.
    /// </summary>
    public Texture? Texture => _surface?.Texture;

    /// <summary>
    /// The underlying surface for advanced usage.
    /// </summary>
    public DenOfIzSkiaSurface? Surface => _surface;

    /// <summary>
    /// Width of the render target in pixels.
    /// </summary>
    public int PixelWidth => _surface?.Width ?? 0;

    /// <summary>
    /// Height of the render target in pixels.
    /// </summary>
    public int PixelHeight => _surface?.Height ?? 0;

    /// <summary>
    /// Creates a new DenOfIzTopLevel with the specified size.
    /// </summary>
    public DenOfIzTopLevel(int width, int height, double scaling = 1.0)
        : this(new DenOfIzTopLevelImpl())
    {
        _scaling = scaling;
        SetRenderSize(width, height, scaling);

        // Prepare the control root - this sets up input root and other essentials
        Prepare();

        // Trigger initial layout
        InvalidateMeasure();
        InvalidateArrange();
    }

    private DenOfIzTopLevel(DenOfIzTopLevelImpl impl)
        : base(impl)
    {
        _impl = impl;

        // Set transparent background - content will provide its own background
        Background = null;
    }

    private void SetRenderSize(int width, int height, double scaling)
    {
        if (width <= 0 || height <= 0)
            return;

        _scaling = scaling;

        if (_surface == null)
        {
            _surface = new DenOfIzSkiaSurface(width, height, scaling);
        }
        else if (_surface.Width != width || _surface.Height != height)
        {
            _surface.Resize(width, height, scaling);
        }

        _impl.SetClientSize(new Size(width / scaling, height / scaling), scaling);
    }

    /// <summary>
    /// Resizes the render target.
    /// </summary>
    public void Resize(int width, int height, double scaling = 1.0)
    {
        SetRenderSize(width, height, scaling);

        // Force re-layout after resize
        InvalidateMeasure();
        InvalidateArrange();
    }

    private int _frameCount = 0;

    /// <summary>
    /// Triggers rendering. Call this each frame from your game loop.
    /// </summary>
    public void Render()
    {
        if (_surface == null)
            return;

        _frameCount++;
        if (_frameCount % 60 == 0)
        {
            Console.WriteLine($"[Avalonia] Frame {_frameCount}: Surface={_surface.Width}x{_surface.Height}, Scaling={_scaling}, VisualChildren={VisualChildren.Count}");
        }

        // Get the size to layout with (logical pixels)
        var size = new Size(_surface.Width / _scaling, _surface.Height / _scaling);

        // Force layout pass if needed
        if (!IsMeasureValid || !IsArrangeValid)
        {
            Measure(size);
            Arrange(new Rect(size));
        }

        // Clear the canvas
        var canvas = _surface.RenderTarget.Canvas;
        canvas.Clear(SkiaSharp.SKColors.Transparent);

        // Render Avalonia visual directly to our Skia canvas
        // DrawingContextHelper renders at the visual's arranged size (physical pixels when scaling=1)
        DrawingContextHelper.RenderAsync(canvas, this).GetAwaiter().GetResult();

        // Flush the Skia surface to ensure rendering is complete
        _surface.RenderTarget.Flush();
    }

    /// <summary>
    /// Injects a mouse move event. Position is in logical pixels.
    /// </summary>
    public void InjectMouseMove(double x, double y, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseMove(new Point(x, y), modifiers);
    }

    /// <summary>
    /// Injects a mouse button press event.
    /// </summary>
    public void InjectMouseDown(double x, double y, AvaloniaMouseButton button = AvaloniaMouseButton.Left, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseDown(new Point(x, y), button, modifiers);
    }

    /// <summary>
    /// Injects a mouse button release event.
    /// </summary>
    public void InjectMouseUp(double x, double y, AvaloniaMouseButton button = AvaloniaMouseButton.Left, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseUp(new Point(x, y), button, modifiers);
    }

    /// <summary>
    /// Injects a mouse wheel event.
    /// </summary>
    public void InjectMouseWheel(double x, double y, double deltaX, double deltaY, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectMouseWheel(new Point(x, y), new Vector(deltaX, deltaY), modifiers);
    }

    /// <summary>
    /// Injects a key down event.
    /// </summary>
    public void InjectKeyDown(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectKeyDown(key, modifiers);
    }

    /// <summary>
    /// Injects a key up event.
    /// </summary>
    public void InjectKeyUp(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _impl.InjectKeyUp(key, modifiers);
    }

    /// <summary>
    /// Injects a text input event.
    /// </summary>
    public void InjectTextInput(string text)
    {
        _impl.InjectTextInput(text);
    }
}
