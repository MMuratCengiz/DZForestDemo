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
using NiziKit.Graphics.Renderer;
using NiziKit.Skia;
using NiziKit.Skia.Avalonia;

namespace NiziKit.Editor;

public sealed class EditorGame : Game
{
    private EditorRenderer _renderer = null!;
    private IRenderer _gameRenderer = null!;
    private DenOfIzTopLevel _topLevel = null!;
    private Avalonia.Application _avaloniaApp = null!;
    private SkiaContext _skiaContext = null!;
    private GameObject _editorCameraObject = null!;
    private CameraComponent _editorCamera = null!;
    private FreeFlyController _editorController = null!;

    private uint _width;
    private uint _height;
    private double _scaling = 1.0;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _mouseOverUi;
    private bool _textInputActive;
    private bool _gizmoDragging;
    private bool _shiftHeld;

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

        _scaling = Display.GetPrimaryDisplay().DpiScale;
        _topLevel = new DenOfIzTopLevel((int)_width, (int)_height, _scaling);
        _topLevel.Content = new EditorMainView();

        _gameRenderer = (IRenderer)Activator.CreateInstance(RendererType)!;
        _renderer = new EditorRenderer(_topLevel, _gameRenderer);

        _editorCameraObject = new GameObject("EditorCamera");
        _editorCameraObject.LocalPosition = new Vector3(0, 15, -15);

        _editorCamera = _editorCameraObject.AddComponent<CameraComponent>();
        _editorCamera.Priority = 1000;

        _editorController = _editorCameraObject.AddComponent<FreeFlyController>();
        _editorController.MoveSpeed = 10f;
        _editorController.SetPositionAndLookAt(new Vector3(0, 15, -15), Vector3.Zero, immediate: true);

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

            if (mainView.ViewModel != null)
            {
                mainView.ViewModel.ViewPresetChanged += OnViewPresetChanged;
                mainView.ViewModel.ProjectionModeChanged += OnProjectionModeChanged;
                mainView.ViewModel.GridSettingsChanged += OnGridSettingsChanged;
                mainView.ViewModel.SetGridDesc(_renderer.GizmoPass.Gizmo.GridDesc);
                SyncGridSettings(mainView.ViewModel);
            }
        }
    }

    protected override void Update(float dt)
    {
        _editorController.UpdateCamera(dt);

        if (!_mouseOverUi && !_gizmoDragging)
        {
            UpdateGizmoHover();
        }

        _renderer.EditorViewModel?.UpdateStatistics();
        _renderer.EditorViewModel?.Update(dt);

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
                HandleGizmoDrag(_shiftHeld);
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

            if (ev.Key.KeyCode == KeyCode.Lshift || ev.Key.KeyCode == KeyCode.Rshift)
            {
                _shiftHeld = true;
            }

            if (!_textInputActive)
            {
                HandleGizmoModeKey(ev.Key.KeyCode);

                if (ev.Key.KeyCode == KeyCode.F)
                {
                    FocusOnSelectedObject();
                }
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

            if (ev.Key.KeyCode == KeyCode.Lshift || ev.Key.KeyCode == KeyCode.Rshift)
            {
                _shiftHeld = false;
            }
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
            _editorController.HandleEvent(in ev);
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

    private void HandleGizmoDrag(bool shiftHeld)
    {
        var gizmoPass = _renderer.GizmoPass;
        if (gizmoPass == null)
        {
            return;
        }

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        gizmoPass.Gizmo.UpdateDrag(ray, _editorCamera, shiftHeld);

        _renderer.EditorViewModel?.SelectedGameObject?.Refresh();
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

        _renderer.EditorViewModel?.SelectedGameObject?.Refresh();
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

        _renderer.EditorViewModel?.UpdateGizmoModeText(gizmoPass.Gizmo.Mode);
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

    private void FocusOnSelectedObject()
    {
        var selected = _renderer.EditorViewModel?.SelectedGameObject?.GameObject;
        if (selected == null)
        {
            return;
        }

        var targetPosition = selected.WorldPosition;
        var distance = 5f;

        var meshComponent = selected.GetComponent<MeshComponent>();
        if (meshComponent?.Mesh != null)
        {
            var bounds = meshComponent.Mesh.Bounds;
            var size = bounds.Max - bounds.Min;
            var scale = selected.LocalScale;
            var maxExtent = MathF.Max(size.X * scale.X, MathF.Max(size.Y * scale.Y, size.Z * scale.Z));
            distance = maxExtent * 2f;
            distance = MathF.Max(distance, 2f);

            var center = (bounds.Min + bounds.Max) * 0.5f;
            targetPosition = Vector3.Transform(center, selected.WorldMatrix);
        }

        var directionToTarget = targetPosition - _editorCameraObject.LocalPosition;
        if (directionToTarget.LengthSquared() > 0.001f)
        {
            directionToTarget = Vector3.Normalize(directionToTarget);
        }
        else
        {
            directionToTarget = _editorController.Forward;
        }

        var cameraPosition = targetPosition - directionToTarget * distance;
        _editorController.SetPositionAndLookAt(cameraPosition, targetPosition, immediate: false);
    }

    private void OnViewPresetChanged()
    {
        var vm = _renderer.EditorViewModel;
        if (vm == null)
        {
            return;
        }

        var preset = vm.CurrentViewPreset;
        var target = Vector3.Zero;
        var distance = 20f;

        // Get position and direction based on preset
        var (position, rotation) = preset switch
        {
            ViewPreset.Top => (new Vector3(0, distance, 0), Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 2, 0)),
            ViewPreset.Bottom => (new Vector3(0, -distance, 0), Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 2, 0)),
            ViewPreset.Front => (new Vector3(0, 0, -distance), Quaternion.Identity),
            ViewPreset.Back => (new Vector3(0, 0, distance), Quaternion.CreateFromYawPitchRoll(MathF.PI, 0, 0)),
            ViewPreset.Right => (new Vector3(distance, 0, 0), Quaternion.CreateFromYawPitchRoll(-MathF.PI / 2, 0, 0)),
            ViewPreset.Left => (new Vector3(-distance, 0, 0), Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, 0, 0)),
            _ => (new Vector3(0, 15, -15), Quaternion.Identity) // Free/Perspective
        };

        // For orthographic presets, set orthographic mode
        if (preset != ViewPreset.Free)
        {
            _editorCamera.ProjectionType = ProjectionType.Orthographic;
            _editorCamera.OrthographicSize = 10f;
            vm.Is2DMode = true;
        }
        else
        {
            _editorCamera.ProjectionType = ProjectionType.Perspective;
            vm.Is2DMode = false;
        }

        _editorController.SetPositionAndLookAt(position, target, immediate: true);
    }

    private void OnProjectionModeChanged()
    {
        var vm = _renderer.EditorViewModel;
        if (vm == null)
        {
            return;
        }

        if (vm.Is2DMode)
        {
            _editorCamera.ProjectionType = ProjectionType.Orthographic;
            _editorCamera.OrthographicSize = 10f;

            // Switch to Top view if currently in Free mode
            if (vm.CurrentViewPreset == ViewPreset.Free)
            {
                vm.CurrentViewPreset = ViewPreset.Top;
                OnViewPresetChanged();
            }
        }
        else
        {
            _editorCamera.ProjectionType = ProjectionType.Perspective;
            vm.CurrentViewPreset = ViewPreset.Free;
        }
    }

    private void OnGridSettingsChanged()
    {
        var vm = _renderer.EditorViewModel;
        if (vm == null)
        {
            return;
        }

        SyncGridSettings(vm);
    }

    private void SyncGridSettings(EditorViewModel vm)
    {
        var gizmoPass = _renderer.GizmoPass;
        gizmoPass.ShowGrid = vm.ShowGrid;
        gizmoPass.GridSize = vm.GridSize;
        gizmoPass.GridSpacing = vm.GridSpacing;
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

        if (_renderer.EditorViewModel != null)
        {
            _renderer.EditorViewModel.ViewPresetChanged -= OnViewPresetChanged;
            _renderer.EditorViewModel.ProjectionModeChanged -= OnProjectionModeChanged;
            _renderer.EditorViewModel.GridSettingsChanged -= OnGridSettingsChanged;
        }

        _topLevel.TextInputActiveChanged -= OnTextInputActiveChanged;
        _renderer.Dispose();
        _skiaContext.Dispose();
    }
}
