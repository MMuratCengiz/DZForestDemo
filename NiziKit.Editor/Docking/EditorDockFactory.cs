using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Docking;

public class EditorDockFactory : Factory
{
    private readonly EditorViewModel _editorViewModel;

    public EditorDockFactory(EditorViewModel editorViewModel)
    {
        _editorViewModel = editorViewModel;
    }

    public override IRootDock CreateLayout()
    {
        var sceneTreeTool = new SceneTreeToolViewModel
        {
            Id = "SceneTree",
            Title = "Scene",
            EditorViewModel = _editorViewModel,
            CanClose = false,
            CanPin = true,
            CanFloat = true
        };

        var inspectorTool = new InspectorToolViewModel
        {
            Id = "Inspector",
            Title = "Inspector",
            EditorViewModel = _editorViewModel,
            CanClose = false,
            CanPin = true,
            CanFloat = true
        };

        var assetBrowserTool = new AssetBrowserToolViewModel
        {
            Id = "AssetBrowser",
            Title = "Asset Browser",
            EditorViewModel = _editorViewModel,
            CanClose = false,
            CanPin = true,
            CanFloat = true
        };

        var packManagerTool = new PackManagerToolViewModel
        {
            Id = "PackManager",
            Title = "Pack Manager",
            EditorViewModel = _editorViewModel,
            CanClose = false,
            CanPin = true,
            CanFloat = true
        };

        var leftDock = new ToolDock
        {
            Id = "LeftDock",
            Title = "LeftDock",
            Proportion = 0.18,
            VisibleDockables = CreateList<IDockable>(sceneTreeTool),
            ActiveDockable = sceneTreeTool,
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible
        };

        var rightDock = new ToolDock
        {
            Id = "RightDock",
            Title = "RightDock",
            Proportion = 0.22,
            VisibleDockables = CreateList<IDockable>(inspectorTool),
            ActiveDockable = inspectorTool,
            Alignment = Alignment.Right,
            GripMode = GripMode.Visible
        };

        var bottomDock = new ToolDock
        {
            Id = "BottomDock",
            Title = "BottomDock",
            Proportion = 0.30,
            VisibleDockables = CreateList<IDockable>(assetBrowserTool, packManagerTool),
            ActiveDockable = assetBrowserTool,
            Alignment = Alignment.Bottom,
            GripMode = GripMode.Visible
        };

        var centerDock = new ProportionalDock
        {
            Id = "CenterDock",
            Title = "CenterDock",
            Proportion = 0.60,
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(),
            IsCollapsable = false
        };

        var topRow = new ProportionalDock
        {
            Id = "TopRow",
            Title = "TopRow",
            Proportion = 0.70,
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                leftDock,
                new ProportionalDockSplitter(),
                centerDock,
                new ProportionalDockSplitter(),
                rightDock
            )
        };

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Title = "MainLayout",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(
                topRow,
                new ProportionalDockSplitter(),
                bottomDock
            )
        };

        var rootDock = CreateRootDock();
        rootDock.Id = "Root";
        rootDock.Title = "Root";
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

        return rootDock;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["SceneTree"] = () => _editorViewModel,
            ["Inspector"] = () => _editorViewModel,
            ["AssetBrowser"] = () => _editorViewModel.AssetBrowserViewModel,
            ["PackManager"] = () => _editorViewModel.PackManagerViewModel
        };

        DockableLocator = new Dictionary<string, Func<IDockable?>>
        {
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}
