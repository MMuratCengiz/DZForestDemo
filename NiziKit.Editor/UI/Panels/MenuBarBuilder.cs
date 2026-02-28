using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class MenuBarBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var bar = NiziUi.Panel("MenuBar")
            .Horizontal()
            .Background(t.PanelBackground)
            .Border(UiBorder.Vertical(0, UiColor.Transparent)
                with { Bottom = 1, Color = t.Border })
            .Padding(6, 2)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(2)
            .Open();

        BuildFileMenu(vm);
        BuildEditMenu(vm);
        BuildAssetsMenu(vm);
        NiziUi.FlexSpacer();

        if (vm.IsDirty)
        {
            NiziUi.Text("Modified", new UiTextStyle { Color = t.Warning, FontSize = t.FontSizeCaption });
        }

        using (NiziUi.Panel("EditorBadge")
            .Background(t.Accent)
            .Padding(10, 4)
            .FitWidth()
            .FitHeight()
            .Open())
        {
            NiziUi.Text("NiziKit Editor", new UiTextStyle { Color = UiColor.White, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildFileMenu(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton("FileMenu", "File", out var fileClicked, out var fileMenuState);
        if (fileClicked)
        {
            fileMenuState.OpenBelow(NiziUi.GetElementId("FileMenu"));
        }

        var fileItems = new[]
        {
            UiContextMenuItem.Item("Open Scene...", FontAwesome.FolderOpen),
            UiContextMenuItem.Item("Save Scene", FontAwesome.Save, "Ctrl+S"),
        };

        var fileResult = NiziUi.ContextMenu("FileMenu_menu", fileItems)
            .Background(t.PanelBackground)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .FontSize(t.FontSizeBody)
            .Show();

        if (fileResult == 0)
        {
            vm.OpenScene();
        }
        else if (fileResult == 1)
        {
            vm.SaveScene();
        }
    }

    private static void BuildEditMenu(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton("EditMenu", "Edit", out var editClicked, out var editMenuState);
        if (editClicked)
        {
            editMenuState.OpenBelow(NiziUi.GetElementId("EditMenu"));
        }

        var undoItem = UiContextMenuItem.Item("Undo", FontAwesome.Undo, "Ctrl+Z");
        undoItem.IsDisabled = !vm.UndoSystem.CanUndo;
        var redoItem = UiContextMenuItem.Item("Redo", FontAwesome.Redo, "Ctrl+Shift+Z");
        redoItem.IsDisabled = !vm.UndoSystem.CanRedo;

        var editItems = new[]
        {
            undoItem,
            redoItem,
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("New Object", FontAwesome.Plus),
            UiContextMenuItem.Item("New Child", FontAwesome.Plus),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("Duplicate", FontAwesome.Copy, "Ctrl+D"),
            UiContextMenuItem.Item("Delete", FontAwesome.Trash, "Del"),
        };

        var editResult = NiziUi.ContextMenu("EditMenu_menu", editItems)
            .Background(t.PanelBackground)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .FontSize(t.FontSizeBody)
            .Show();

        if (editResult == 0)
        {
            vm.Undo();
        }
        else if (editResult == 1)
        {
            vm.Redo();
        }
        else if (editResult == 3)
        {
            vm.NewObject();
        }
        else if (editResult == 4)
        {
            vm.NewChildObject();
        }
        else if (editResult == 6)
        {
            vm.DuplicateObject();
        }
        else if (editResult == 7)
        {
            vm.DeleteObject();
        }
    }

    private static void BuildAssetsMenu(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton("AssetsMenu", "Assets", out var assetsClicked, out var assetsMenuState);
        if (assetsClicked)
        {
            assetsMenuState.OpenBelow(NiziUi.GetElementId("AssetsMenu"));
        }

        var assetsItems = new[]
        {
            UiContextMenuItem.Item("Import...", FontAwesome.Upload),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("Refresh", FontAwesome.Refresh),
        };

        var assetsResult = NiziUi.ContextMenu("AssetsMenu_menu", assetsItems)
            .Background(t.PanelBackground)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .FontSize(t.FontSizeBody)
            .Show();

        if (assetsResult == 0)
        {
            vm.OpenImportPanel();
        }
        else if (assetsResult == 2)
        {
            vm.RefreshAssets();
        }
    }
}
