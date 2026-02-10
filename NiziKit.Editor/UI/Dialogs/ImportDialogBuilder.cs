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
        using var dialog = EditorUi.DialogContainer(ui, ctx, "ImportDialog", "Import Assets", 900, 650);

        using (ui.Panel("ImportBody")
            .Horizontal()
            .Grow()
            .Open())
        {
            // Left side: File browser (top) + Import queue (bottom)
            BuildLeftPanel(ui, ctx, importVm);

            // Divider
            using (ui.Panel("ImportDivider").Width(1).GrowHeight().Background(t.Border).Open()) { }

            // Right side: Asset details for selected item
            BuildDetailsPanel(ui, ctx, importVm);
        }

        // Footer with output directory, progress, and actions
        BuildFooter(ui, ctx, vm, importVm);
    }

    private static void BuildLeftPanel(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = ui.Panel("ImportLeft")
            .Vertical()
            .Width(UiSizing.Percent(0.45f))
            .GrowHeight()
            .Open();

        // File browser section (top, takes ~60% height)
        BuildFileBrowser(ui, ctx, importVm);

        // Add to queue button
        using (ui.Panel("ImportAddRow")
            .Horizontal()
            .Padding(8, 8)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .Open())
        {
            if (EditorUi.AccentButton(ctx, "AddToQueueBtn", "Add to Queue"))
            {
                importVm.AddToQueue();
            }
        }

        // Import queue section (bottom)
        BuildImportQueue(ui, ctx, importVm);
    }

    private static void BuildFileBrowser(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;
        var browser = importVm.FileBrowser;

        using var panel = ui.Panel("ImportBrowser")
            .Vertical()
            .GrowWidth()
            .Height(UiSizing.Percent(0.55f))
            .Background(t.PanelElevated)
            .Border(1, t.Border)
            .Open();

        // Browser header with navigation
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
    }

    private static void BuildImportQueue(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = ui.Panel("ImportQueueSection")
            .Vertical()
            .GrowWidth()
            .Grow()
            .Background(t.PanelElevated)
            .Border(1, t.Border)
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

                var rowBtn = Ui.Button(ctx, "QueueItem_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(10, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using (rowBtn.Open())
                {
                    var icon = item.IsModel ? FontAwesome.Cube : FontAwesome.Image;
                    using (ui.Panel("QueueItemIcon_" + i).FitWidth().FitHeight().AlignChildrenY(UiAlignY.Center).Open())
                    {
                        ui.Icon(icon, t.Accent, t.IconSizeSmall);
                    }

                    using (ui.Panel("QueueItemInfo_" + i)
                        .Vertical()
                        .Gap(2)
                        .GrowWidth()
                        .Open())
                    {
                        ui.Text(item.FileName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });
                        ui.Text(item.StatusText, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                    }

                    if (EditorUi.IconButton(ctx, "RemoveQueue_" + i, FontAwesome.Xmark))
                    {
                        importVm.RemoveFromQueue(item);
                    }
                }

                if (rowBtn.WasClicked())
                {
                    importVm.SelectedImportItem = item;
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
    }

    private static void BuildDetailsPanel(UiFrame ui, UiContext ctx, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = ui.Panel("ImportDetails")
            .Vertical()
            .Width(UiSizing.Percent(0.55f))
            .GrowHeight()
            .ScrollVertical()
            .Padding(16, 12)
            .Gap(12)
            .Open();

        var item = importVm.SelectedImportItem;
        if (item == null)
        {
            using (ui.Panel("NoSelection")
                .Grow()
                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                .Open())
            {
                ui.Text("Select an item to view details", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeBody });
            }
            return;
        }

        ui.Text("Asset Details", new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeSubtitle });
        {
            var grid = Ui.PropertyGrid(ctx, "DetailGrid")
                .LabelWidth(140)
                .FontSize(t.FontSizeCaption)
                .RowHeight(28)
                .Gap(4)
                .LabelColor(t.TextSecondary)
                .Open();

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

            {
                using var row = grid.Row("Output Subdir");
                var outSubdir = item.OutputSubdirectory;
                if (Ui.TextField(ctx, "ImportOutSubdir", ref outSubdir)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .GrowWidth()
                    .Placeholder("(root output)")
                    .Show(ref outSubdir))
                {
                    item.OutputSubdirectory = outSubdir;
                }
            }

            grid.Dispose();
        }

        if (item.IsTexture)
        {
            BuildTextureOptions(ui, ctx, item);
        }
        if (item.IsModel)
        {
            BuildModelDetails(ui, ctx, item);
        }
    }

    private static void BuildTextureOptions(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using (ui.Panel("TextureOpts")
            .Vertical()
            .GrowWidth()
            .FitHeight()
            .Gap(8)
            .Open())
        {
            var genMips = item.GenerateMips;
            var newGenMips = Ui.Checkbox(ctx, "ImportGenMips", "Generate Mipmaps", genMips)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .LabelColor(t.TextPrimary)
                .FontSize(t.FontSizeCaption)
                .Show();
            if (newGenMips != genMips)
            {
                item.GenerateMips = newGenMips;
            }
        }
    }

    private static void BuildModelDetails(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;
        if (item.IsScanning)
        {
            using (ui.Panel("ScanningRow")
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                ui.Icon(FontAwesome.Sync, t.TextSecondary, t.IconSizeSmall);
                ui.Text("Scanning...", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            }
            return;
        }
        if (item.ScanError != null)
        {
            ui.Text(item.ScanError, new UiTextStyle { Color = t.Error, FontSize = t.FontSizeCaption });
            return;
        }

        if (!item.ScanComplete)
        {
            return;
        }

        BuildMeshesSection(ui, ctx, item);
        if (item.HasSkeleton)
        {
            BuildSkeletonSection(ui, ctx, item);
        }
        BuildAnimationsSection(ui, ctx, item);
        BuildOptionsSection(ui, ctx, item);
    }

    private static void BuildMeshesSection(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = Ui.CollapsibleSection(ctx, "MeshesSection", "Meshes", true)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeCaption)
            .Padding(8)
            .Gap(4)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        for (var i = 0; i < item.Meshes.Count; i++)
        {
            var mesh = item.Meshes[i];

            using (ui.Panel("MeshRow_" + i)
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Padding(2, 4)
                .Open())
            {
                var enabled = mesh.IsEnabled;
                var newEnabled = Ui.Checkbox(ctx, "MeshEnable_" + i, "", enabled)
                    .BoxColor(t.SurfaceInset, t.Hover)
                    .CheckColor(t.Accent)
                    .BorderColor(t.Border)
                    .BoxSize(14)
                    .CornerRadius(t.RadiusSmall)
                    .Show();
                if (newEnabled != enabled)
                {
                    mesh.IsEnabled = newEnabled;
                }

                var exportName = mesh.ExportName;
                if (Ui.TextField(ctx, "MeshName_" + i, ref exportName)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .Width(UiSizing.Grow(120))
                    .Show(ref exportName))
                {
                    mesh.ExportName = exportName;
                }

                ui.Text(mesh.DisplayInfo, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }

        if (item.Meshes.Count == 0)
        {
            ui.Text("No meshes found", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildSkeletonSection(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using (ui.Panel("SkeletonRow")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(12)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .CornerRadius(t.RadiusSmall)
            .Open())
        {
            var exportSkel = item.ExportSkeleton;
            var newExportSkel = Ui.Checkbox(ctx, "ImportExportSkel", "Export Skeleton", exportSkel)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .LabelColor(t.TextPrimary)
                .FontSize(t.FontSizeCaption)
                .Show();
            if (newExportSkel != exportSkel)
            {
                item.ExportSkeleton = newExportSkel;
            }

            using (ui.Panel("SkelSpacer").GrowWidth().Open()) { }

            var info = $"{item.JointCount} joints, root: {item.RootJointName}";
            ui.Text(info, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildAnimationsSection(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = Ui.CollapsibleSection(ctx, "AnimsSection", "Animations", true)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeCaption)
            .Padding(8)
            .Gap(4)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        var exportAnims = item.ExportAnimations;
        var newExportAnims = Ui.Checkbox(ctx, "ImportExportAnims", "Export Animations", exportAnims)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .LabelColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .Show();
        if (newExportAnims != exportAnims)
        {
            item.ExportAnimations = newExportAnims;
        }

        if (!item.ExportAnimations)
        {
            return;
        }

        for (var i = 0; i < item.Animations.Count; i++)
        {
            var anim = item.Animations[i];

            using (ui.Panel("AnimRow_" + i)
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Padding(2, 4)
                .Open())
            {
                var enabled = anim.IsEnabled;
                var newEnabled = Ui.Checkbox(ctx, "AnimEnable_" + i, "", enabled)
                    .BoxColor(t.SurfaceInset, t.Hover)
                    .CheckColor(t.Accent)
                    .BorderColor(t.Border)
                    .BoxSize(14)
                    .CornerRadius(t.RadiusSmall)
                    .Show();
                if (newEnabled != enabled)
                {
                    anim.IsEnabled = newEnabled;
                }

                var exportName = anim.ExportName;
                if (Ui.TextField(ctx, "AnimName_" + i, ref exportName)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .Width(UiSizing.Grow(120))
                    .Show(ref exportName))
                {
                    anim.ExportName = exportName;
                }

                ui.Text(anim.DisplayInfo, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }

        if (item.Animations.Count == 0)
        {
            ui.Text("No animations found", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildOptionsSection(UiFrame ui, UiContext ctx, ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = Ui.CollapsibleSection(ctx, "OptionsSection", "Options", false)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeCaption)
            .Padding(8)
            .Gap(6)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        {
            var grid = Ui.PropertyGrid(ctx, "OptsGrid")
                .LabelWidth(140)
                .FontSize(t.FontSizeCaption)
                .RowHeight(26)
                .Gap(4)
                .LabelColor(t.TextSecondary)
                .Open();

            {
                using var row = grid.Row("Scale");
                var scale = item.Scale;
                if (Ui.DraggableValue(ctx, "ImportScale")
                    .Sensitivity(0.01f)
                    .Format("F2")
                    .FontSize(t.FontSizeCaption)
                    .Width(UiSizing.Fixed(90))
                    .ValueColor(t.InputBackground)
                    .ValueTextColor(t.InputText)
                    .Show(ref scale))
                {
                    item.Scale = scale;
                }
            }

            grid.Dispose();
        }

        using (ui.Panel("OptsChecks1")
            .Horizontal()
            .Gap(16)
            .GrowWidth()
            .FitHeight()
            .Padding(0, 4)
            .Open())
        {
            item.OptimizeMeshes = CheckboxOption(ctx, "OptOptimize", "Optimize Meshes", item.OptimizeMeshes);
            item.GenerateNormals = CheckboxOption(ctx, "OptNormals", "Generate Normals", item.GenerateNormals);
            item.CalculateTangents = CheckboxOption(ctx, "OptTangents", "Tangents", item.CalculateTangents);
        }

        using (ui.Panel("OptsChecks2")
            .Horizontal()
            .Gap(16)
            .GrowWidth()
            .FitHeight()
            .Padding(0, 4)
            .Open())
        {
            item.TriangulateMeshes = CheckboxOption(ctx, "OptTriangulate", "Triangulate", item.TriangulateMeshes);
            item.JoinIdenticalVertices = CheckboxOption(ctx, "OptJoinVerts", "Join Vertices", item.JoinIdenticalVertices);
        }

        using (ui.Panel("OptsSmoothRow")
            .Horizontal()
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Padding(0, 4)
            .Open())
        {
            item.SmoothNormals = CheckboxOption(ctx, "OptSmooth", "Smooth Normals", item.SmoothNormals);
            ui.Text("Angle:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            var angle = item.SmoothNormalsAngle;
            if (Ui.DraggableValue(ctx, "OptSmoothAngle")
                .Sensitivity(0.5f)
                .Format("F0")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Fixed(60))
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref angle))
            {
                item.SmoothNormalsAngle = angle;
            }
        }

        using (ui.Panel("OptsBoneRow")
            .Horizontal()
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Padding(0, 4)
            .Open())
        {
            item.LimitBoneWeights = CheckboxOption(ctx, "OptBoneWeights", "Limit Bone Weights", item.LimitBoneWeights);
            ui.Text("Max:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            var maxWeights = (float)item.MaxBoneWeightsPerVertex;
            if (Ui.DraggableValue(ctx, "OptMaxBoneW")
                .Sensitivity(1f)
                .Format("F0")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Fixed(60))
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref maxWeights))
            {
                item.MaxBoneWeightsPerVertex = (uint)Math.Max(1, maxWeights);
            }
        }
    }

    private static bool CheckboxOption(UiContext ctx, string id, string label, bool value)
    {
        var t = EditorTheme.Current;
        return Ui.Checkbox(ctx, id, label, value)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .LabelColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .Show();
    }

    private static void BuildFooter(UiFrame ui, UiContext ctx, EditorViewModel vm, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var footer = ui.Panel("ImportFooter")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(10)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open();

        ui.Text("Output:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
        var outDir = importVm.OutputDirectory;
        if (Ui.TextField(ctx, "ImportOutDir", ref outDir)
            .BackgroundColor(t.SurfaceInset)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .Width(UiSizing.Fixed(200))
            .Placeholder("Assets/...")
            .Show(ref outDir))
        {
            importVm.OutputDirectory = outDir;
        }

        if (importVm.IsImporting)
        {
            if (!string.IsNullOrEmpty(importVm.ProgressText))
            {
                ui.Text(importVm.ProgressText, new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            }

            using (ui.Panel("ImportProgress")
                .Background(t.SurfaceInset)
                .Width(120)
                .Height(6)
                .CornerRadius(3)
                .Open())
            {
                var fillWidth = (float)(importVm.Progress / 100.0 * 120);
                using (ui.Panel("ImportProgressFill")
                    .Background(t.Accent)
                    .Width(fillWidth)
                    .Height(6)
                    .CornerRadius(3)
                    .Open()) { }
            }
        }
        else if (importVm.ImportSucceeded && !string.IsNullOrEmpty(importVm.ProgressText))
        {
            ui.Text(importVm.ProgressText, new UiTextStyle { Color = t.Success, FontSize = t.FontSizeCaption });
        }

        Ui.FlexSpacer(ctx);
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
            if (EditorUi.AccentButton(ctx, "ImportStart", "Import All"))
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
