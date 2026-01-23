using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.Services;
using NiziKit.Editor.Views.Editors;

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

public partial class EditorViewModel : ObservableObject
{
    private readonly EditorSceneService _sceneService;
    private readonly AssetBrowserService _assetBrowserService;
    private readonly AssetFileService _assetFileService;
    private readonly List<AnimationPreviewEditor> _animationPreviewEditors = [];

    public EditorViewModel()
    {
        _sceneService = new EditorSceneService();
        _assetBrowserService = new AssetBrowserService();
        _assetFileService = new AssetFileService();

        AssetBrowserViewModel = new AssetBrowserViewModel();
        PackManagerViewModel = new PackManagerViewModel();
        ContentBrowserViewModel = new ContentBrowserViewModel();
        ImportViewModel = new ImportViewModel();

        ImportViewModel.ImportCompleted += () =>
        {
            AssetBrowserViewModel.Refresh();
            IsImportPanelOpen = false;
        };
        ImportViewModel.ImportCancelled += () => IsImportPanelOpen = false;
    }

    public void RegisterAnimationPreview(AnimationPreviewEditor editor)
    {
        if (!_animationPreviewEditors.Contains(editor))
        {
            _animationPreviewEditors.Add(editor);
        }
    }

    public void UnregisterAnimationPreview(AnimationPreviewEditor editor)
    {
        _animationPreviewEditors.Remove(editor);
    }

    public void UpdateAnimationPreviews(float deltaTime)
    {
        foreach (var editor in _animationPreviewEditors)
        {
            editor.Update(deltaTime);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private GameObjectViewModel? _selectedGameObject;

    public bool HasSelection => SelectedGameObject != null;

    public ObservableCollection<GameObjectViewModel> RootObjects { get; } = [];

    public AssetBrowserService AssetBrowser => _assetBrowserService;

    // Asset Management ViewModels
    [ObservableProperty]
    private AssetBrowserViewModel _assetBrowserViewModel;

    [ObservableProperty]
    private PackManagerViewModel _packManagerViewModel;

    [ObservableProperty]
    private ContentBrowserViewModel _contentBrowserViewModel;

    [ObservableProperty]
    private ImportViewModel _importViewModel;

    [ObservableProperty]
    private bool _isImportPanelOpen;

    // Bottom panel states
    [ObservableProperty]
    private bool _isAssetBrowserOpen;

    [ObservableProperty]
    private bool _isPackManagerOpen;

    [ObservableProperty]
    private bool _isContentBrowserOpen;

    // Asset Picker state
    [ObservableProperty]
    private bool _isAssetPickerOpen;

    [ObservableProperty]
    private AssetRefType _assetPickerAssetType;

    [ObservableProperty]
    private string? _assetPickerCurrentPack;

    [ObservableProperty]
    private string? _assetPickerCurrentAssetName;

    private Action<AssetInfo?>? _assetPickerCallback;

    public void LoadFromCurrentScene()
    {
        RootObjects.Clear();
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        foreach (var obj in scene.RootObjects)
        {
            RootObjects.Add(new GameObjectViewModel(obj, this));
        }
    }

    [RelayCommand]
    private void NewObject()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        var obj = scene.CreateObject("New Object");
        var vm = new GameObjectViewModel(obj, this);
        RootObjects.Add(vm);
        SelectedGameObject = vm;
    }

    [RelayCommand]
    private void NewChildObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var child = SelectedGameObject.GameObject.CreateChild("New Child");
        var vm = new GameObjectViewModel(child, this);
        SelectedGameObject.Children.Add(vm);
    }

    [RelayCommand]
    private void DeleteObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        var parent = FindParent(SelectedGameObject);
        if (parent != null)
        {
            parent.RemoveChild(SelectedGameObject);
        }
        else
        {
            scene.Destroy(SelectedGameObject.GameObject);
            RootObjects.Remove(SelectedGameObject);
        }

        SelectedGameObject = null;
    }

    [RelayCommand]
    private void DuplicateObject()
    {
        if (SelectedGameObject == null)
        {
            return;
        }

        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        var original = SelectedGameObject.GameObject;
        var clone = _sceneService.CloneGameObject(original);

        var parent = FindParent(SelectedGameObject);
        if (parent != null)
        {
            parent.GameObject.AddChild(clone);
            var vm = new GameObjectViewModel(clone, this);
            parent.Children.Add(vm);
            SelectedGameObject = vm;
        }
        else
        {
            scene.Add(clone);
            var vm = new GameObjectViewModel(clone, this);
            RootObjects.Add(vm);
            SelectedGameObject = vm;
        }
    }

    [RelayCommand]
    private void SaveScene()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        _sceneService.SaveScene(scene);
    }

    [RelayCommand]
    private void LoadScene(string path)
    {
        World.LoadScene(path);
        LoadFromCurrentScene();
    }

    [RelayCommand]
    private void OpenImportPanel()
    {
        IsImportPanelOpen = true;
    }

    [RelayCommand]
    private void CloseImportPanel()
    {
        IsImportPanelOpen = false;
    }

    [RelayCommand]
    private void CloseAssetBrowser()
    {
        IsAssetBrowserOpen = false;
    }

    [RelayCommand]
    private void ClosePackManager()
    {
        IsPackManagerOpen = false;
    }

    [RelayCommand]
    private void CloseContentBrowser()
    {
        IsContentBrowserOpen = false;
    }

    public void OpenAssetPicker(AssetRefType assetType, string? currentPack, string? currentAssetName, Action<AssetInfo?> callback)
    {
        AssetPickerAssetType = assetType;
        AssetPickerCurrentPack = currentPack;
        AssetPickerCurrentAssetName = currentAssetName;
        _assetPickerCallback = callback;
        IsAssetPickerOpen = true;
    }

    [RelayCommand]
    private void CloseAssetPicker()
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

    [RelayCommand]
    private void RefreshAssets()
    {
        AssetBrowserViewModel.Refresh();
    }

    // Viewport and Statistics properties
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is3DMode))]
    [NotifyPropertyChangedFor(nameof(ProjectionModeText))]
    private bool _is2DMode;

    public bool Is3DMode => !Is2DMode;

    public string ProjectionModeText => Is2DMode ? "2D" : "3D";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ViewPresetText))]
    private ViewPreset _currentViewPreset = ViewPreset.Free;

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

    [ObservableProperty]
    private bool _showStatistics = true;

    [ObservableProperty]
    private float _fps;

    [ObservableProperty]
    private float _frameTime;

    [ObservableProperty]
    private string _gizmoModeText = "Move (W)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SnapStatusText))]
    private bool _snapEnabled;

    [ObservableProperty]
    private float _positionSnapIncrement = 1f;

    [ObservableProperty]
    private float _rotationSnapIncrement = 15f;

    [ObservableProperty]
    private float _scaleSnapIncrement = 0.1f;

    public string SnapStatusText => SnapEnabled ? "Snap: On" : "Snap: Off";

    private GridDesc? _gridSettings;

    public void SetGridSettings(GridDesc desc)
    {
        _gridSettings = desc;
        SnapEnabled = desc.SnapEnabled;
        PositionSnapIncrement = desc.PositionSnapIncrement;
        RotationSnapIncrement = desc.RotationSnapIncrement;
        ScaleSnapIncrement = desc.ScaleSnapIncrement;
    }

    partial void OnSnapEnabledChanged(bool value)
    {
        if (_gridSettings != null)
        {
            _gridSettings.SnapEnabled = value;
        }
    }

    partial void OnPositionSnapIncrementChanged(float value)
    {
        if (_gridSettings != null)
        {
            _gridSettings.PositionSnapIncrement = value;
        }
    }

    partial void OnRotationSnapIncrementChanged(float value)
    {
        if (_gridSettings != null)
        {
            _gridSettings.RotationSnapIncrement = value;
        }
    }

    partial void OnScaleSnapIncrementChanged(float value)
    {
        if (_gridSettings != null)
        {
            _gridSettings.ScaleSnapIncrement = value;
        }
    }

    [RelayCommand]
    private void ToggleSnap()
    {
        SnapEnabled = !SnapEnabled;
    }

    private float _statsUpdateTimer;
    private int _frameCountSinceLastUpdate;
    private const float StatsUpdateInterval = 0.5f;

    public event Action? ViewPresetChanged;
    public event Action? ProjectionModeChanged;

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

    public void UpdateGizmoModeText(GizmoMode mode)
    {
        GizmoModeText = mode switch
        {
            GizmoMode.Translate => "Move (W)",
            GizmoMode.Rotate => "Rotate (E)",
            GizmoMode.Scale => "Scale (R)",
            _ => "Move (W)"
        };
    }

    [RelayCommand]
    private void Toggle2DMode()
    {
        Is2DMode = !Is2DMode;
        if (Is2DMode && CurrentViewPreset == ViewPreset.Free)
        {
            CurrentViewPreset = ViewPreset.Top;
        }
        ProjectionModeChanged?.Invoke();
    }

    [RelayCommand]
    private void SetViewPreset(string preset)
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

    [RelayCommand]
    private void ToggleStatistics()
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
