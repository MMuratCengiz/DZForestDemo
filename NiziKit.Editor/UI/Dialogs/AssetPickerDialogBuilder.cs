using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public class FolderNode
{
    public string Name { get; init; } = "";
    public string FullPath { get; init; } = "";
    public List<FolderNode> Children { get; } = [];
    public bool IsExpanded { get; set; } = true;
}

public class AssetPickerState
{
    public string SearchText { get; set; } = "";
    public AssetRefType? CachedAssetType { get; set; }
    public List<AssetInfo> CachedAssets { get; set; } = [];
    public FolderNode? RootFolder { get; set; }
    public string? SelectedFolder { get; set; }
    public int CurrentPage { get; set; }
    public int FolderNodeIndex { get; set; }
}

public static class AssetPickerDialogBuilder
{
    private const int GridColumns = 5;
    private const int ItemsPerPage = 20;

    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var state = ctx.GetOrCreateState<AssetPickerState>("AssetPickerDialog");

        if (state.CachedAssetType != vm.AssetPickerAssetType)
        {
            state.CachedAssetType = vm.AssetPickerAssetType;
            state.CachedAssets = vm.AssetBrowser.GetAllAssetsOfType(vm.AssetPickerAssetType).ToList();
            state.RootFolder = BuildFolderTree(state.CachedAssets);
            state.SelectedFolder = null;
            state.SearchText = "";
            state.CurrentPage = 0;
        }

        using var overlay = EditorUi.DialogOverlay(ui, "AssetPickerOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "AssetPickerDialog",
            "Select " + vm.AssetPickerAssetType, 850, 580);

        RenderSearchBar(ui, ctx, state, t);

        using (ui.Panel("AssetPickerContent")
            .Horizontal()
            .Grow()
            .Open())
        {
            RenderFolderPanel(ui, ctx, state, t);
            Ui.VerticalDivider(ctx, t.Border);
            RenderAssetPanel(ui, ctx, vm, state, t);
        }

        RenderFooter(ui, ctx, vm, state, t);
    }

    private static void RenderSearchBar(UiFrame ui, UiContext ctx, AssetPickerState state, IEditorTheme t)
    {
        using (ui.Panel("SearchBarPanel")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .Padding(t.SpacingMD, t.SpacingSM)
            .Gap(t.SpacingSM)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelBackground)
            .Open())
        {
            ui.Icon(FontAwesome.Search, t.TextMuted, t.IconSizeSmall);

            var search = state.SearchText;
            if (Ui.TextField(ctx, "AssetSearchField", ref search)
                .BackgroundColor(t.SurfaceInset)
                .TextColor(t.TextPrimary)
                .PlaceholderColor(t.TextMuted)
                .FontSize(t.FontSizeCaption)
                .Placeholder("Search assets...")
                .GrowWidth()
                .Height(24)
                .CornerRadius(t.RadiusSmall)
                .BorderColor(t.Border)
                .Padding(6, 4)
                .Show(ref search))
            {
                state.CurrentPage = 0;
            }
            state.SearchText = search;
        }

        using (ui.Panel("SearchDivider").GrowWidth().Height(1).Background(t.Border).Open()) { }
    }

    private static void RenderFolderPanel(UiFrame ui, UiContext ctx, AssetPickerState state, IEditorTheme t)
    {
        using (ui.Panel("AssetFolderTree")
            .Vertical()
            .Width(UiSizing.Fixed(200))
            .GrowHeight()
            .Background(t.SurfaceInset)
            .ScrollVertical()
            .Padding(t.SpacingXS)
            .Gap(1)
            .Open())
        {
            var allSelected = state.SelectedFolder == null;
            var allEl = ui.Panel("FolderAll")
                .Horizontal()
                .GrowWidth()
                .Height(24)
                .AlignChildrenY(UiAlignY.Center)
                .AlignChildrenX(UiAlignX.Left)
                .Padding(6, 0)
                .Gap(t.SpacingXS)
                .CornerRadius(t.RadiusSmall);

            var allHovered = allEl.IsHovered();
            allEl = allEl.Background(allSelected ? t.Selected : allHovered ? t.Hover : UiColor.Transparent);

            using (allEl.Open())
            {
                ui.Icon(FontAwesome.FolderOpen,
                    allSelected ? t.AccentLight : t.TextMuted, t.IconSizeSmall);
                ui.Text("All Assets", new UiTextStyle
                {
                    Color = allSelected ? UiColor.White : t.TextPrimary,
                    FontSize = t.FontSizeCaption
                });
            }

            if (allEl.WasClicked())
            {
                state.SelectedFolder = null;
                state.CurrentPage = 0;
            }

            Ui.VerticalSpacer(ctx, 2);

            if (state.RootFolder != null)
            {
                state.FolderNodeIndex = 0;
                RenderFolderNode(ui, ctx, state, state.RootFolder, 0, t);
            }
        }
    }

    private static void RenderAssetPanel(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetPickerState state, IEditorTheme t)
    {
        var filteredAssets = GetFilteredAssets(state.CachedAssets, state.SelectedFolder, state.SearchText);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredAssets.Count / ItemsPerPage));
        state.CurrentPage = Math.Clamp(state.CurrentPage, 0, totalPages - 1);

        var startIdx = state.CurrentPage * ItemsPerPage;
        var endIdx = Math.Min(startIdx + ItemsPerPage, filteredAssets.Count);

        using (ui.Panel("AssetGridPanel")
            .Vertical()
            .Grow()
            .Open())
        {
            {
                var noneSelected = vm.AssetPickerCurrentAssetPath == null;
                var noneEl = ui.Panel("AssetPick_None")
                    .Horizontal()
                    .GrowWidth()
                    .FitHeight()
                    .Padding(t.SpacingMD, t.SpacingXS)
                    .Gap(t.SpacingXS)
                    .AlignChildrenY(UiAlignY.Center)
                    .AlignChildrenX(UiAlignX.Left)
                    .CornerRadius(t.RadiusSmall);

                var noneHovered = noneEl.IsHovered();
                noneEl = noneEl.Background(noneSelected ? t.Selected : noneHovered ? t.Hover : UiColor.Transparent);

                using (noneEl.Open())
                {
                    ui.Icon(FontAwesome.Times,
                        noneSelected ? UiColor.White : t.TextMuted, t.IconSizeSmall);
                    ui.Text("None (Clear)", new UiTextStyle
                    {
                        Color = noneSelected ? UiColor.White : t.TextMuted,
                        FontSize = t.FontSizeCaption
                    });
                }

                if (noneEl.WasClicked())
                {
                    vm.OnAssetPickerSelected(null);
                }
            }

            using (ui.Panel("AssetPickerDivider2").GrowWidth().Height(1).Background(t.Border.WithAlpha(80)).Open()) { }

            using (ui.Panel("AssetGrid")
                .Vertical()
                .Grow()
                .ScrollVertical()
                .Padding(t.SpacingMD)
                .Gap(t.SpacingSM)
                .Open())
            {
                if (filteredAssets.Count == 0)
                {
                    using (ui.Panel("EmptyState")
                        .Vertical()
                        .Grow()
                        .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                        .Gap(t.SpacingSM)
                        .Open())
                    {
                        ui.Icon(FontAwesome.Search, t.TextDisabled, t.IconSizeXL);
                        ui.Text("No assets found", new UiTextStyle
                        {
                            Color = t.TextMuted,
                            FontSize = t.FontSizeBody
                        });
                    }
                }
                else
                {
                    var pageItems = endIdx - startIdx;
                    var rows = (pageItems + GridColumns - 1) / GridColumns;

                    for (var row = 0; row < rows; row++)
                    {
                        using (ui.Panel("GR_" + row)
                            .Horizontal()
                            .GrowWidth()
                            .FitHeight()
                            .Gap(t.SpacingSM)
                            .Open())
                        {
                            for (var col = 0; col < GridColumns; col++)
                            {
                                var itemIdx = startIdx + row * GridColumns + col;
                                if (itemIdx < endIdx)
                                {
                                    RenderGridItem(ui, ctx, vm, filteredAssets[itemIdx], itemIdx, t);
                                }
                                else
                                {
                                    using (ui.Panel("GE_" + row + "_" + col)
                                        .GrowWidth()
                                        .Height(ItemHeight)
                                        .Open()) { }
                                }
                            }
                        }
                    }
                }
            }

            if (filteredAssets.Count > 0)
            {
                RenderPagination(ui, ctx, state, filteredAssets.Count, totalPages, t);
            }
        }
    }

    private const float ItemHeight = 105;

    private static void RenderGridItem(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetInfo asset, int index, IEditorTheme t)
    {
        var isSelected = vm.AssetPickerCurrentAssetPath == asset.Path;
        var bg = isSelected ? t.Selected : t.SurfaceInset;

        var btn = Ui.Button(ctx, "AI_" + index, "")
            .Color(bg, t.Hover, t.Active)
            .Border(isSelected ? 1 : 0, isSelected ? t.SelectedBorder : UiColor.Transparent)
            .CornerRadius(t.RadiusMedium)
            .Vertical()
            .GrowWidth()
            .Height(ItemHeight)
            .Padding(t.SpacingSM)
            .Gap(t.SpacingXS);

        using var scope = btn.Open();

        using (ui.Panel("AIcon_" + index)
            .GrowWidth()
            .Grow()
            .AlignChildren(UiAlignX.Center, UiAlignY.Center)
            .Open())
        {
            ui.Icon(GetAssetIcon(vm.AssetPickerAssetType),
                isSelected ? UiColor.White : t.Accent, 36);
        }

        using (ui.Panel("ALabel_" + index)
            .GrowWidth()
            .FitHeight(0, 28)
            .AlignChildrenX(UiAlignX.Center)
            .Open())
        {
            ui.Text(asset.Name, new UiTextStyle
            {
                Color = isSelected ? UiColor.White : t.TextSecondary,
                FontSize = t.FontSizeCaption,
                Alignment = UiTextAlign.Center
            });
        }

        if (btn.WasClicked())
        {
            vm.OnAssetPickerSelected(asset);
        }
    }

    private static void RenderPagination(UiFrame ui, UiContext ctx, AssetPickerState state,
        int totalItems, int totalPages, IEditorTheme t)
    {
        using (ui.Panel("Pagination")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .Padding(t.SpacingSM, t.SpacingXS)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.SurfaceInset)
            .Open())
        {
            ui.Text(totalItems + " items", new UiTextStyle
            {
                Color = t.TextMuted,
                FontSize = t.FontSizeCaption
            });

            Ui.FlexSpacer(ctx);

            var canPrev = state.CurrentPage > 0;
            {
                var prevBtn = Ui.Button(ctx, "PrevPage", "")
                    .Color(UiColor.Transparent, t.Hover, t.Active)
                    .Padding(4, 3)
                    .CornerRadius(t.RadiusSmall)
                    .Border(0, UiColor.Transparent);

                using var prevScope = prevBtn.Open();
                prevScope.Icon(FontAwesome.ChevronLeft,
                    canPrev ? t.TextPrimary : t.TextDisabled, t.IconSizeSmall);

                if (prevBtn.WasClicked() && canPrev)
                {
                    state.CurrentPage--;
                }
            }

            ui.Text($"{state.CurrentPage + 1} / {totalPages}", new UiTextStyle
            {
                Color = t.TextSecondary,
                FontSize = t.FontSizeCaption
            });

            var canNext = state.CurrentPage < totalPages - 1;
            {
                var nextBtn = Ui.Button(ctx, "NextPage", "")
                    .Color(UiColor.Transparent, t.Hover, t.Active)
                    .Padding(4, 3)
                    .CornerRadius(t.RadiusSmall)
                    .Border(0, UiColor.Transparent);

                using var nextScope = nextBtn.Open();
                nextScope.Icon(FontAwesome.ChevronRight,
                    canNext ? t.TextPrimary : t.TextDisabled, t.IconSizeSmall);

                if (nextBtn.WasClicked() && canNext)
                {
                    state.CurrentPage++;
                }
            }
        }
    }

    private static void RenderFooter(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetPickerState state, IEditorTheme t)
    {
        using (ui.Panel("AssetPickerFooter")
            .Horizontal()
            .Padding(t.SpacingMD, t.SpacingSM)
            .Gap(t.SpacingSM)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenX(UiAlignX.Right)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelElevated)
            .Open())
        {
            if (EditorUi.GhostButton(ctx, "AssetPickerCancel", "Cancel"))
            {
                vm.CloseAssetPicker();
            }
        }
    }

    private static void RenderFolderNode(UiFrame ui, UiContext ctx,
        AssetPickerState state, FolderNode node, int depth, IEditorTheme t)
    {
        var guideColor = t.Border.WithAlpha(80);

        foreach (var child in node.Children)
        {
            var hasChildren = child.Children.Count > 0;
            var isSelected = state.SelectedFolder == child.FullPath;
            var idx = state.FolderNodeIndex++;

            var rowEl = ui.Panel("FR_" + idx)
                .Horizontal()
                .GrowWidth()
                .Height(24)
                .AlignChildrenY(UiAlignY.Center)
                .AlignChildrenX(UiAlignX.Left)
                .Padding(4, 0)
                .Gap(2)
                .CornerRadius(t.RadiusSmall);

            var rowHovered = rowEl.IsHovered();
            rowEl = rowEl.Background(isSelected ? t.Selected : rowHovered ? t.Hover : UiColor.Transparent);

            var chevronClicked = false;

            using (rowEl.Open())
            {
                if (depth > 0)
                {
                    using (ui.Panel("FGs_" + idx)
                        .Horizontal()
                        .Width(depth * 14)
                        .Height(24)
                        .Open())
                    {
                        for (var i = 0; i < depth; i++)
                        {
                            using (ui.Panel("FG_" + idx + "_" + i)
                                .Width(14)
                                .Height(24)
                                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                                .Open())
                            {
                                using (ui.Panel("FL_" + idx + "_" + i)
                                    .Width(1)
                                    .Height(24)
                                    .Background(guideColor)
                                    .Open()) { }
                            }
                        }
                    }
                }

                if (hasChildren)
                {
                    var chevron = child.IsExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;
                    if (EditorUi.IconButton(ctx, "FE_" + idx, chevron))
                    {
                        child.IsExpanded = !child.IsExpanded;
                        chevronClicked = true;
                    }
                }
                else
                {
                    Ui.HorizontalSpacer(ctx, 22);
                }

                ui.Icon(FontAwesome.Folder,
                    isSelected ? t.AccentLight : t.TextMuted, t.IconSizeSmall);
                ui.Text(child.Name, new UiTextStyle
                {
                    Color = isSelected ? UiColor.White : t.TextPrimary,
                    FontSize = t.FontSizeCaption
                });
            }

            if (!chevronClicked && rowEl.WasClicked())
            {
                state.SelectedFolder = child.FullPath;
                state.CurrentPage = 0;
            }

            if (hasChildren && child.IsExpanded)
            {
                RenderFolderNode(ui, ctx, state, child, depth + 1, t);
            }
        }
    }

    private static FolderNode BuildFolderTree(List<AssetInfo> assets)
    {
        var root = new FolderNode { Name = "", FullPath = "" };

        foreach (var asset in assets)
        {
            var path = asset.Path;
            var lastSlash = path.LastIndexOfAny(['/', '\\']);
            if (lastSlash < 0)
            {
                continue;
            }

            var folderPath = path[..lastSlash];
            var parts = folderPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

            var current = root;
            var pathBuilder = "";

            foreach (var part in parts)
            {
                pathBuilder = string.IsNullOrEmpty(pathBuilder) ? part : pathBuilder + "/" + part;
                var existing = current.Children.Find(c => c.Name == part);
                if (existing == null)
                {
                    existing = new FolderNode { Name = part, FullPath = pathBuilder };
                    current.Children.Add(existing);
                }
                current = existing;
            }
        }

        SortFolders(root);
        return root;
    }

    private static void SortFolders(FolderNode node)
    {
        node.Children.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var child in node.Children)
        {
            SortFolders(child);
        }
    }

    private static List<AssetInfo> GetFilteredAssets(List<AssetInfo> assets, string? selectedFolder, string searchText)
    {
        IEnumerable<AssetInfo> result = assets;

        if (!string.IsNullOrEmpty(selectedFolder))
        {
            var prefix = selectedFolder + "/";
            result = result.Where(a =>
                a.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                (a.Path.StartsWith(selectedFolder, StringComparison.OrdinalIgnoreCase) &&
                 a.Path.LastIndexOfAny(['/', '\\']) == selectedFolder.Length));
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            result = result.Where(a =>
                a.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return result.ToList();
    }

    private static string GetAssetIcon(AssetRefType assetType)
    {
        return assetType switch
        {
            AssetRefType.Mesh => FontAwesome.Cube,
            AssetRefType.Texture => FontAwesome.Image,
            AssetRefType.Skeleton => FontAwesome.Bone,
            AssetRefType.Animation => FontAwesome.Film,
            _ => FontAwesome.File
        };
    }
}
