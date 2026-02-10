using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class ImportDialogBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var importVm = vm.ImportViewModel;

        using var overlay = EditorUi.DialogOverlay(ui, "ImportOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "ImportDialog", "Import Assets", 800, 600);

        using (ui.Panel("ImportBody")
            .Horizontal()
            .Grow()
            .Open())
        {
            // Left: File browser
            BuildFileBrowser(ui, ctx, importVm);

            // Divider
            using (ui.Panel("ImportDivider").Width(1).GrowHeight().Background(t.Border).Open()) { }

            // Right: Import queue + settings
            BuildImportQueue(ui, ctx, importVm);
        }

        // Footer with progress and actions
        BuildFooter(ui, ctx, vm, importVm);
    }

    private static void BuildFileBrowser(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;
        var browser = importVm.FileBrowser;

        using var panel = ui.Panel("ImportBrowser")
            .Vertical()
            .Width(UiSizing.Percent(50))
            .GrowHeight()
            .Open();

        // Browser header
        using (ui.Panel("ImportBrowserHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(10, 8)
            .Gap(4)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            if (browser.CanNavigateUp)
            {
                if (EditorUi.IconButton(ctx, "ImportNavUp", FontAwesome.ArrowUp))
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

                if (Ui.Button(ctx, "ImportBc_" + i, part.Name)
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

        // File list
        using (ui.Panel("ImportFileList")
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
                var icon = GetFileIcon(entry);
                var iconColor = entry.IsDirectory ? t.Warning : t.TextSecondary;

                var btn = Ui.Button(ctx, "ImportFile_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(8, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using var scope = btn.Open();
                scope.Icon(icon, iconColor, t.IconSizeSmall);
                scope.Text(entry.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });

                if (btn.WasClicked())
                {
                    if (entry.IsDirectory)
                    {
                        browser.HandleDoubleClick(entry);
                    }
                    else
                    {
                        browser.HandleSelection(entry);
                    }
                }
            }
        }

        // Add to queue button
        using (ui.Panel("ImportAddRow")
            .Horizontal()
            .Padding(8, 8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Center)
            .Open())
        {
            if (EditorUi.AccentButton(ctx, "AddToQueueBtn", "Add to Queue"))
            {
                importVm.AddToQueue();
            }
        }
    }

    private static void BuildImportQueue(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = ui.Panel("ImportQueue")
            .Vertical()
            .Width(UiSizing.Percent(50))
            .GrowHeight()
            .Open();

        // Queue header
        EditorUi.SectionHeader(ui, ctx, "QueueHeader", FontAwesome.ListCheck, "Import Queue");

        // Queue items
        using (ui.Panel("QueueList")
            .Vertical()
            .Grow()
            .ScrollVertical()
            .Gap(1)
            .Open())
        {
            for (var i = 0; i < importVm.ImportItems.Count; i++)
            {
                var item = importVm.ImportItems[i];
                var isSelected = importVm.SelectedImportItem == item;
                var bg = isSelected ? t.Selected : UiColor.Transparent;

                using (ui.Panel("QueueItem_" + i)
                    .Horizontal()
                    .Background(bg)
                    .Padding(10, 8)
                    .Gap(8)
                    .GrowWidth()
                    .FitHeight()
                    .AlignChildrenY(UiAlignY.Center)
                    .Open())
                {
                    var icon = item.IsModel ? FontAwesome.Cube : FontAwesome.Image;
                    ui.Icon(icon, t.Accent, t.IconSizeSmall);

                    using (ui.Panel("QueueItemInfo_" + i).Vertical().Gap(2).GrowWidth().Open())
                    {
                        ui.Text(item.FileName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });
                        ui.Text(item.StatusText, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                    }

                    // Select button (invisible click area)
                    var interaction = ui.Panel("QueueItemClick_" + i)
                        .Background(UiColor.Transparent)
                        .Grow()
                        .GetInteraction();

                    if (interaction.WasClicked)
                    {
                        importVm.SelectedImportItem = item;
                    }

                    // Remove button
                    if (EditorUi.IconButton(ctx, "RemoveQueue_" + i, FontAwesome.Xmark))
                    {
                        importVm.RemoveFromQueue(item);
                    }
                }
            }

            if (importVm.ImportItems.Count == 0)
            {
                using (ui.Panel("QueueEmpty")
                    .Grow()
                    .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                    .Open())
                {
                    ui.Text("No items in queue", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeBody });
                }
            }
        }

        // Selected item settings
        if (importVm.SelectedImportItem is { IsModel: true })
        {
            BuildModelSettings(ui, ctx, importVm.SelectedImportItem);
        }
    }

    private static void BuildModelSettings(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = Ui.CollapsibleSection(ctx, "ModelSettings", "Model Settings", true)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeCaption)
            .Padding(10)
            .Gap(4)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        using var grid = Ui.PropertyGrid(ctx, "ModelSettingsGrid")
            .LabelWidth(75)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        // Asset Name
        {
            using var row = grid.Row("Asset Name");
            var assetName = item.AssetName;
            if (Ui.TextField(ctx, "ImportAssetName", ref assetName)
                .BackgroundColor(t.SurfaceInset)
                .TextColor(t.TextPrimary)
                .BorderColor(t.Border, t.Accent)
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .Padding(6, 4)
                .GrowWidth()
                .Show(ref assetName))
            {
                item.AssetName = assetName;
            }
        }

        // Scale
        {
            using var row = grid.Row("Scale");
            var scale = item.Scale;
            if (Ui.DraggableValue(ctx, "ImportScale")
                .Sensitivity(0.01f)
                .Format("F2")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref scale))
            {
                item.Scale = scale;
            }
        }

        // Optimize
        {
            using var row = grid.Row("Optimize");
            var optimize = item.OptimizeMeshes;
            var newOptimize = Ui.Checkbox(ctx, "ImportOptimize", "", optimize)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .Show();
            if (newOptimize != optimize)
            {
                item.OptimizeMeshes = newOptimize;
            }
        }

        // Generate Normals
        {
            using var row = grid.Row("Normals");
            var genNormals = item.GenerateNormals;
            var newGenNormals = Ui.Checkbox(ctx, "ImportNormals", "", genNormals)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .Show();
            if (newGenNormals != genNormals)
            {
                item.GenerateNormals = newGenNormals;
            }
        }

        // Tangents
        {
            using var row = grid.Row("Tangents");
            var tangents = item.CalculateTangents;
            var newTangents = Ui.Checkbox(ctx, "ImportTangents", "", tangents)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .Show();
            if (newTangents != tangents)
            {
                item.CalculateTangents = newTangents;
            }
        }
    }

    private static void BuildFooter(UiFrame ui, UiContext ctx, EditorViewModel vm, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var footer = ui.Panel("ImportFooter")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(16, 12)
            .Gap(12)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open();

        // Progress text
        if (!string.IsNullOrEmpty(importVm.ProgressText))
        {
            ui.Text(importVm.ProgressText, new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
        }

        // Progress bar
        if (importVm.IsImporting)
        {
            using (ui.Panel("ImportProgress")
                .Background(t.SurfaceInset)
                .Width(200)
                .Height(6)
                .CornerRadius(3)
                .Open())
            {
                var fillWidth = (float)(importVm.Progress / 100.0 * 200);
                using (ui.Panel("ImportProgressFill")
                    .Background(t.Accent)
                    .Width(fillWidth)
                    .Height(6)
                    .CornerRadius(3)
                    .Open()) { }
            }
        }

        Ui.FlexSpacer(ctx);

        // Buttons
        if (EditorUi.GhostButton(ctx, "ImportCancel", importVm.IsImporting ? "Cancel" : "Close"))
        {
            if (importVm.IsImporting)
            {
                importVm.Cancel();
            }
            else
            {
                vm.CloseImportPanel();
            }
        }

        if (!importVm.IsImporting && importVm.ImportItems.Count > 0)
        {
            if (EditorUi.AccentButton(ctx, "ImportStart", "Import"))
            {
                importVm.Import();
            }
        }
    }

    private static string GetFileIcon(FileEntry entry)
    {
        if (entry.IsDirectory)
        {
            return FontAwesome.Folder;
        }

        if (ImportViewModel.IsModelFile(entry.FullPath))
        {
            return FontAwesome.Cube;
        }

        if (ImportViewModel.IsTextureFile(entry.FullPath))
        {
            return FontAwesome.Image;
        }

        return FontAwesome.File;
    }
}
