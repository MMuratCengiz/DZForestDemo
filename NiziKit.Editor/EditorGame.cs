using System.Numerics;
using DenOfIz;
using NiziKit.Application;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.Services;
using NiziKit.Editor.UI;
using NiziKit.Editor.ViewModels;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer;
using NiziKit.Inputs;
using NiziKit.Physics;
using NiziKit.UI;

namespace NiziKit.Editor;

public sealed class EditorGame(GameDesc? desc = null) : Game(desc)
{
    private EditorRenderer _renderer = null!;
    private IRenderer _gameRenderer = null!;
    private EditorViewModel _viewModel = null!;
    private GameObject _editorCameraObject = null!;
    private CameraComponent _editorCamera = null!;
    private FreeFlyController _editorController = null!;

    private uint _width;
    private uint _height;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _mouseOverUi;
    private bool _textInputActive;
    private bool _gizmoDragging;
    private bool _shiftHeld;
    private bool _ctrlHeld;
    private bool _keyConsumed;

    private Vector3 _gizmoDragStartPosition;
    private Quaternion _gizmoDragStartRotation;
    private Vector3 _gizmoDragStartScale;

    protected override void Load(Game game)
    {
        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;

        _gameRenderer = (IRenderer)Activator.CreateInstance(RendererType)!;
        _renderer = new EditorRenderer(_gameRenderer);

        _renderer.RenderFrame.EnableUi(UiContextDesc.Default);
        FontAwesome.InitializeEmbedded(_renderer.RenderFrame.UiContext.Clay);

        _editorCameraObject = new GameObject("EditorCamera")
        {
            LocalPosition = new Vector3(0, 15, -15)
        };

        _editorCamera = _editorCameraObject.AddComponent<CameraComponent>();
        _editorCamera.Priority = 1000;

        _editorController = _editorCameraObject.AddComponent<FreeFlyController>();
        _editorController.MoveSpeed = 10f;
        _editorController.SetPositionAndLookAt(new Vector3(0, 15, -15), Vector3.Zero, immediate: true);

        _editorCamera.SetAspectRatio(_width, _height);
        _renderer.Camera = _editorCamera;

        Assets.Pack.AssetPacks.LoadFromManifest();

        if (!string.IsNullOrEmpty(Editor.Desc.InitialScene))
        {
            World.LoadScene(Editor.Desc.InitialScene);
        }

        // Disable player input when running in Editor mode
        Input.Player1.IsEnabled = false;

        _viewModel = new EditorViewModel();
        _viewModel.LoadFromCurrentScene();
        _renderer.EditorViewModel = _viewModel;

        _viewModel.ViewPresetChanged += OnViewPresetChanged;
        _viewModel.ProjectionModeChanged += OnProjectionModeChanged;
        _viewModel.GridSettingsChanged += OnGridSettingsChanged;
        _viewModel.SetGridDesc(_renderer.EditorOverlayPass.Gizmo.GridDesc);
        SyncGridSettings(_viewModel);
    }

    protected override void Update(float dt)
    {
        _editorController.UpdateCamera(dt);

        if (!_mouseOverUi && !_gizmoDragging)
        {
            UpdateGizmoHover();
        }

        _viewModel.UpdateStatistics();
        _viewModel.Update(dt);

        var renderFrame = _renderer.RenderFrame;
        renderFrame.BeginFrame();

        var sceneColor = _renderer.RenderScene();
        var ui = renderFrame.RenderUi(uiFrame => EditorUiBuilder.Build(uiFrame, renderFrame.UiContext, _viewModel));

        renderFrame.AlphaBlit(ui, sceneColor);

        renderFrame.Submit();
        renderFrame.Present(sceneColor);
    }

    private void UpdateGizmoHover()
    {
        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        _renderer.EditorOverlayPass.Gizmo.UpdateHover(ray, _editorCamera);
    }

    private void UpdateMouseOverUi()
    {
        var ctx = _renderer.RenderFrame.UiContext;
        var viewportId = ctx.GetElementId("ViewportFill");
        var overViewport = ctx.Clay.PointerOver(viewportId);

        var hasDialog = _viewModel.IsSavePromptOpen
                        || _viewModel.IsOpenSceneDialogOpen
                        || _viewModel.IsImportPanelOpen
                        || _viewModel.IsAssetPickerOpen;

        _mouseOverUi = !overViewport || hasDialog || ctx.IsPointerOverUi;
    }

    protected override void OnEvent(ref Event ev)
    {
        var renderFrame = _renderer.RenderFrame;

        if (ev.Type == EventType.MouseMotion)
        {
            _lastMouseX = ev.MouseMotion.X;
            _lastMouseY = ev.MouseMotion.Y;

            renderFrame.HandleUiEvent(ev);
            UpdateMouseOverUi();

            if (_gizmoDragging)
            {
                HandleGizmoDrag(_shiftHeld);
            }
        }
        else if (ev.Type == EventType.MouseButtonDown)
        {
            renderFrame.HandleUiEvent(ev);
            UpdateMouseOverUi();

            if (!_mouseOverUi && ev.MouseButton.Button == MouseButton.Left)
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
            renderFrame.HandleUiEvent(ev);
            UpdateMouseOverUi();

            if (ev.MouseButton.Button == MouseButton.Left && _gizmoDragging)
            {
                EndGizmoDrag();
            }
        }
        else if (ev.Type == EventType.MouseWheel)
        {
            renderFrame.HandleUiEvent(ev);
        }
        else if (ev.Type == EventType.KeyDown)
        {
            renderFrame.HandleUiEvent(ev);

            if (ev.Key.KeyCode == KeyCode.Lshift || ev.Key.KeyCode == KeyCode.Rshift)
            {
                _shiftHeld = true;
            }

            if (ev.Key.KeyCode == KeyCode.Lctrl || ev.Key.KeyCode == KeyCode.Rctrl)
            {
                _ctrlHeld = true;
            }

            if (!_textInputActive)
            {
                if (HandleKeyboardShortcuts(ev.Key.KeyCode))
                {
                    _keyConsumed = true;
                }

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
            renderFrame.HandleUiEvent(ev);

            if (ev.Key.KeyCode == KeyCode.Lshift || ev.Key.KeyCode == KeyCode.Rshift)
            {
                _shiftHeld = false;
            }

            if (ev.Key.KeyCode == KeyCode.Lctrl || ev.Key.KeyCode == KeyCode.Rctrl)
            {
                _ctrlHeld = false;
            }

            _keyConsumed = false;
        }
        else if (ev.Type == EventType.TextInput)
        {
            renderFrame.HandleUiEvent(ev);
        }
        else if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.SizeChanged })
        {
            var width = (uint)ev.Window.Data1;
            var height = (uint)ev.Window.Data2;
            OnResize(width, height);
        }

        _textInputActive = renderFrame.UiContext.FocusedTextFieldId != 0;

        if (!UiWantsInput && !_gizmoDragging && !_keyConsumed)
        {
            _editorController.HandleEvent(in ev);
        }
    }

    private bool TryBeginGizmoDrag()
    {
        var gizmoPass = _renderer.EditorOverlayPass;
        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        if (gizmoPass.Gizmo.BeginDrag(ray, _editorCamera))
        {
            _gizmoDragging = true;
            PhysicsPaused = true;

            var selected = _viewModel.SelectedGameObject?.GameObject;
            if (selected != null)
            {
                _gizmoDragStartPosition = selected.LocalPosition;
                _gizmoDragStartRotation = selected.LocalRotation;
                _gizmoDragStartScale = selected.LocalScale;
            }

            return true;
        }

        return false;
    }

    private void HandleGizmoDrag(bool shiftHeld)
    {
        var gizmoPass = _renderer.EditorOverlayPass;

        var ray = _editorCamera.ScreenPointToRay(_lastMouseX, _lastMouseY, _width, _height);
        gizmoPass.Gizmo.UpdateDrag(ray, _editorCamera, shiftHeld);

        var selected = _viewModel.SelectedGameObject?.GameObject;
        if (selected != null)
        {
            World.PhysicsWorld.SyncEditorTransform(selected.Id, selected.LocalPosition, selected.LocalRotation);
        }
    }

    private void EndGizmoDrag()
    {
        var gizmoPass = _renderer.EditorOverlayPass;
        gizmoPass?.Gizmo.EndDrag();
        _gizmoDragging = false;
        PhysicsPaused = false;

        var selected = _viewModel.SelectedGameObject?.GameObject;
        if (selected != null)
        {
            var newPos = selected.LocalPosition;
            var newRot = selected.LocalRotation;
            var newScl = selected.LocalScale;

            if (newPos != _gizmoDragStartPosition || newRot != _gizmoDragStartRotation ||
                newScl != _gizmoDragStartScale)
            {
                _viewModel.UndoSystem.Execute(new GizmoTransformAction(selected,
                    _gizmoDragStartPosition, _gizmoDragStartRotation, _gizmoDragStartScale,
                    newPos, newRot, newScl));
                _viewModel.MarkDirty();
            }

            World.PhysicsWorld.SyncEditorTransform(selected.Id, newPos, newRot);
        }
    }

    private void CancelGizmoDrag()
    {
        var gizmoPass = _renderer.EditorOverlayPass;
        gizmoPass?.Gizmo.CancelDrag();
        _gizmoDragging = false;
        PhysicsPaused = false;

        var selected = _viewModel.SelectedGameObject?.GameObject;
        if (selected != null)
        {
            World.PhysicsWorld.SyncEditorTransform(selected.Id, selected.LocalPosition, selected.LocalRotation);
        }
    }

    private bool HandleKeyboardShortcuts(KeyCode keyCode)
    {
        if (_ctrlHeld)
        {
            switch (keyCode)
            {
                case KeyCode.Z when _shiftHeld:
                    _viewModel.Redo();
                    return true;
                case KeyCode.Z:
                    _viewModel.Undo();
                    return true;
                case KeyCode.Y:
                    _viewModel.Redo();
                    return true;
                case KeyCode.S:
                    _viewModel.SaveScene();
                    return true;
                case KeyCode.D:
                    _viewModel.DuplicateObject();
                    return true;
            }
        }

        if (keyCode == KeyCode.Delete)
        {
            _viewModel.DeleteObject();
            return true;
        }

        return false;
    }

    private void HandleGizmoModeKey(KeyCode keyCode)
    {
        var gizmoPass = _renderer.EditorOverlayPass;
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
            case KeyCode.X:
                gizmoPass.Gizmo.Space = gizmoPass.Gizmo.Space == GizmoSpace.Local
                    ? GizmoSpace.World
                    : GizmoSpace.Local;
                break;
        }

        _viewModel.UpdateGizmoModeText(gizmoPass.Gizmo.Mode, gizmoPass.Gizmo.Space);
    }

    private void TrySelectMeshAtCursor()
    {
        var scene = World.CurrentScene;
        if (scene == null)
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

            if (TransformGizmo.RayBoundsIntersection(ray, meshComponent.Mesh.Bounds, gameObject.WorldMatrix,
                    out var distance))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = gameObject;
                }
            }
        }

        foreach (var obj in scene.RootObjects)
        {
            if (obj.GetComponent<MeshComponent>() != null)
            {
                continue;
            }

            if (RayPointIntersection(ray, obj.WorldPosition, 0.5f, out var distance) && distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }

        if (closestObject != null)
        {
            var objectVm = FindGameObjectViewModel(_viewModel.RootObjects, closestObject);
            _viewModel.SelectObject(objectVm);
        }
        else
        {
            _viewModel.SelectObject(null);
        }
    }

    private static bool RayPointIntersection(Ray ray, Vector3 point, float radius, out float distance)
    {
        distance = float.MaxValue;
        var oc = ray.Origin - point;
        var b = Vector3.Dot(oc, ray.Direction);
        var c = Vector3.Dot(oc, oc) - radius * radius;
        var discriminant = b * b - c;

        if (discriminant < 0)
        {
            return false;
        }

        var t = -b - MathF.Sqrt(discriminant);
        if (t < 0)
        {
            t = -b + MathF.Sqrt(discriminant);
        }

        if (t < 0)
        {
            return false;
        }

        distance = t;
        return true;
    }

    private static GameObjectViewModel? FindGameObjectViewModel(IEnumerable<GameObjectViewModel> viewModels,
        GameObject target)
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
        var selected = _viewModel.SelectedGameObject?.GameObject;
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
        var preset = _viewModel.CurrentViewPreset;
        var target = Vector3.Zero;
        var distance = 20f;

        var (position, rotation) = preset switch
        {
            ViewPreset.Top => (new Vector3(0, distance, 0), Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 2, 0)),
            ViewPreset.Bottom => (new Vector3(0, -distance, 0), Quaternion.CreateFromYawPitchRoll(0, -MathF.PI / 2, 0)),
            ViewPreset.Front => (new Vector3(0, 0, -distance), Quaternion.Identity),
            ViewPreset.Back => (new Vector3(0, 0, distance), Quaternion.CreateFromYawPitchRoll(MathF.PI, 0, 0)),
            ViewPreset.Right => (new Vector3(distance, 0, 0), Quaternion.CreateFromYawPitchRoll(-MathF.PI / 2, 0, 0)),
            ViewPreset.Left => (new Vector3(-distance, 0, 0), Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, 0, 0)),
            _ => (new Vector3(0, 15, -15), Quaternion.Identity)
        };

        if (preset != ViewPreset.Free)
        {
            _editorCamera.ProjectionType = ProjectionType.Orthographic;
            _editorCamera.OrthographicSize = 10f;
            _viewModel.Is2DMode = true;
        }
        else
        {
            _editorCamera.ProjectionType = ProjectionType.Perspective;
            _viewModel.Is2DMode = false;
        }

        var gizmoPass = _renderer.EditorOverlayPass;
        gizmoPass.GridOrientation = preset switch
        {
            ViewPreset.Front or ViewPreset.Back => Matrix4x4.CreateRotationX(MathF.PI / 2),
            ViewPreset.Right or ViewPreset.Left => Matrix4x4.CreateRotationZ(MathF.PI / 2),
            _ => Matrix4x4.Identity
        };

        _editorController.SetPositionAndLookAt(position, target, immediate: true);
    }

    private void OnProjectionModeChanged()
    {
        if (_viewModel.Is2DMode)
        {
            _editorCamera.ProjectionType = ProjectionType.Orthographic;
            _editorCamera.OrthographicSize = 10f;

            if (_viewModel.CurrentViewPreset == ViewPreset.Free)
            {
                _viewModel.CurrentViewPreset = ViewPreset.Top;
                OnViewPresetChanged();
            }
        }
        else
        {
            _editorCamera.ProjectionType = ProjectionType.Perspective;
            _viewModel.CurrentViewPreset = ViewPreset.Free;
            _renderer.EditorOverlayPass.GridOrientation = Matrix4x4.Identity;
        }
    }

    private void OnGridSettingsChanged()
    {
        SyncGridSettings(_viewModel);
    }

    private void SyncGridSettings(EditorViewModel vm)
    {
        var gizmoPass = _renderer.EditorOverlayPass;
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

        _width = width;
        _height = height;

        _editorCamera.SetAspectRatio(width, height);
        _renderer.OnResize(width, height);
        _renderer.RenderFrame.UiContext.SetViewportSize(width, height);
    }

    private bool UiWantsInput => _mouseOverUi || _textInputActive;

    protected override void OnShutdown()
    {
        GraphicsContext.WaitIdle();

        _viewModel.ViewPresetChanged -= OnViewPresetChanged;
        _viewModel.ProjectionModeChanged -= OnProjectionModeChanged;
        _viewModel.GridSettingsChanged -= OnGridSettingsChanged;

        FontAwesome.Shutdown();
        _renderer.Dispose();
    }
}
