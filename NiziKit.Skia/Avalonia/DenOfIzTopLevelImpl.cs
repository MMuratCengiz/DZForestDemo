using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using DenOfIz;
using AvaloniaMouseButton = Avalonia.Input.MouseButton;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Top-level implementation for rendering Avalonia UI to a DenOfIz texture.
/// This is the core bridge between Avalonia and DenOfIz.
/// </summary>
public sealed class DenOfIzTopLevelImpl : ITopLevelImpl
{
    private DenOfIzSkiaSurface? _surface;
    private PixelSize _renderSize;
    private double _scaling = 1.0;
    private readonly List<object> _surfaces = new();
    private IInputRoot? _inputRoot;

    /// <summary>
    /// The DenOfIz texture containing the rendered UI.
    /// </summary>
    public Texture? Texture => _surface?.Texture;

    /// <summary>
    /// The underlying Skia surface.
    /// </summary>
    public DenOfIzSkiaSurface? Surface => _surface;

    /// <summary>
    /// Width of the render target in pixels.
    /// </summary>
    public int Width => _renderSize.Width;

    /// <summary>
    /// Height of the render target in pixels.
    /// </summary>
    public int Height => _renderSize.Height;

    // ITopLevelImpl implementation

    public double DesktopScaling => _scaling;

    public double RenderScaling => _scaling;

    public IScreenImpl? Screen => null;

    public IPlatformHandle? Handle => null;

    public Size ClientSize => new(_renderSize.Width / _scaling, _renderSize.Height / _scaling);

    public Size? FrameSize => null;

    public IEnumerable<object> Surfaces => GetOrCreateSurfaces();

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
    /// Sets the size of the render target.
    /// </summary>
    public void SetRenderSize(int width, int height, double scaling = 1.0)
    {
        var newSize = new PixelSize(width, height);
        var scalingChanged = Math.Abs(scaling - _scaling) > 0.001;
        var sizeChanged = newSize != _renderSize;

        if (!sizeChanged && !scalingChanged)
            return;

        _renderSize = newSize;
        _scaling = scaling;

        // Create or resize the surface
        if (_surface != null)
        {
            _surface.Resize(newSize, scaling);
        }
        else if (newSize.Width > 0 && newSize.Height > 0)
        {
            // Create surface eagerly on first valid size
            _surface = new DenOfIzSkiaSurface(newSize, scaling);
            _surfaces.Clear();
            _surfaces.Add(_surface);
        }

        if (scalingChanged)
        {
            ScalingChanged?.Invoke(scaling);
        }

        if (sizeChanged)
        {
            Resized?.Invoke(ClientSize, WindowResizeReason.Unspecified);
        }
    }

    /// <summary>
    /// Triggers a paint operation. Call this each frame.
    /// </summary>
    public void TriggerPaint()
    {
        if (_surface == null)
            return;

        Paint?.Invoke(new Rect(0, 0, ClientSize.Width, ClientSize.Height));
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

    private IEnumerable<object> GetOrCreateSurfaces()
    {
        if (_surface == null && _renderSize.Width > 0 && _renderSize.Height > 0)
        {
            _surface = new DenOfIzSkiaSurface(_renderSize, _scaling);
            _surfaces.Clear();
            _surfaces.Add(_surface);
        }
        return _surfaces;
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
        _surface?.Dispose();
        _surface = null;
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

