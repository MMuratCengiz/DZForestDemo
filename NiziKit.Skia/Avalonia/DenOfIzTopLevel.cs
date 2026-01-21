using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using DenOfIz;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// A top-level Avalonia control that renders to a DenOfIz texture.
/// Use this to embed Avalonia UI in your DenOfIz application.
/// </summary>
public sealed class DenOfIzTopLevel : TopLevel
{
    private readonly DenOfIzTopLevelImpl _impl;

    /// <summary>
    /// The DenOfIz texture containing the rendered Avalonia UI.
    /// Display this texture in your DenOfIz rendering pipeline.
    /// </summary>
    public Texture? Texture => _impl.Texture;

    /// <summary>
    /// The underlying surface for advanced usage.
    /// </summary>
    public DenOfIzSkiaSurface? Surface => _impl.Surface;

    /// <summary>
    /// Width of the render target in pixels.
    /// </summary>
    public int PixelWidth => _impl.Width;

    /// <summary>
    /// Height of the render target in pixels.
    /// </summary>
    public int PixelHeight => _impl.Height;

    /// <summary>
    /// Creates a new DenOfIzTopLevel with the specified size.
    /// </summary>
    public DenOfIzTopLevel(int width, int height, double scaling = 1.0)
        : this(new DenOfIzTopLevelImpl())
    {
        _impl.SetRenderSize(width, height, scaling);

        // DEBUG: Set a green background to verify TopLevel rendering
        Background = Brushes.Green;

        // Trigger initial layout
        InvalidateMeasure();
        InvalidateArrange();
    }

    private DenOfIzTopLevel(DenOfIzTopLevelImpl impl)
        : base(impl)
    {
        _impl = impl;
    }

    /// <summary>
    /// Resizes the render target.
    /// </summary>
    public void Resize(int width, int height, double scaling = 1.0)
    {
        _impl.SetRenderSize(width, height, scaling);

        // Force re-layout after resize
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>
    /// Triggers rendering. Call this each frame from your game loop.
    /// </summary>
    public void Render()
    {
        // Get the size to layout with
        var size = _impl.ClientSize;

        // Force layout pass if needed
        if (!IsMeasureValid || !IsArrangeValid)
        {
            Measure(size);
            Arrange(new Rect(size));
        }

        _impl.TriggerPaint();
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
