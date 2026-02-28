using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class OpenSceneDialogBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var browser = vm.SceneBrowserViewModel;
        if (browser == null)
        {
            return;
        }

        using var overlay = EditorUi.DialogOverlay("OpenSceneOverlay");
        using var dialog = EditorUi.DialogContainer("OpenSceneDialog", "Open Scene", 600, 500);

        using (NiziUi.Panel("SceneBreadcrumb")
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
                if (EditorUi.IconButton("SceneNavUp", FontAwesome.ArrowUp))
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
                    NiziUi.Text("/", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                }

                if (NiziUi.Button("SceneBc_" + i, part.Name)
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

        using (NiziUi.Panel("SceneFileList")
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

                var btn = NiziUi.Button("SceneEntry_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(8, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using var scope = btn.Open();
                NiziUi.Icon(icon, iconColor, t.IconSizeSmall);
                NiziUi.Text(entry.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });

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

        using (NiziUi.Panel("SceneDialogFooter")
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
            if (EditorUi.GhostButton("SceneCancel", "Cancel"))
            {
                vm.CloseOpenSceneDialog();
            }

            if (browser.SelectedEntry != null && !browser.SelectedEntry.IsDirectory)
            {
                if (EditorUi.AccentButton("SceneOpen", "Open"))
                {
                    vm.OnSceneFileSelected(browser.SelectedEntry.FullPath);
                }
            }
        }
    }
}
