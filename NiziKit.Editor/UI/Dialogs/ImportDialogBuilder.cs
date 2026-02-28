using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public static class ImportDialogBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var importVm = vm.ImportViewModel;

        using var overlay = EditorUi.DialogOverlay("ImportOverlay");
        using var dialog = EditorUi.DialogContainer("ImportDialog", "Import Assets", 900, 650);

        using (NiziUi.Panel("ImportBody")
            .Horizontal()
            .Grow()
            .Open())
        {
            // Left side: File browser (top) + Import queue (bottom)
            BuildLeftPanel(importVm);

            // Divider
            using (NiziUi.Panel("ImportDivider").Width(1).GrowHeight().Background(t.Border).Open()) { }

            // Right side: Asset details for selected item
            BuildDetailsPanel(importVm);
        }

        // Footer with output directory, progress, and actions
        BuildFooter(vm, importVm);
    }

    private static void BuildLeftPanel(ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = NiziUi.Panel("ImportLeft")
            .Vertical()
            .Width(UiSizing.Percent(0.45f))
            .GrowHeight()
            .Open();

        // File browser section (top, takes ~60% height)
        BuildFileBrowser(importVm);

        // Add to queue button
        using (NiziUi.Panel("ImportAddRow")
            .Horizontal()
            .Padding(8, 8)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .Open())
        {
            if (EditorUi.AccentButton("AddToQueueBtn", "Add to Queue"))
            {
                importVm.AddToQueue();
            }
        }

        // Import queue section (bottom)
        BuildImportQueue(importVm);
    }

    private static void BuildFileBrowser(ImportViewModel importVm)
    {
        var t = EditorTheme.Current;
        var browser = importVm.FileBrowser;

        using var panel = NiziUi.Panel("ImportBrowser")
            .Vertical()
            .GrowWidth()
            .Height(UiSizing.Percent(0.55f))
            .Background(t.PanelElevated)
            .Border(1, t.Border)
            .Open();

        // Browser header with navigation
        using (NiziUi.Panel("ImportBrowserHeader")
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
                if (EditorUi.IconButton("ImportNavUp", FontAwesome.ArrowUp))
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

                if (NiziUi.Button("ImportBc_" + i, part.Name)
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
        using (NiziUi.Panel("ImportFileList")
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

                var btn = NiziUi.Button("ImportFile_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(8, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using var scope = btn.Open();
                NiziUi.Icon(icon, iconColor, t.IconSizeSmall);
                NiziUi.Text(entry.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });

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

    private static void BuildImportQueue(ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = NiziUi.Panel("ImportQueueSection")
            .Vertical()
            .GrowWidth()
            .Grow()
            .Background(t.PanelElevated)
            .Border(1, t.Border)
            .Open();

        // Queue header
        EditorUi.SectionHeader("QueueHeader", FontAwesome.ListCheck, "Import Queue");

        // Queue items
        using (NiziUi.Panel("QueueList")
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

                var rowBtn = NiziUi.Button("QueueItem_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(10, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(8);

                using (rowBtn.Open())
                {
                    var icon = item.IsModel ? FontAwesome.Cube : FontAwesome.Image;
                    using (NiziUi.Panel("QueueItemIcon_" + i).FitWidth().FitHeight().AlignChildrenY(UiAlignY.Center).Open())
                    {
                        NiziUi.Icon(icon, t.Accent, t.IconSizeSmall);
                    }

                    using (NiziUi.Panel("QueueItemInfo_" + i)
                        .Vertical()
                        .Gap(2)
                        .GrowWidth()
                        .Open())
                    {
                        NiziUi.Text(item.FileName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });
                        NiziUi.Text(item.StatusText, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                    }

                    if (EditorUi.IconButton("RemoveQueue_" + i, FontAwesome.Xmark))
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
                using (NiziUi.Panel("QueueEmpty")
                    .Grow()
                    .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                    .Open())
                {
                    NiziUi.Text("No items in queue", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeBody });
                }
            }
        }
    }

    private static void BuildDetailsPanel(ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var panel = NiziUi.Panel("ImportDetails")
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
            using (NiziUi.Panel("NoSelection")
                .Grow()
                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                .Open())
            {
                NiziUi.Text("Select an item to view details", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeBody });
            }
            return;
        }

        NiziUi.Text("Asset Details", new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeSubtitle });
        {
            var grid = NiziUi.PropertyGrid("DetailGrid")
                .LabelWidth(140)
                .FontSize(t.FontSizeCaption)
                .RowHeight(28)
                .Gap(4)
                .LabelColor(t.TextSecondary)
                .Open();

            {
                using var row = grid.Row("Asset Name");
                var assetName = item.AssetName;
                if (NiziUi.TextField("ImportAssetName", ref assetName)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .GrowWidth()
                    .Show())
                {
                    item.AssetName = assetName;
                }
            }

            {
                using var row = grid.Row("Output Subdir");
                var outSubdir = item.OutputSubdirectory;
                if (NiziUi.TextField("ImportOutSubdir", ref outSubdir)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .GrowWidth()
                    .Placeholder("(root output)")
                    .Show())
                {
                    item.OutputSubdirectory = outSubdir;
                }
            }

            grid.Dispose();
        }

        if (item.IsTexture)
        {
            BuildTextureOptions(item);
        }
        if (item.IsModel)
        {
            BuildModelDetails(item);
        }
    }

    private static void BuildTextureOptions(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using (NiziUi.Panel("TextureOpts")
            .Vertical()
            .GrowWidth()
            .FitHeight()
            .Gap(8)
            .Open())
        {
            var genMips = item.GenerateMips;
            var newGenMips = NiziUi.Checkbox("ImportGenMips", "Generate Mipmaps", genMips)
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

    private static void BuildModelDetails(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;
        if (item.IsScanning)
        {
            using (NiziUi.Panel("ScanningRow")
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                NiziUi.Icon(FontAwesome.Sync, t.TextSecondary, t.IconSizeSmall);
                NiziUi.Text("Scanning...", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            }
            return;
        }
        if (item.ScanError != null)
        {
            NiziUi.Text(item.ScanError, new UiTextStyle { Color = t.Error, FontSize = t.FontSizeCaption });
            return;
        }

        if (!item.ScanComplete)
        {
            return;
        }

        BuildMeshesSection(item);
        if (item.HasSkeleton)
        {
            BuildSkeletonSection(item);
        }
        BuildAnimationsSection(item);
        BuildOptionsSection(item);
    }

    private static void BuildMeshesSection(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = NiziUi.CollapsibleSection("MeshesSection", "Meshes", true)
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

            using (NiziUi.Panel("MeshRow_" + i)
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Padding(2, 4)
                .Open())
            {
                var enabled = mesh.IsEnabled;
                var newEnabled = NiziUi.Checkbox("MeshEnable_" + i, "", enabled)
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
                if (NiziUi.TextField("MeshName_" + i, ref exportName)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .Width(UiSizing.Grow(120))
                    .Show())
                {
                    mesh.ExportName = exportName;
                }

                NiziUi.Text(mesh.DisplayInfo, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }

        if (item.Meshes.Count == 0)
        {
            NiziUi.Text("No meshes found", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildSkeletonSection(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using (NiziUi.Panel("SkeletonRow")
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
            var newExportSkel = NiziUi.Checkbox("ImportExportSkel", "Export Skeleton", exportSkel)
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

            using (NiziUi.Panel("SkelSpacer").GrowWidth().Open()) { }

            var info = $"{item.JointCount} joints, root: {item.RootJointName}";
            NiziUi.Text(info, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildAnimationsSection(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = NiziUi.CollapsibleSection("AnimsSection", "Animations", true)
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
        var newExportAnims = NiziUi.Checkbox("ImportExportAnims", "Export Animations", exportAnims)
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

            using (NiziUi.Panel("AnimRow_" + i)
                .Horizontal()
                .Gap(8)
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Padding(2, 4)
                .Open())
            {
                var enabled = anim.IsEnabled;
                var newEnabled = NiziUi.Checkbox("AnimEnable_" + i, "", enabled)
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
                if (NiziUi.TextField("AnimName_" + i, ref exportName)
                    .BackgroundColor(t.SurfaceInset)
                    .TextColor(t.TextPrimary)
                    .BorderColor(t.Border, t.Accent)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(6, 4)
                    .Width(UiSizing.Grow(120))
                    .Show())
                {
                    anim.ExportName = exportName;
                }

                NiziUi.Text(anim.DisplayInfo, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }

        if (item.Animations.Count == 0)
        {
            NiziUi.Text("No animations found", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildOptionsSection(ImportAssetItemViewModel item)
    {
        var t = EditorTheme.Current;

        using var section = NiziUi.CollapsibleSection("OptionsSection", "Options")
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
            var grid = NiziUi.PropertyGrid("OptsGrid")
                .LabelWidth(140)
                .FontSize(t.FontSizeCaption)
                .RowHeight(26)
                .Gap(4)
                .LabelColor(t.TextSecondary)
                .Open();

            {
                using var row = grid.Row("Scale");
                var scale = item.Scale;
                if (NiziUi.DraggableValue("ImportScale")
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

        using (NiziUi.Panel("OptsChecks1")
            .Horizontal()
            .Gap(16)
            .GrowWidth()
            .FitHeight()
            .Padding(0, 4)
            .Open())
        {
            item.OptimizeMeshes = CheckboxOption("OptOptimize", "Optimize Meshes", item.OptimizeMeshes);
            item.GenerateNormals = CheckboxOption("OptNormals", "Generate Normals", item.GenerateNormals);
            item.CalculateTangents = CheckboxOption("OptTangents", "Tangents", item.CalculateTangents);
        }

        using (NiziUi.Panel("OptsChecks2")
            .Horizontal()
            .Gap(16)
            .GrowWidth()
            .FitHeight()
            .Padding(0, 4)
            .Open())
        {
            item.TriangulateMeshes = CheckboxOption("OptTriangulate", "Triangulate", item.TriangulateMeshes);
            item.JoinIdenticalVertices = CheckboxOption("OptJoinVerts", "Join Vertices", item.JoinIdenticalVertices);
        }

        using (NiziUi.Panel("OptsSmoothRow")
            .Horizontal()
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Padding(0, 4)
            .Open())
        {
            item.SmoothNormals = CheckboxOption("OptSmooth", "Smooth Normals", item.SmoothNormals);
            NiziUi.Text("Angle:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            var angle = item.SmoothNormalsAngle;
            if (NiziUi.DraggableValue("OptSmoothAngle")
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

        using (NiziUi.Panel("OptsBoneRow")
            .Horizontal()
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Padding(0, 4)
            .Open())
        {
            item.LimitBoneWeights = CheckboxOption("OptBoneWeights", "Limit Bone Weights", item.LimitBoneWeights);
            NiziUi.Text("Max:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            var maxWeights = (float)item.MaxBoneWeightsPerVertex;
            if (NiziUi.DraggableValue("OptMaxBoneW")
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

    private static bool CheckboxOption(string id, string label, bool value)
    {
        var t = EditorTheme.Current;
        return NiziUi.Checkbox(id, label, value)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .LabelColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .Show();
    }

    private static void BuildFooter(EditorViewModel vm, ImportViewModel importVm)
    {
        var t = EditorTheme.Current;

        using var footer = NiziUi.Panel("ImportFooter")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(10)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open();

        NiziUi.Text("Output:", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
        var outDir = importVm.OutputDirectory;
        if (NiziUi.TextField("ImportOutDir", ref outDir)
            .BackgroundColor(t.SurfaceInset)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .Width(UiSizing.Fixed(200))
            .Placeholder("Assets/...")
            .Show())
        {
            importVm.OutputDirectory = outDir;
        }

        if (importVm.IsImporting)
        {
            if (!string.IsNullOrEmpty(importVm.ProgressText))
            {
                NiziUi.Text(importVm.ProgressText, new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
            }

            using (NiziUi.Panel("ImportProgress")
                .Background(t.SurfaceInset)
                .Width(120)
                .Height(6)
                .CornerRadius(3)
                .Open())
            {
                var fillWidth = (float)(importVm.Progress / 100.0 * 120);
                using (NiziUi.Panel("ImportProgressFill")
                    .Background(t.Accent)
                    .Width(fillWidth)
                    .Height(6)
                    .CornerRadius(3)
                    .Open()) { }
            }
        }
        else if (importVm.ImportSucceeded && !string.IsNullOrEmpty(importVm.ProgressText))
        {
            NiziUi.Text(importVm.ProgressText, new UiTextStyle { Color = t.Success, FontSize = t.FontSizeCaption });
        }

        NiziUi.FlexSpacer();
        if (EditorUi.GhostButton("ImportCancel", importVm.IsImporting ? "Cancel" : "Close"))
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
            if (EditorUi.AccentButton("ImportStart", "Import All"))
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
