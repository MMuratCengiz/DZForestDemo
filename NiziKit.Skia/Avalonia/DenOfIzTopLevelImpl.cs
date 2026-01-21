using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

public sealed class DenOfIzTopLevelImpl : ITopLevelImpl
{
    private Size _clientSize;
    private double _scaling = 1.0;
    private IInputRoot? _inputRoot;
    private DenOfIzSkiaSurface? _surface;

    public DenOfIzTopLevelImpl()
    {
    }

    public DenOfIzSkiaSurface? Surface => _surface;

    public double DesktopScaling => _scaling;

    public double RenderScaling => _scaling;

    public IScreenImpl? Screen => null;

    public IPlatformHandle? Handle => null;

    public Size ClientSize => _clientSize;

    public Size? FrameSize => null;

    public IEnumerable<object> Surfaces => _surface != null ? new object[] { _surface } : Array.Empty<object>();

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

    public void SetRenderSize(int width, int height, double scaling)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var newClientSize = new Size(width / scaling, height / scaling);
        var scalingChanged = Math.Abs(scaling - _scaling) > 0.001;
        var sizeChanged = newClientSize != _clientSize;

        _scaling = scaling;
        _clientSize = newClientSize;

        if (_surface == null)
        {
            _surface = new DenOfIzSkiaSurface(width, height, scaling);
        }
        else if (_surface.Width != width || _surface.Height != height)
        {
            _surface.Resize(width, height, scaling);
        }

        if (scalingChanged)
        {
            ScalingChanged?.Invoke(scaling);
        }

        if (sizeChanged)
        {
            Resized?.Invoke(_clientSize, WindowResizeReason.Unspecified);
        }
    }

    public void TriggerPaint()
    {
        Paint?.Invoke(new Rect(0, 0, _clientSize.Width, _clientSize.Height));
    }

    public void InjectMouseMove(Point position, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

        Input.Invoke(new RawPointerEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawPointerEventType.Move,
            position,
            modifiers));
    }

    public void InjectMouseDown(Point position, AvaloniaMouseButton button, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

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

    public void InjectMouseUp(Point position, AvaloniaMouseButton button, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

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

    public void InjectMouseWheel(Point position, Vector delta, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

        Input.Invoke(new RawMouseWheelEventArgs(
            GetMouseDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            position,
            delta,
            modifiers));
    }

    public void InjectKeyDown(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

        Input.Invoke(new RawKeyEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawKeyEventType.KeyDown,
            key,
            modifiers));
    }

    public void InjectKeyUp(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

        Input.Invoke(new RawKeyEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            RawKeyEventType.KeyUp,
            key,
            modifiers));
    }

    public void InjectTextInput(string text)
    {
        var inputRoot = GetInputRoot();
        if (inputRoot == null || Input == null)
        {
            return;
        }

        Input.Invoke(new RawTextInputEventArgs(
            GetKeyboardDevice(),
            (ulong)Environment.TickCount64,
            inputRoot,
            text));
    }

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

    public void Dispose()
    {
        Closed?.Invoke();
    }

    public void Invalidate(Rect rect)
    {
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
    }

    public IPopupImpl? CreatePopup()
        => null; // Popups not supported in embedded mode

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
    }

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
    }

    public object? TryGetFeature(Type featureType)
        => null;
}

