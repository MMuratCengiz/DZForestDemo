using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Top-level implementation for rendering Avalonia UI to a DenOfIz texture.
/// This is a minimal implementation - rendering is done directly via DrawingContextHelper.
/// </summary>
public sealed class DenOfIzTopLevelImpl : ITopLevelImpl
{
    private Size _clientSize;
    private double _scaling = 1.0;
    private IInputRoot? _inputRoot;

    public DenOfIzTopLevelImpl()
    {
    }

    // ITopLevelImpl implementation

    public double DesktopScaling => _scaling;

    public double RenderScaling => _scaling;

    public IScreenImpl? Screen => null;

    public IPlatformHandle? Handle => null;

    public Size ClientSize => _clientSize;

    public Size? FrameSize => null;

    public IEnumerable<object> Surfaces => Array.Empty<object>();

    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }

    public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new(1, 1, 1);

    public Compositor Compositor => DenOfIzPlatform.Compositor;

    /// <summary>
    /// Sets the client size for layout purposes.
    /// </summary>
    public void SetClientSize(Size size, double scaling = 1.0)
    {
        var scalingChanged = Math.Abs(scaling - _scaling) > 0.001;
        var sizeChanged = size != _clientSize;

        if (!sizeChanged && !scalingChanged)
            return;

        _clientSize = size;
        _scaling = scaling;

        if (scalingChanged)
        {
            ScalingChanged?.Invoke(scaling);
        }

        if (sizeChanged)
        {
            Resized?.Invoke(_clientSize, WindowResizeReason.Unspecified);
        }
    }

    /// <summary>
    /// Injects a mouse move event.
    /// </summary>
    public void InjectMouseMove(Point position, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        Input.Invoke(new RawPointerEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawPointerEventType.Move,
            position,
            modifiers));
    }

    /// <summary>
    /// Injects a mouse button press event.
    /// </summary>
    public void InjectMouseDown(Point position, AvaloniaMouseButton button, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        var eventType = button switch
        {
            AvaloniaMouseButton.Left => RawPointerEventType.LeftButtonDown,
            AvaloniaMouseButton.Right => RawPointerEventType.RightButtonDown,
            AvaloniaMouseButton.Middle => RawPointerEventType.MiddleButtonDown,
            _ => RawPointerEventType.LeftButtonDown
        };

        Input.Invoke(new RawPointerEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            eventType,
            position,
            modifiers));
    }

    /// <summary>
    /// Injects a mouse button release event.
    /// </summary>
    public void InjectMouseUp(Point position, AvaloniaMouseButton button, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        var eventType = button switch
        {
            AvaloniaMouseButton.Left => RawPointerEventType.LeftButtonUp,
            AvaloniaMouseButton.Right => RawPointerEventType.RightButtonUp,
            AvaloniaMouseButton.Middle => RawPointerEventType.MiddleButtonUp,
            _ => RawPointerEventType.LeftButtonUp
        };

        Input.Invoke(new RawPointerEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            eventType,
            position,
            modifiers));
    }

    /// <summary>
    /// Injects a mouse wheel event.
    /// </summary>
    public void InjectMouseWheel(Point position, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        Input.Invoke(new RawMouseWheelEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            position,
            delta,
            modifiers));
    }

    /// <summary>
    /// Injects a key down event.
    /// </summary>
    public void InjectKeyDown(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        Input.Invoke(new RawKeyEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawKeyEventType.KeyDown,
            key,
            modifiers));
    }

    /// <summary>
    /// Injects a key up event.
    /// </summary>
    public void InjectKeyUp(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        Input.Invoke(new RawKeyEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawKeyEventType.KeyUp,
            key,
            modifiers));
    }

    /// <summary>
    /// Injects a text input event.
    /// </summary>
    public void InjectTextInput(string text)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
            return;

        Input.Invoke(new RawTextInputEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            text));
    }

    /// <summary>
    /// Returns true if input can be injected (input root is set).
    /// </summary>
    public bool IsInputReady => _inputRoot != null;

    private IInputRoot? GetInputRoot() => _inputRoot;

    private static IMouseDevice GetMouseDevice()
    {
        return new MouseDevice();
    }

    private static IKeyboardDevice GetKeyboardDevice()
    {
        return AvaloniaLocator.Current.GetRequiredService<IKeyboardDevice>();
    }

    // ITopLevelImpl methods

    public void Dispose()
    {
        Closed?.Invoke();
    }

    public void Invalidate(Rect rect)
    {
        // Trigger repaint
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
        _inputRoot = inputRoot;
    }

    public Point PointToClient(PixelPoint point)
        => new(point.X / _scaling, point.Y / _scaling);

    public PixelPoint PointToScreen(Point point)
        => new((int)(point.X * _scaling), (int)(point.Y * _scaling));

    public void SetCursor(ICursorImpl? cursor)
    {
        // Cursor is handled by DenOfIz/game engine
    }

    public IPopupImpl? CreatePopup()
        => null; // Popups not supported in embedded mode

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        // Transparency handled by texture alpha
    }

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
        // Theme handled by Avalonia styles
    }

    public object? TryGetFeature(Type featureType)
        => null;
}

