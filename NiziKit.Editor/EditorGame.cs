using System.Numerics;
using Avalonia;
using Avalonia.Input;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.ViewModels;
using NiziKit.Graphics;
using NiziKit.Skia;
using NiziKit.Skia.Avalonia;

namespace NiziKit.Editor;

public sealed class EditorGame : Game
{
    private EditorRenderer _renderer = null!;
    private DenOfIzTopLevel _topLevel = null!;
    private Avalonia.Application _avaloniaApp = null!;
    private SkiaContext _skiaContext = null!;
    private CameraObject _editorCamera = null!;

    private uint _width;
    private uint _height;
    private double _scaling = 1.0;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _mouseOverUi;
    private bool _textInputActive;
    private bool _gizmoDragging;

    public EditorGame(GameDesc? desc = null) : base(desc)
    {
    }

    protected override void Load(Game game)
    {
        _skiaContext = new SkiaContext();

        _avaloniaApp = AppBuilder.Configure<EditorApp>()
            .UseDenOfIz()
            .SetupWithoutStarting()
            .Instance!;

        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;

        _scaling = 1.0;
        _topLevel = new DenOfIzTopLevel((int)_width, (int)_height, _scaling);
        _topLevel.Content = new EditorMainView();

        _renderer = new EditorRenderer(_topLevel);

        _editorCamera = new CameraObject("EditorCamera");
        _editorCamera.LocalPosition = new Vector3(0, 15, -15);
        var controller = new CameraController { MoveSpeed = 10f };
        _editorCamera.AddComponent(controller);
        controller.SetPositionAndLookAt(new Vector3(0, 15, -15), Vector3.Zero, immediate: true);
        _editorCamera.SetAspectRatio(_width, _height);
        _renderer.Camera = _editorCamera;

        _topLevel.TextInputActiveChanged += OnTextInputActiveChanged;

        if (!string.IsNullOrEmpty(Editor.Config.InitialScene))
        {
            World.LoadScene(Editor.Config.InitialScene);
        }
        if (_topLevel.Content is EditorMainView mainView)
        {
            mainView.Initialize();
            _renderer.EditorViewModel = mainView.ViewModel;
        }
    }

    protected override void Update(float dt)
    {
        _editorCamera.Update(dt);

        if (!_mouseOverUi && !_gizmoDragging)
        {
            UpdateGizmoHover();
        }

        _renderer.Render(dt);
    }

    private void UpdateGizmoHover()
    {
        var gizmoPass = _renderer.GizmoPass;
        if (gizmoPass == null)
        {
            return;
        }

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        gizmoPass.Gizmo.UpdateHover(ray, _editorCamera);
    }

    protected override void OnEvent(ref Event ev)
    {
        if (ev.Type == EventType.MouseMotion)
        {
            _lastMouseX = ev.MouseMotion.X;
            _lastMouseY = ev.MouseMotion.Y;
            _mouseOverUi = _topLevel.HitTest(_lastMouseX, _lastMouseY);
            _topLevel.InjectMouseMove(ev.MouseMotion.X, ev.MouseMotion.Y);

            if (_gizmoDragging)
            {
                HandleGizmoDrag();
            }
        }
        else if (ev.Type == EventType.MouseButtonDown)
        {
            var button = MapMouseButton(ev.MouseButton.Button);
            _topLevel.InjectMouseDown(ev.MouseButton.X, ev.MouseButton.Y, button);

            if (!_mouseOverUi && ev.MouseButton.Button == DenOfIz.MouseButton.Left)
            {
                if (TryBeginGizmoDrag())
                {
                    return;
                }

                TrySelectMeshAtCursor();
            }
        }
        else if (ev.Type == EventType.MouseButtonUp)
        {
            var button = MapMouseButton(ev.MouseButton.Button);
            _topLevel.InjectMouseUp(ev.MouseButton.X, ev.MouseButton.Y, button);

            if (ev.MouseButton.Button == DenOfIz.MouseButton.Left && _gizmoDragging)
            {
                EndGizmoDrag();
            }
        }
        else if (ev.Type == EventType.MouseWheel)
        {
            _topLevel.InjectMouseWheel(_lastMouseX, _lastMouseY, ev.MouseWheel.X, ev.MouseWheel.Y);
        }
        else if (ev.Type == EventType.KeyDown)
        {
            var key = MapKey(ev.Key.KeyCode);
            var modifiers = MapModifiers((KeyMod)ev.Key.Mod);
            _topLevel.InjectKeyDown(key, modifiers);

            if (!_textInputActive)
            {
                HandleGizmoModeKey(ev.Key.KeyCode);
            }

            if (ev.Key.KeyCode == KeyCode.Escape && _gizmoDragging)
            {
                CancelGizmoDrag();
            }
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

        if (!UiWantsInput && !_gizmoDragging)
        {
            _editorCamera.HandleEvent(in ev);
        }
    }

    private bool TryBeginGizmoDrag()
    {
        var gizmoPass = _renderer.GizmoPass;
        if (gizmoPass == null)
        {
            return false;
        }

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        if (gizmoPass.Gizmo.BeginDrag(ray, _editorCamera))
        {
            _gizmoDragging = true;
            return true;
        }

        return false;
    }

    private void HandleGizmoDrag()
    {
        var gizmoPass = _renderer.GizmoPass;
        if (gizmoPass == null)
        {
            return;
        }

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        gizmoPass.Gizmo.UpdateDrag(ray, _editorCamera);
    }

    private void EndGizmoDrag()
    {
        var gizmoPass = _renderer.GizmoPass;
        gizmoPass?.Gizmo.EndDrag();
        _gizmoDragging = false;
    }

    private void CancelGizmoDrag()
    {
        var gizmoPass = _renderer.GizmoPass;
        gizmoPass?.Gizmo.CancelDrag();
        _gizmoDragging = false;
    }

    private void HandleGizmoModeKey(KeyCode keyCode)
    {
        var gizmoPass = _renderer.GizmoPass;
        if (gizmoPass == null)
        {
            return;
        }

        switch (keyCode)
        {
            case KeyCode.W:
                gizmoPass.Gizmo.Mode = GizmoMode.Translate;
                break;
            case KeyCode.E:
                gizmoPass.Gizmo.Mode = GizmoMode.Rotate;
                break;
            case KeyCode.R:
                gizmoPass.Gizmo.Mode = GizmoMode.Scale;
                break;
        }
    }

    private void TrySelectMeshAtCursor()
    {
        var scene = World.CurrentScene;
        var viewModel = _renderer.EditorViewModel;
        if (scene == null || viewModel == null)
        {
            return;
        }

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);

        GameObject? closestObject = null;
        var closestDistance = float.MaxValue;

        foreach (var meshComponent in scene.FindComponents<MeshComponent>())
        {
            if (meshComponent.Mesh == null)
            {
                continue;
            }

            var gameObject = meshComponent.Owner;
            if (gameObject == null)
            {
                continue;
            }

            if (TransformGizmo.RayBoundsIntersection(ray, meshComponent.Mesh.Bounds, gameObject.WorldMatrix, out var distance))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = gameObject;
                }
            }
        }

        if (closestObject != null)
        {
            var objectVm = FindGameObjectViewModel(viewModel.RootObjects, closestObject);
            viewModel.SelectObject(objectVm);
        }
        else
        {
            viewModel.SelectObject(null);
        }
    }

    private static GameObjectViewModel? FindGameObjectViewModel(IEnumerable<GameObjectViewModel> viewModels, GameObject target)
    {
        foreach (var vm in viewModels)
        {
            if (vm.GameObject == target)
            {
                return vm;
            }

            var found = FindGameObjectViewModel(vm.Children, target);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private void OnResize(uint width, uint height)
    {
        if (_width == width && _height == height)
        {
            return;
        }

        GraphicsContext.WaitIdle();

        _width = width;
        _height = height;

        _editorCamera.SetAspectRatio(width, height);
        _renderer.OnResize(width, height);
        _topLevel.Resize((int)width, (int)height, _scaling);
    }

    private void OnTextInputActiveChanged(bool active)
    {
        _textInputActive = active;
        if (active)
        {
            InputSystem.StartTextInput();
        }
        else
        {
            InputSystem.StopTextInput();
        }
    }

    private bool UiWantsInput => _mouseOverUi || _textInputActive;

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
        {
            result |= RawInputModifiers.Shift;
        }

        if ((mod & KeyMod.Ctrl) != 0)
        {
            result |= RawInputModifiers.Control;
        }

        if ((mod & KeyMod.Alt) != 0)
        {
            result |= RawInputModifiers.Alt;
        }

        if ((mod & KeyMod.Gui) != 0)
        {
            result |= RawInputModifiers.Meta;
        }

        return result;
    }

    protected override void OnShutdown()
    {
        GraphicsContext.WaitIdle();

        _topLevel.TextInputActiveChanged -= OnTextInputActiveChanged;
        _renderer.Dispose();
        _skiaContext.Dispose();
    }
}
