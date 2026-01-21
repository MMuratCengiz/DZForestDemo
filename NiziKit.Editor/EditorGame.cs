using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;
using NiziKit.Skia;
using NiziKit.Skia.Avalonia;

namespace NiziKit.Editor;

public sealed class EditorGame : Game
{
    private RenderFrame _renderFrame = null!;
    private CycledTexture _sceneColor = null!;
    private DenOfIzTopLevel _topLevel = null!;
    private Avalonia.Application _avaloniaApp = null!;
    private SkiaContext _skiaContext = null!;

    private uint _width;
    private uint _height;
    private double _scaling = 1.0;

    public EditorGame(GameDesc? desc = null) : base(desc)
    {
    }

    protected override void Load(Game game)
    {
        // Initialize Skia context for GPU rendering
        _skiaContext = new SkiaContext();

        // Initialize Avalonia with DenOfIz platform
        _avaloniaApp = AppBuilder.Configure<EditorApp>()
            .UseDenOfIz()
            .SetupWithoutStarting()
            .Instance!;

        // Create render frame
        _renderFrame = new RenderFrame();

        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;

        // Create render target
        _sceneColor = CycledTexture.ColorAttachment("SceneColor");

        // Create Avalonia top-level for rendering
        // Use scaling=1.0 - we render at physical pixel resolution
        // Avalonia will use the full canvas size
        _scaling = 1.0;
        Console.WriteLine($"[EditorGame] GraphicsContext size: {_width}x{_height}, scaling: {_scaling}");
        _topLevel = new DenOfIzTopLevel((int)_width, (int)_height, _scaling);
        _topLevel.Content = new EditorMainView();
        Console.WriteLine($"[EditorGame] TopLevel created, Content type: {_topLevel.Content?.GetType().Name}");
    }

    protected override void Update(float dt)
    {
        // Pump Avalonia dispatcher and render tick
        DenOfIzPlatform.TriggerRenderTick(TimeSpan.FromSeconds(dt));
        Dispatcher.UIThread.RunJobs();

        // Render Avalonia UI
        _topLevel.Render();

        // Render frame
        _renderFrame.BeginFrame();

        // Clear the scene color
        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.Begin();
        pass.End();

        // Blit Avalonia texture to scene
        if (_topLevel.Texture != null)
        {
            _renderFrame.AlphaBlit(_topLevel.Texture, _sceneColor);
        }

        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
    }

    protected override void OnEvent(ref Event ev)
    {
        // Forward mouse events to Avalonia (coordinates are in physical pixels, matching our canvas)
        if (ev.Type == EventType.MouseMotion)
        {
            _topLevel.InjectMouseMove(ev.MouseMotion.X, ev.MouseMotion.Y);
        }
        else if (ev.Type == EventType.MouseButtonDown)
        {
            var button = MapMouseButton(ev.MouseButton.Button);
            _topLevel.InjectMouseDown(ev.MouseButton.X, ev.MouseButton.Y, button);
        }
        else if (ev.Type == EventType.MouseButtonUp)
        {
            var button = MapMouseButton(ev.MouseButton.Button);
            _topLevel.InjectMouseUp(ev.MouseButton.X, ev.MouseButton.Y, button);
        }
        else if (ev.Type == EventType.MouseWheel)
        {
            // MouseWheel doesn't have mouse position, use 0,0 as fallback
            _topLevel.InjectMouseWheel(0, 0, ev.MouseWheel.X, ev.MouseWheel.Y);
        }
        else if (ev.Type == EventType.KeyDown)
        {
            var key = MapKey(ev.Key.KeyCode);
            var modifiers = MapModifiers((KeyMod)ev.Key.Mod);
            _topLevel.InjectKeyDown(key, modifiers);
        }
        else if (ev.Type == EventType.KeyUp)
        {
            var key = MapKey(ev.Key.KeyCode);
            var modifiers = MapModifiers((KeyMod)ev.Key.Mod);
            _topLevel.InjectKeyUp(key, modifiers);
        }
        else if (ev.Type == EventType.TextInput)
        {
            var text = ev.Text.Text.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                _topLevel.InjectTextInput(text);
            }
        }
        else if (ev.Type == EventType.WindowEvent && ev.Window.Event == WindowEventType.Resized)
        {
            var width = (uint)ev.Window.Data1;
            var height = (uint)ev.Window.Data2;
            OnResize(width, height);
        }
    }

    private void OnResize(uint width, uint height)
    {
        if (_width == width && _height == height)
            return;

        GraphicsContext.WaitIdle();

        _sceneColor.Dispose();

        _width = width;
        _height = height;

        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _topLevel.Resize((int)width, (int)height, _scaling);
    }

    private static Avalonia.Input.MouseButton MapMouseButton(DenOfIz.MouseButton button)
    {
        return button switch
        {
            DenOfIz.MouseButton.Left => Avalonia.Input.MouseButton.Left,
            DenOfIz.MouseButton.Right => Avalonia.Input.MouseButton.Right,
            DenOfIz.MouseButton.Middle => Avalonia.Input.MouseButton.Middle,
            _ => Avalonia.Input.MouseButton.Left
        };
    }

    private static Key MapKey(KeyCode keyCode)
    {
        // Map common keys - this can be expanded as needed
        return keyCode switch
        {
            KeyCode.A => Key.A,
            KeyCode.B => Key.B,
            KeyCode.C => Key.C,
            KeyCode.D => Key.D,
            KeyCode.E => Key.E,
            KeyCode.F => Key.F,
            KeyCode.G => Key.G,
            KeyCode.H => Key.H,
            KeyCode.I => Key.I,
            KeyCode.J => Key.J,
            KeyCode.K => Key.K,
            KeyCode.L => Key.L,
            KeyCode.M => Key.M,
            KeyCode.N => Key.N,
            KeyCode.O => Key.O,
            KeyCode.P => Key.P,
            KeyCode.Q => Key.Q,
            KeyCode.R => Key.R,
            KeyCode.S => Key.S,
            KeyCode.T => Key.T,
            KeyCode.U => Key.U,
            KeyCode.V => Key.V,
            KeyCode.W => Key.W,
            KeyCode.X => Key.X,
            KeyCode.Y => Key.Y,
            KeyCode.Z => Key.Z,
            KeyCode.Num0 => Key.D0,
            KeyCode.Num1 => Key.D1,
            KeyCode.Num2 => Key.D2,
            KeyCode.Num3 => Key.D3,
            KeyCode.Num4 => Key.D4,
            KeyCode.Num5 => Key.D5,
            KeyCode.Num6 => Key.D6,
            KeyCode.Num7 => Key.D7,
            KeyCode.Num8 => Key.D8,
            KeyCode.Num9 => Key.D9,
            KeyCode.Return => Key.Enter,
            KeyCode.Escape => Key.Escape,
            KeyCode.Backspace => Key.Back,
            KeyCode.Tab => Key.Tab,
            KeyCode.Space => Key.Space,
            KeyCode.Left => Key.Left,
            KeyCode.Right => Key.Right,
            KeyCode.Up => Key.Up,
            KeyCode.Down => Key.Down,
            KeyCode.Delete => Key.Delete,
            KeyCode.Home => Key.Home,
            KeyCode.End => Key.End,
            KeyCode.F1 => Key.F1,
            KeyCode.F2 => Key.F2,
            KeyCode.F3 => Key.F3,
            KeyCode.F4 => Key.F4,
            KeyCode.F5 => Key.F5,
            KeyCode.F6 => Key.F6,
            KeyCode.F7 => Key.F7,
            KeyCode.F8 => Key.F8,
            KeyCode.F9 => Key.F9,
            KeyCode.F10 => Key.F10,
            KeyCode.F11 => Key.F11,
            KeyCode.F12 => Key.F12,
            _ => Key.None
        };
    }

    private static RawInputModifiers MapModifiers(KeyMod mod)
    {
        var result = RawInputModifiers.None;

        if ((mod & KeyMod.Shift) != 0)
            result |= RawInputModifiers.Shift;
        if ((mod & KeyMod.Ctrl) != 0)
            result |= RawInputModifiers.Control;
        if ((mod & KeyMod.Alt) != 0)
            result |= RawInputModifiers.Alt;
        if ((mod & KeyMod.Gui) != 0)
            result |= RawInputModifiers.Meta;

        return result;
    }

    protected override void OnShutdown()
    {
        GraphicsContext.WaitIdle();

        _sceneColor.Dispose();
        _renderFrame.Dispose();
        _skiaContext.Dispose();
    }
}
