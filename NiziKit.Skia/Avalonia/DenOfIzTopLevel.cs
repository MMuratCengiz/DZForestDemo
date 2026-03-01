using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Skia.Avalonia;

internal sealed class DenOfIzTopLevel : EmbeddableControlRoot
{
    private readonly DenOfIzTopLevelImpl _impl;
    private DenOfIzSkiaSurface? _surface;
    private double _scaling = 1.0;

    private float _lastMouseX;
    private float _lastMouseY;
    private bool _shiftHeld;
    private bool _ctrlHeld;
    private bool _altHeld;

    public Texture? Texture => _surface?.Texture;

    public DenOfIzTopLevel()
        : this((int)GraphicsContext.Width, (int)GraphicsContext.Height, OS.GetDisplayScale((int)Display.GetPrimaryDisplay().ID))
    {
        _impl.TextInputActiveChanged += active =>
        {
            if (active) InputSystem.StartTextInput();
            else InputSystem.StopTextInput();
        };
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

        _impl.SetClientSize(new Size(width / scaling, height / scaling), scaling);
    }

    private void Resize(int width, int height)
    {
        SetRenderSize(width, height, _scaling);
        InvalidateMeasure();
        InvalidateArrange();
    }

    private void Layout()
    {
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
    }

    public void Update(float dt)
    {
        Layout();
        DenOfIzPlatform.TriggerRenderTick(TimeSpan.FromSeconds(dt));
    }

    public void ProcessEvent(ref Event ev)
    {
        switch (ev.Type)
        {
            case EventType.MouseMotion:
                _lastMouseX = ev.MouseMotion.X;
                _lastMouseY = ev.MouseMotion.Y;
                _impl.InjectMouseMove(new Point(_lastMouseX / _scaling, _lastMouseY / _scaling), CurrentModifiers);
                break;

            case EventType.MouseButtonDown:
                _impl.InjectMouseDown(
                    new Point(ev.MouseButton.X / _scaling, ev.MouseButton.Y / _scaling),
                    DenOfIzKeyMapper.ToAvaloniaMouseButton(ev.MouseButton.Button),
                    CurrentModifiers);
                break;

            case EventType.MouseButtonUp:
                _impl.InjectMouseUp(
                    new Point(ev.MouseButton.X / _scaling, ev.MouseButton.Y / _scaling),
                    DenOfIzKeyMapper.ToAvaloniaMouseButton(ev.MouseButton.Button),
                    CurrentModifiers);
                break;

            case EventType.MouseWheel:
                _impl.InjectMouseWheel(
                    new Point(_lastMouseX / _scaling, _lastMouseY / _scaling),
                    new Vector(ev.MouseWheel.X, ev.MouseWheel.Y),
                    CurrentModifiers);
                break;

            case EventType.KeyDown:
                UpdateModifiers(ev.Key.KeyCode, pressed: true);
                _impl.InjectKeyDown(DenOfIzKeyMapper.ToAvaloniaKey(ev.Key.KeyCode), CurrentModifiers);
                break;

            case EventType.KeyUp:
                _impl.InjectKeyUp(DenOfIzKeyMapper.ToAvaloniaKey(ev.Key.KeyCode), CurrentModifiers);
                UpdateModifiers(ev.Key.KeyCode, pressed: false);
                break;

            case EventType.TextInput:
                _impl.InjectTextInput(ev.Text.Text.ToString());
                break;

            case EventType.WindowEvent when ev.Window.Event == WindowEventType.SizeChanged:
                Resize((int)ev.Window.Data1, (int)ev.Window.Data2);
                break;
        }
    }

    public bool HitTest(double x, double y)
    {
        var hit = this.InputHitTest(new Point(x, y));

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

            if (current is Border { Background: not null })
            {
                return true;
            }

            if (current is Panel { Background: not null })
            {
                return true;
            }

            current = current.GetVisualParent() as Visual;
        }

        return false;
    }

    private void UpdateModifiers(KeyCode key, bool pressed)
    {
        switch (key)
        {
            case KeyCode.Lshift or KeyCode.Rshift: _shiftHeld = pressed; break;
            case KeyCode.Lctrl or KeyCode.Rctrl:   _ctrlHeld  = pressed; break;
            case KeyCode.Lalt  or KeyCode.Ralt:    _altHeld   = pressed; break;
        }
    }

    private RawInputModifiers CurrentModifiers =>
        DenOfIzKeyMapper.ToModifiers(_shiftHeld, _ctrlHeld, _altHeld);
}
