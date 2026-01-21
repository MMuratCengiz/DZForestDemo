using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Controls;
using Dock.Model.Core;
using NiziKit.Core;
using NiziKit.Editor.Docking;
using NiziKit.Editor.Services;

namespace NiziKit.Editor.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly EditorSceneService _sceneService;
    private readonly AssetBrowserService _assetBrowserService;
    private readonly AssetFileService _assetFileService;

    public EditorViewModel()
    {
        _sceneService = new EditorSceneService();
        _assetBrowserService = new AssetBrowserService();
        _assetFileService = new AssetFileService();

        AssetBrowserViewModel = new AssetBrowserViewModel();
        PackManagerViewModel = new PackManagerViewModel();
        ImportViewModel = new ImportViewModel();

        ImportViewModel.ImportCompleted += () =>
        {
            AssetBrowserViewModel.Refresh();
            IsImportPanelOpen = false;
        };
        ImportViewModel.ImportCancelled += () => IsImportPanelOpen = false;

        var factory = new EditorDockFactory(this);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        DockLayout = layout;
        DockFactory = factory;
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
    private ImportViewModel _importViewModel;

    [ObservableProperty]
    private bool _isImportPanelOpen;

    [ObservableProperty]
    private IRootDock? _dockLayout;

    public IFactory? DockFactory { get; private set; }

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
    private async Task SaveScene()
    {
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        await _sceneService.SaveSceneAsync(scene, $"{scene.Name}.niziscene.json");
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
    private void RefreshAssets()
    {
        AssetBrowserViewModel.Refresh();
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
