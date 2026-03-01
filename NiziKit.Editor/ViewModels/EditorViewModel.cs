using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.Services;
using NiziKit.Light;

namespace NiziKit.Editor.ViewModels;

public enum ViewPreset
{
    Free,
    Top,
    Bottom,
    Front,
    Back,
    Right,
    Left
}

public class EditorViewModel
{
    private readonly EditorSceneService _sceneService;
    private readonly AssetBrowserService _assetBrowserService;
    private readonly AssetFileService _assetFileService;

    public UndoRedoSystem UndoSystem { get; } = new();

    public EditorViewModel()
    {
        _sceneService = new EditorSceneService();
        _assetBrowserService = new AssetBrowserService();
        _assetFileService = new AssetFileService();

        AssetBrowserViewModel = new AssetBrowserViewModel();
        ImportViewModel = new ImportViewModel();

        ImportViewModel.ImportCompleted += () =>
        {
            AssetBrowserViewModel.Refresh();
            IsImportPanelOpen = false;
        };
        ImportViewModel.ImportCancelled += () => IsImportPanelOpen = false;
    }

    public GameObjectViewModel? SelectedGameObject { get; set; }
    public bool HasSelection => SelectedGameObject != null;

    public string SceneDisplayName { get; set; } = "Scene";

    public List<GameObjectViewModel> RootObjects { get; } = [];

    public AssetBrowserService AssetBrowser => _assetBrowserService;

    // Asset Management ViewModels
    public AssetBrowserViewModel AssetBrowserViewModel { get; set; }
    public ImportViewModel ImportViewModel { get; set; }

    public bool IsImportPanelOpen { get; set; }
    public bool IsOpenSceneDialogOpen { get; set; }
    public bool IsSavePromptOpen { get; set; }

    public FileBrowserViewModel? SceneBrowserViewModel { get; set; }

    public bool IsDirty { get; set; }

    private string? _pendingScenePath;

    private float _autoSaveTimer;
    private float _contentRefreshTimer;
    private bool _isAutoSaving;
    private bool _isRefreshingContent;
    private const float AutoSaveInterval = 60f;
    private const float ContentRefreshInterval = 5f;

    public bool AutoSaveEnabled { get; set; } = true;
    public string AutoSaveStatus { get; set; } = "";

    // Bottom panel states
    public bool IsAssetBrowserOpen { get; set; }

    // Asset Picker state
    public bool IsAssetPickerOpen { get; set; }
    public AssetRefType AssetPickerAssetType { get; set; }
    public string? AssetPickerCurrentAssetPath { get; set; }
    private Action<AssetInfo?>? _assetPickerCallback;

    public void LoadFromCurrentScene()
    {
        RootObjects.Clear();
        var scene = World.CurrentScene;
        SceneDisplayName = scene.Name ?? "Scene";

        foreach (var obj in scene.RootObjects)
        {
            DisableCameraControllersRecursive(obj);
            RootObjects.Add(new GameObjectViewModel(obj, this));
        }
    }

    private static void DisableCameraControllersRecursive(GameObject obj)
    {
        var freeFly = obj.GetComponent<FreeFlyController>();
        if (freeFly != null)
        {
            freeFly.IsEnabled = false;
        }

        var orbit = obj.GetComponent<OrbitController>();
        if (orbit != null)
        {
            orbit.IsEnabled = false;
        }

        foreach (var child in obj.Children)
        {
            DisableCameraControllersRecursive(child);
        }
    }

    public void NewObject()
    {
        var scene = World.CurrentScene;
        var obj = scene.CreateObject("New Object");
        var vm = new GameObjectViewModel(obj, this);
        RootObjects.Add(vm);
        SelectObject(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Create Object"));
    }

    public void NewChildObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var parent = SelectedGameObject;
        var child = parent.GameObject.CreateChild("New Child");
        var vm = new GameObjectViewModel(child, this);
        parent.Children.Add(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, parent, "Create Child Object"));
    }

    public void NewSpriteObject()
    {
        var scene = World.CurrentScene;
        var obj = scene.CreateObject("New Sprite");
        var sprite = obj.AddComponent<SpriteComponent>();
        sprite.Color = System.Numerics.Vector4.One;
        sprite.Size = new System.Numerics.Vector2(1, 1);

        var vm = new GameObjectViewModel(obj, this);
        RootObjects.Add(vm);
        SelectObject(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Create Sprite"));
    }

    public void NewDirectionalLight()
    {
        var scene = World.CurrentScene;
        var light = scene.CreateObject<DirectionalLight>("Directional Light");
        light.LookAt(new System.Numerics.Vector3(0.5f, -1f, 0.5f));
        var vm = new GameObjectViewModel(light, this);
        RootObjects.Add(vm);
        SelectObject(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Create Directional Light"));
    }

    public void NewPointLight()
    {
        var scene = World.CurrentScene;
        var light = scene.CreateObject<PointLight>("Point Light");
        light.LocalPosition = new System.Numerics.Vector3(0, 3, 0);
        var vm = new GameObjectViewModel(light, this);
        RootObjects.Add(vm);
        SelectObject(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Create Point Light"));
    }

    public void NewSpotLight()
    {
        var scene = World.CurrentScene;
        var light = scene.CreateObject<SpotLight>("Spot Light");
        light.LocalPosition = new System.Numerics.Vector3(0, 3, 0);
        light.LookAt(new System.Numerics.Vector3(0, -1, 0));
        var vm = new GameObjectViewModel(light, this);
        RootObjects.Add(vm);
        SelectObject(vm);

        UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Create Spot Light"));
    }

    public void DeleteObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var scene = World.CurrentScene;
        var objectVm = SelectedGameObject;
        var parent = FindParent(objectVm);
        int index;

        if (parent != null)
        {
            index = parent.Children.IndexOf(objectVm);
            parent.RemoveChild(objectVm);
        }
        else
        {
            index = RootObjects.IndexOf(objectVm);
            scene.Destroy(objectVm.GameObject);
            RootObjects.Remove(objectVm);
        }

        SelectObject(null);
        UndoSystem.Execute(new DeleteObjectAction(this, objectVm, parent, index));
    }

    public void DuplicateObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var scene = World.CurrentScene;
        var original = SelectedGameObject.GameObject;
        var clone = _sceneService.CloneGameObject(original);

        var parent = FindParent(SelectedGameObject);
        if (parent != null)
        {
            parent.GameObject.AddChild(clone);
            var vm = new GameObjectViewModel(clone, this);
            parent.Children.Add(vm);
            SelectObject(vm);

            UndoSystem.Execute(new CreateObjectAction(this, vm, parent, "Duplicate Object"));
        }
        else
        {
            scene.Add(clone);
            var vm = new GameObjectViewModel(clone, this);
            RootObjects.Add(vm);
            SelectObject(vm);

            UndoSystem.Execute(new CreateObjectAction(this, vm, null, "Duplicate Object"));
        }
    }

    public void SaveScene()
    {
        var scene = World.CurrentScene;
        _sceneService.SaveScene(scene);
        IsDirty = false;
    }

    public void Undo()
    {
        UndoSystem.Undo();
        MarkDirty();
    }

    public void Redo()
    {
        UndoSystem.Redo();
        MarkDirty();
    }

    public void LoadScene(string path)
    {
        World.LoadScene(path);
        LoadFromCurrentScene();
        UndoSystem.Clear();
        IsDirty = false;
    }

    public void OpenScene()
    {
        SceneBrowserViewModel = new FileBrowserViewModel(_assetFileService)
        {
            Filter = AssetFileType.Scene
        };
        IsOpenSceneDialogOpen = true;
    }

    public void CloseOpenSceneDialog()
    {
        IsOpenSceneDialogOpen = false;
        SceneBrowserViewModel = null;
    }

    public void OnSceneFileSelected(string path)
    {
        IsOpenSceneDialogOpen = false;

        if (IsDirty)
        {
            _pendingScenePath = path;
            IsSavePromptOpen = true;
        }
        else
        {
            LoadSceneFromPath(path);
        }
    }

    public void SavePromptSave()
    {
        SaveScene();
        IsSavePromptOpen = false;
        if (_pendingScenePath != null)
        {
            LoadSceneFromPath(_pendingScenePath);
            _pendingScenePath = null;
        }
    }

    public void SavePromptDiscard()
    {
        IsSavePromptOpen = false;
        if (_pendingScenePath != null)
        {
            LoadSceneFromPath(_pendingScenePath);
            _pendingScenePath = null;
        }
    }

    public void SavePromptCancel()
    {
        IsSavePromptOpen = false;
        _pendingScenePath = null;
    }

    private void LoadSceneFromPath(string path)
    {
        World.CurrentScene?.Dispose();
        World.LoadScene(path);
        LoadFromCurrentScene();
        UndoSystem.Clear();
        IsDirty = false;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void Update(float deltaTime)
    {
        if (AutoSaveEnabled && IsDirty && !_isAutoSaving)
        {
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0f;
                _ = PerformAutoSaveAsync();
            }
        }

        if (!_isRefreshingContent)
        {
            _contentRefreshTimer += deltaTime;
            if (_contentRefreshTimer >= ContentRefreshInterval)
            {
                _contentRefreshTimer = 0f;
                _ = RefreshContentCachesAsync();
            }
        }
    }

    private async Task PerformAutoSaveAsync()
    {
        var scene = World.CurrentScene;
        if (scene == null || _isAutoSaving)
        {
            return;
        }

        _isAutoSaving = true;
        AutoSaveStatus = "Auto-saving...";

        try
        {
            await Task.Run(async () =>
            {
                await _sceneService.SaveSceneAsync(scene);
            });

            IsDirty = false;
            AutoSaveStatus = $"Auto-saved at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            AutoSaveStatus = $"Auto-save failed: {ex.Message}";
        }
        finally
        {
            _isAutoSaving = false;
        }
    }

    private async Task RefreshContentCachesAsync()
    {
        if (_isRefreshingContent)
        {
            return;
        }

        _isRefreshingContent = true;

        try
        {
            await Task.Run(() =>
            {
                if (IsAssetBrowserOpen)
                {
                    AssetBrowserViewModel.Refresh();
                }
            });
        }
        catch
        {
        }
        finally
        {
            _isRefreshingContent = false;
        }
    }

    public void OpenImportPanel()
    {
        IsImportPanelOpen = true;
    }

    public void OpenImportPanelWithFiles(IEnumerable<string> paths)
    {
        IsImportPanelOpen = true;
        ImportViewModel.AddFilesToQueue(paths);
    }

    public void CloseImportPanel()
    {
        IsImportPanelOpen = false;
    }

    public void CloseAssetBrowser()
    {
        IsAssetBrowserOpen = false;
    }

    public void OpenAssetPicker(AssetRefType assetType, string? currentAssetPath, Action<AssetInfo?> callback)
    {
        AssetPickerAssetType = assetType;
        AssetPickerCurrentAssetPath = currentAssetPath;
        _assetPickerCallback = callback;
        IsAssetPickerOpen = true;
    }

    public void CloseAssetPicker()
    {
        IsAssetPickerOpen = false;
        _assetPickerCallback = null;
    }

    public void OnAssetPickerSelected(AssetInfo? asset)
    {
        _assetPickerCallback?.Invoke(asset);
        IsAssetPickerOpen = false;
        _assetPickerCallback = null;
    }

    public void RefreshAssets()
    {
        AssetBrowserViewModel.Refresh();
    }

    public bool Is2DMode { get; set; }
    public bool Is3DMode => !Is2DMode;
    public string ProjectionModeText => Is2DMode ? "2D" : "3D";

    public ViewPreset CurrentViewPreset { get; set; } = ViewPreset.Free;

    public string ViewPresetText => CurrentViewPreset switch
    {
        ViewPreset.Free => "Persp",
        ViewPreset.Top => "Top",
        ViewPreset.Bottom => "Bottom",
        ViewPreset.Front => "Front",
        ViewPreset.Back => "Back",
        ViewPreset.Right => "Right",
        ViewPreset.Left => "Left",
        _ => "Free"
    };

    public bool ShowStatistics { get; set; } = true;
    public float Fps { get; set; }
    public float FrameTime { get; set; }
    public string GizmoModeText { get; set; } = "Move (W) · Local (X)";

    public bool SnapEnabled { get; set; }
    public bool ShowGrid { get; set; } = true;
    public float PositionSnapIncrement { get; set; } = 1f;
    public float RotationSnapIncrement { get; set; } = 15f;
    public float ScaleSnapIncrement { get; set; } = 0.1f;
    public float GridSize { get; set; } = 50f;
    public float GridSpacing { get; set; } = 1f;

    public string SnapStatusText => SnapEnabled ? "Snap: On" : "Snap: Off";
    public string GridStatusText => ShowGrid ? "Grid: On" : "Grid: Off";

    public event Action? GridSettingsChanged;
    public event Action? ViewPresetChanged;
    public event Action? ProjectionModeChanged;

    private GridDesc? _gridDesc;

    public void SetGridDesc(GridDesc desc)
    {
        _gridDesc = desc;
        SnapEnabled = desc.SnapEnabled;
        PositionSnapIncrement = GridSpacing;
        desc.PositionSnapIncrement = GridSpacing;
        RotationSnapIncrement = desc.RotationSnapIncrement;
        ScaleSnapIncrement = desc.ScaleSnapIncrement;
    }

    public void SyncSnapToGrid()
    {
        if (_gridDesc != null)
        {
            _gridDesc.SnapEnabled = SnapEnabled;
            _gridDesc.PositionSnapIncrement = PositionSnapIncrement;
            _gridDesc.RotationSnapIncrement = RotationSnapIncrement;
            _gridDesc.ScaleSnapIncrement = ScaleSnapIncrement;
        }
    }

    public void NotifyGridSettingsChanged()
    {
        PositionSnapIncrement = GridSpacing;
        GridSettingsChanged?.Invoke();
    }

    public void ToggleSnap()
    {
        SnapEnabled = !SnapEnabled;
        SyncSnapToGrid();
    }

    public void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
        GridSettingsChanged?.Invoke();
    }

    private float _statsUpdateTimer;
    private int _frameCountSinceLastUpdate;
    private const float StatsUpdateInterval = 0.5f;

    public void UpdateStatistics()
    {
        _statsUpdateTimer += Time.DeltaTime;
        _frameCountSinceLastUpdate++;

        if (_statsUpdateTimer >= StatsUpdateInterval)
        {
            Fps = _frameCountSinceLastUpdate / _statsUpdateTimer;
            FrameTime = _statsUpdateTimer / _frameCountSinceLastUpdate * 1000f;
            _statsUpdateTimer = 0f;
            _frameCountSinceLastUpdate = 0;
        }
    }

    public void UpdateGizmoModeText(GizmoMode mode, GizmoSpace space)
    {
        var modeText = mode switch
        {
            GizmoMode.Translate => "Move (W)",
            GizmoMode.Rotate => "Rotate (E)",
            GizmoMode.Scale => "Scale (R)",
            _ => "Move (W)"
        };
        var spaceText = space == GizmoSpace.Local ? "Local" : "World";
        GizmoModeText = $"{modeText} · {spaceText} (X)";
    }

    public void Toggle2DMode()
    {
        Is2DMode = !Is2DMode;
        if (Is2DMode && CurrentViewPreset == ViewPreset.Free)
        {
            CurrentViewPreset = ViewPreset.Top;
        }
        ProjectionModeChanged?.Invoke();
    }

    public void SetViewPreset(string preset)
    {
        CurrentViewPreset = preset switch
        {
            "Free" => ViewPreset.Free,
            "Top" => ViewPreset.Top,
            "Bottom" => ViewPreset.Bottom,
            "Front" => ViewPreset.Front,
            "Back" => ViewPreset.Back,
            "Right" => ViewPreset.Right,
            "Left" => ViewPreset.Left,
            _ => ViewPreset.Free
        };
        ViewPresetChanged?.Invoke();
    }

    public void ToggleStatistics()
    {
        ShowStatistics = !ShowStatistics;
    }

    private GameObjectViewModel? FindParent(GameObjectViewModel target)
    {
        foreach (var root in RootObjects)
        {
            var parent = FindParentRecursive(root, target);
            if (parent != null)
            {
                return parent;
            }
        }
        return null;
    }

    private GameObjectViewModel? FindParentRecursive(GameObjectViewModel current, GameObjectViewModel target)
    {
        foreach (var child in current.Children)
        {
            if (child == target)
            {
                return current;
            }

            var found = FindParentRecursive(child, target);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public void SelectObject(GameObjectViewModel? vm)
    {
        if (SelectedGameObject != null)
        {
            SelectedGameObject.IsSelected = false;
        }
        SelectedGameObject = vm;
        if (vm != null)
        {
            vm.IsSelected = true;
        }
    }
}
