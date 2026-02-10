using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class SceneHierarchyBuilder
{
    private static string _selectedNodeId = "";

    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        var roots = new List<UiTreeNode>();
        foreach (var obj in vm.RootObjects)
        {
            roots.Add(BuildNode(obj));
        }

        if (vm.SelectedGameObject != null)
        {
            _selectedNodeId = vm.SelectedGameObject.Name;
        }
        else
        {
            _selectedNodeId = "";
        }

        var treeView = Ui.TreeView(ctx, "SceneTree", roots)
            .Background(UiColor.Transparent)
            .SelectedColor(t.Selected)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeCaption)
            .IndentSize(14)
            .ItemHeight(22)
            .Width(UiSizing.Grow())
            .Height(UiSizing.Grow());

        var changed = treeView.Show(ref _selectedNodeId);

        if (changed)
        {
            var selected = FindByName(vm.RootObjects, _selectedNodeId);
            vm.SelectObject(selected);
        }

        // Detect right-click on tree view to open context menu
        if (ctx.WasRightClicked(treeView.Id))
        {
            var menuState = Ui.GetContextMenuState(ctx, "HierarchyCtx_menu");
            menuState.OpenAt(ctx.MouseX, ctx.MouseY);
        }
    }

    private static UiTreeNode BuildNode(GameObjectViewModel obj)
    {
        var node = new UiTreeNode
        {
            Id = obj.Name,
            Label = obj.Name,
            Icon = obj.TypeIcon,
            Tag = obj,
            Children = []
        };

        foreach (var child in obj.Children)
        {
            node.Children.Add(BuildNode(child));
        }

        return node;
    }

    public static void BuildContextMenu(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        var menuState = Ui.GetContextMenuState(ctx, "HierarchyCtx_menu");

        var items = new[]
        {
            UiContextMenuItem.Item("New Object", FontAwesome.Plus),
            UiContextMenuItem.Item("New Child", FontAwesome.Plus),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("New Directional Light", FontAwesome.Lightbulb),
            UiContextMenuItem.Item("New Point Light", FontAwesome.Lightbulb),
            UiContextMenuItem.Item("New Spot Light", FontAwesome.Lightbulb),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("Duplicate", FontAwesome.Copy),
            UiContextMenuItem.Item("Delete", FontAwesome.Trash),
        };

        var result = Ui.ContextMenu(ctx, "HierarchyCtx_menu", items)
            .Background(t.PanelBackground)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .FontSize(t.FontSizeBody)
            .Show();

        switch (result)
        {
            case 0: vm.NewObject(); break;
            case 1: vm.NewChildObject(); break;
            case 3: vm.NewDirectionalLight(); break;
            case 4: vm.NewPointLight(); break;
            case 5: vm.NewSpotLight(); break;
            case 7: vm.DuplicateObject(); break;
            case 8: vm.DeleteObject(); break;
        }
    }

    private static GameObjectViewModel? FindByName(List<GameObjectViewModel> objects, string name)
    {
        foreach (var obj in objects)
        {
            if (obj.Name == name)
            {
                return obj;
            }

            var found = FindByName(obj.Children, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
