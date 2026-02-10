using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class OpenSceneDialogBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var browser = vm.SceneBrowserViewModel;
        if (browser == null)
        {
            return;
        }

        using var overlay = EditorUi.DialogOverlay(ui, "OpenSceneOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "OpenSceneDialog", "Open Scene", 600, 500);

        using (ui.Panel("SceneBreadcrumb")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(4)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            if (browser.CanNavigateUp)
            {
                if (EditorUi.IconButton(ctx, "SceneNavUp", FontAwesome.ArrowUp))
                {
                    browser.NavigateUp();
                }
            }

            var parts = browser.BreadcrumbParts;
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (i > 0)
                {
                    ui.Text("/", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                }

                if (Ui.Button(ctx, "SceneBc_" + i, part.Name)
                    .Color(UiColor.Transparent, t.Hover, t.Active)
                    .TextColor(t.TextSecondary)
                    .FontSize(t.FontSizeCaption)
                    .Padding(4, 2)
                    .CornerRadius(t.RadiusSmall)
                    .Border(0, UiColor.Transparent)
                    .Show())
                {
                    browser.NavigateTo(part.Path);
                }
            }
        }

        using (ui.Panel("SceneFileList")
            .Vertical()
            .Grow()
            .ScrollVertical()
            .Gap(1)
            .Open())
        {
            for (var i = 0; i < browser.Entries.Count; i++)
            {
                var entry = browser.Entries[i];
                var isSelected = browser.SelectedEntry == entry;
                var bg = isSelected ? t.Selected : UiColor.Transparent;
                var icon = entry.IsDirectory ? FontAwesome.Folder : FontAwesome.Film;
                var iconColor = entry.IsDirectory ? t.Warning : t.Accent;

                var btn = Ui.Button(ctx, "SceneEntry_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(8, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using var scope = btn.Open();
                scope.Icon(icon, iconColor, t.IconSizeSmall);
                scope.Text(entry.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });

                if (btn.WasClicked())
                {
                    if (entry.IsDirectory)
                    {
                        browser.NavigateTo(entry.FullPath);
                    }
                    else
                    {
                        browser.HandleSelection(entry);
                    }
                }
            }
        }

        using (ui.Panel("SceneDialogFooter")
            .Horizontal()
            .Padding(12, 8)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Right)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelElevated)
            .Open())
        {
            if (EditorUi.GhostButton(ctx, "SceneCancel", "Cancel"))
            {
                vm.CloseOpenSceneDialog();
            }

            if (browser.SelectedEntry != null && !browser.SelectedEntry.IsDirectory)
            {
                if (EditorUi.AccentButton(ctx, "SceneOpen", "Open"))
                {
                    vm.OnSceneFileSelected(browser.SelectedEntry.FullPath);
                }
            }
        }
    }
}
