using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class MenuBarBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var bar = ui.Panel("MenuBar")
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

        BuildFileMenu(ui, ctx, vm);
        BuildEditMenu(ui, ctx, vm);
        BuildAssetsMenu(ui, ctx, vm);
        Ui.FlexSpacer(ctx);

        if (vm.IsDirty)
        {
            ui.Text("Modified", new UiTextStyle { Color = t.Warning, FontSize = t.FontSizeCaption });
        }

        using (ui.Panel("EditorBadge")
            .Background(t.Accent)
            .Padding(10, 4)
            .FitWidth()
            .FitHeight()
            .Open())
        {
            ui.Text("NiziKit Editor", new UiTextStyle { Color = UiColor.White, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildFileMenu(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton(ctx, "FileMenu", "File", out var fileClicked, out var fileMenuState);
        if (fileClicked)
        {
            fileMenuState.OpenBelow(ctx.GetElementId("FileMenu"));
        }

        var fileItems = new[]
        {
            UiContextMenuItem.Item("Open Scene...", FontAwesome.FolderOpen),
            UiContextMenuItem.Item("Save Scene", FontAwesome.Save),
        };

        var fileResult = Ui.ContextMenu(ctx, "FileMenu_menu", fileItems)
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

    private static void BuildEditMenu(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton(ctx, "EditMenu", "Edit", out var editClicked, out var editMenuState);
        if (editClicked)
        {
            editMenuState.OpenBelow(ctx.GetElementId("EditMenu"));
        }

        var editItems = new[]
        {
            UiContextMenuItem.Item("New Object", FontAwesome.Plus),
            UiContextMenuItem.Item("New Child", FontAwesome.Plus),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("Duplicate", FontAwesome.Copy),
            UiContextMenuItem.Item("Delete", FontAwesome.Trash),
        };

        var editResult = Ui.ContextMenu(ctx, "EditMenu_menu", editItems)
            .Background(t.PanelBackground)
            .HoverColor(t.Hover)
            .TextColor(t.TextPrimary)
            .IconColor(t.TextSecondary)
            .FontSize(t.FontSizeBody)
            .Show();

        if (editResult == 0)
        {
            vm.NewObject();
        }
        else if (editResult == 1)
        {
            vm.NewChildObject();
        }
        else if (editResult == 3)
        {
            vm.DuplicateObject();
        }
        else if (editResult == 4)
        {
            vm.DeleteObject();
        }
    }

    private static void BuildAssetsMenu(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        EditorUi.MenuButton(ctx, "AssetsMenu", "Assets", out var assetsClicked, out var assetsMenuState);
        if (assetsClicked)
        {
            assetsMenuState.OpenBelow(ctx.GetElementId("AssetsMenu"));
        }

        var assetsItems = new[]
        {
            UiContextMenuItem.Item("Import...", FontAwesome.Upload),
            UiContextMenuItem.Separator(),
            UiContextMenuItem.Item("Refresh", FontAwesome.Refresh),
        };

        var assetsResult = Ui.ContextMenu(ctx, "AssetsMenu_menu", assetsItems)
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
