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
    public bool IsExpanded { get; set; }
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
    public string? HoveredAssetPath { get; set; }
    public int FilteredAssetCount { get; set; }
    public int TotalPages { get; set; }
}

public static class AssetPickerDialogBuilder
{
    private const int GridColumns = 4;
    private const int ItemsPerPage = 20;
    private const float DialogWidth = 850;
    private const float FolderPanelWidth = 200;
    private const float SearchBarHeight = 40;
    private const float FolderIconWidth = 16;

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

        var overlayEl = ui.Panel("AssetPickerOverlay")
            .Background(t.DialogOverlay)
            .FloatingRoot(900)
            .Grow()
            .AlignChildren(UiAlignX.Center, UiAlignY.Center);
        var overlayClicked = overlayEl.WasClicked();

        using (overlayEl.Open())
        {
            var dialogEl = ui.Panel("AssetPickerDialog")
                .Background(t.PanelBackground)
                .Border(1, t.Border)
                .CornerRadius(t.RadiusLarge)
                .Width(850)
                .Height(580)
                .Vertical();
            var dialogHovered = dialogEl.IsHovered();

            using (dialogEl.Open())
            {
                RenderHeader(ui, ctx, vm, t);
                using (ui.Panel("AssetPickerDialog_div").GrowWidth().Height(1).Background(t.Border).Open()) { }
                RenderSearchBar(ui, ctx, state, t);

                using (ui.Panel("AssetPickerContent")
                    .Horizontal()
                    .Grow()
                    .Open())
                {
                    RenderFolderPanel(ui, ctx, state, t);
                    Ui.VerticalDivider(ctx, t.Border);
                    RenderAssetGrid(ui, ctx, vm, state, t);
                }

                RenderFooter(ui, ctx, vm, state, t);
            }

            if (overlayClicked && !dialogHovered)
            {
                vm.CloseAssetPicker();
            }
        }
    }

    private static void RenderHeader(UiFrame ui, UiContext ctx, EditorViewModel vm, IEditorTheme t)
    {
        using (ui.Panel("AssetPickerDialog_header")
            .Background(t.Accent)
            .Padding(20, 10)
            .GrowWidth()
            .Height(44)
            .Horizontal()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            ui.Text("Select " + vm.AssetPickerAssetType,
                new UiTextStyle { Color = UiColor.White, FontSize = t.FontSizeSubtitle });

            Ui.FlexSpacer(ctx);

            var closeEl = ui.Panel("AssetPickerClose")
                .Width(24)
                .Height(24)
                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                .CornerRadius(t.RadiusSmall);

            var closeHovered = closeEl.IsHovered();
            closeEl = closeEl.Background(closeHovered ? UiColor.Rgba(255, 255, 255, 40) : UiColor.Transparent);

            using (closeEl.Open())
            {
                ui.Icon(FontAwesome.Xmark, UiColor.White, t.IconSizeSmall);
            }

            if (closeEl.WasClicked())
            {
                vm.CloseAssetPicker();
            }
        }
    }

    private static void RenderSearchBar(UiFrame ui, UiContext ctx, AssetPickerState state, IEditorTheme t)
    {
        using (ui.Panel("SearchBarPanel")
            .Horizontal()
            .GrowWidth()
            .Height(SearchBarHeight)
            .Padding(t.SpacingMD, t.SpacingSM)
            .Gap(t.SpacingSM)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelBackground)
            .Open())
        {
            using (ui.Panel("SearchIcon")
                .Width(t.IconSizeSmall)
                .Height(t.IconSizeSmall)
                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                .Open())
            {
                ui.Icon(FontAwesome.Search, t.TextMuted, t.IconSizeSmall);
            }

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
                .Overflow(UiTextOverflow.Scroll)
                .Show())
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
            .CornerRadius(new UiCornerRadius(0, 0, t.RadiusLarge, 0))
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
                .Gap(2)
                .CornerRadius(t.RadiusSmall);

            var allHovered = allEl.IsHovered();
            allEl = allEl.Background(allSelected ? t.Selected : allHovered ? t.Hover : UiColor.Transparent);

            using (allEl.Open())
            {
                var leftWidth = FolderChevronWidth + FolderIconWidth + 2;
                using (ui.Panel("FolderAllLeft")
                    .Horizontal()
                    .Width(leftWidth)
                    .Height(24)
                    .AlignChildrenY(UiAlignY.Center)
                    .Gap(2)
                    .Open())
                {
                    Ui.HorizontalSpacer(ctx, FolderChevronWidth);

                    using (ui.Panel("FolderAllIcon")
                        .Width(FolderIconWidth)
                        .Height(FolderIconWidth)
                        .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                        .Open())
                    {
                        ui.Icon(FontAwesome.FolderOpen,
                            allSelected ? t.AccentLight : t.TextMuted, t.IconSizeSmall);
                    }
                }

                using (ui.Panel("FolderAllText")
                    .GrowWidth()
                    .Height(24)
                    .AlignChildrenY(UiAlignY.Center)
                    .Open())
                {
                    ui.Text("All Assets", new UiTextStyle
                    {
                        Color = allSelected ? UiColor.White : t.TextPrimary,
                        FontSize = t.FontSizeCaption
                    });
                }
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

    private static void RenderAssetGrid(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetPickerState state, IEditorTheme t)
    {
        var filteredAssets = GetFilteredAssets(state.CachedAssets, state.SelectedFolder, state.SearchText);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredAssets.Count / ItemsPerPage));
        state.CurrentPage = Math.Clamp(state.CurrentPage, 0, totalPages - 1);
        state.FilteredAssetCount = filteredAssets.Count;
        state.TotalPages = totalPages;

        var startIdx = state.CurrentPage * ItemsPerPage;
        var endIdx = Math.Min(startIdx + ItemsPerPage, filteredAssets.Count);

        var gridInnerWidth = DialogWidth - FolderPanelWidth - 2 * t.SpacingMD;
        var tileWidth = (gridInnerWidth - (GridColumns - 1) * t.SpacingSM) / GridColumns;
        var labelMaxWidth = tileWidth - 2 * TilePaddingH;

        state.HoveredAssetPath = null;

        // Get grid area bounds from previous frame to clip interaction
        var gridId = ctx.GetElementId("AssetGrid");
        var gridBounds = ctx.GetElementBounds(gridId);
        var mouseInGrid = gridBounds.Width > 0 &&
                          ctx.MouseX >= gridBounds.X && ctx.MouseX <= gridBounds.X + gridBounds.Width &&
                          ctx.MouseY >= gridBounds.Y && ctx.MouseY <= gridBounds.Y + gridBounds.Height;

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
                                RenderGridItem(ui, ctx, vm, state, filteredAssets[itemIdx], itemIdx, t,
                                    labelMaxWidth, mouseInGrid);
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
    }

    private const float LabelLineHeight = 16;
    private const float LabelAreaHeight = LabelLineHeight * 2;
    private const float IconAreaHeight = 48;
    private const float TilePaddingV = 6;
    private const float TilePaddingH = 6;
    private const float TileGap = 4;
    private const float ItemHeight = TilePaddingV * 2 + IconAreaHeight + TileGap + LabelAreaHeight;

    private static void RenderGridItem(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetPickerState state, AssetInfo asset, int index, IEditorTheme t, float labelMaxWidth,
        bool mouseInGrid)
    {
        var isSelected = vm.AssetPickerCurrentAssetPath == asset.Path;
        var bg = isSelected ? t.Selected : t.SurfaceInset;

        var btn = Ui.Button(ctx, "AI_" + index, "")
            .Color(bg, mouseInGrid ? t.Hover : bg, mouseInGrid ? t.Active : bg)
            .Border(isSelected ? 1 : 0, isSelected ? t.SelectedBorder : UiColor.Transparent)
            .CornerRadius(t.RadiusMedium)
            .Vertical()
            .GrowWidth()
            .Height(ItemHeight)
            .Padding(TilePaddingH, TilePaddingV)
            .Gap(TileGap);

        using var scope = btn.Open();

        using (ui.Panel("AIcon_" + index)
            .GrowWidth()
            .Height(IconAreaHeight)
            .AlignChildren(UiAlignX.Center, UiAlignY.Center)
            .Open())
        {
            ui.Icon(GetAssetIcon(vm.AssetPickerAssetType),
                isSelected ? UiColor.White : t.Accent, t.IconSizeLarge);
        }

        var lines = SplitTileLabel(ctx, asset.Name, t.FontSizeCaption, labelMaxWidth);
        var labelColor = isSelected ? UiColor.White : t.TextSecondary;
        var labelStyle = new UiTextStyle
        {
            Color = labelColor,
            FontSize = t.FontSizeCaption,
            Alignment = UiTextAlign.Center
        };

        using (ui.Panel("ALabel_" + index)
            .Vertical()
            .GrowWidth()
            .Height(LabelAreaHeight)
            .AlignChildrenX(UiAlignX.Center)
            .AlignChildrenY(UiAlignY.Top)
            .Open())
        {
            for (var i = 0; i < lines.Length; i++)
            {
                using (ui.Panel("ALn_" + index + "_" + i)
                    .GrowWidth()
                    .Height(LabelLineHeight)
                    .AlignChildrenX(UiAlignX.Center)
                    .AlignChildrenY(UiAlignY.Center)
                    .Open())
                {
                    ui.Text(lines[i], labelStyle);
                }
            }
        }

        if (mouseInGrid && btn.WasClicked())
        {
            vm.OnAssetPickerSelected(asset);
        }
        else if (mouseInGrid && ctx.Clay.PointerOver(btn.Id))
        {
            state.HoveredAssetPath = asset.Path;
        }
    }

    private static string[] SplitTileLabel(UiContext ctx, string text, ushort fontSize, float maxWidth)
    {
        if (maxWidth <= 0)
        {
            return [text];
        }

        var measured = ctx.Clay.MeasureText(text, 0, fontSize);
        if (measured.Width <= maxWidth)
        {
            return [text];
        }

        var line1Chars = (int)ctx.Clay.GetCharIndexAtOffset(text, maxWidth, 0, fontSize);
        if (line1Chars <= 0)
        {
            line1Chars = 1;
        }

        if (line1Chars >= text.Length)
        {
            return [text];
        }

        var line1 = text[..line1Chars];
        var line2Full = text[line1Chars..];

        var line2Measured = ctx.Clay.MeasureText(line2Full, 0, fontSize);
        if (line2Measured.Width <= maxWidth)
        {
            return [line1, line2Full];
        }

        const string ellipsis = "...";
        var ellipsisWidth = ctx.Clay.MeasureText(ellipsis, 0, fontSize).Width;
        var remaining = maxWidth - ellipsisWidth;
        if (remaining <= 0)
        {
            return [line1, ellipsis];
        }

        var fitChars = (int)ctx.Clay.GetCharIndexAtOffset(line2Full, remaining, 0, fontSize);
        if (fitChars > 0 && fitChars < line2Full.Length)
        {
            return [line1, line2Full[..fitChars] + ellipsis];
        }

        return [line1, ellipsis];
    }

    private static void RenderFooter(UiFrame ui, UiContext ctx, EditorViewModel vm,
        AssetPickerState state, IEditorTheme t)
    {
        using (ui.Panel("AssetPickerFooter")
            .Horizontal()
            .Padding(t.SpacingMD, t.SpacingSM)
            .Gap(t.SpacingSM)
            .GrowWidth()
            .Height(36)
            .AlignChildrenY(UiAlignY.Center)
            .Background(t.PanelElevated)
            .CornerRadius(UiCornerRadius.Bottom(t.RadiusLarge))
            .Open())
        {
            var infoPath = state.HoveredAssetPath ?? vm.AssetPickerCurrentAssetPath;
            if (!string.IsNullOrEmpty(infoPath))
            {
                ui.Text(infoPath, new UiTextStyle
                {
                    Color = t.TextMuted,
                    FontSize = t.FontSizeCaption
                });
            }

            Ui.FlexSpacer(ctx);

            ui.Text(state.FilteredAssetCount + " items", new UiTextStyle
            {
                Color = t.TextMuted,
                FontSize = t.FontSizeCaption
            });

            if (state.TotalPages > 1)
            {
                var canPrev = state.CurrentPage > 0;
                {
                    var prevBtn = Ui.Button(ctx, "PrevPage", "")
                        .Color(UiColor.Transparent, t.Hover, t.Active)
                        .Padding(6, 4)
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

                using (ui.Panel("PagPageInd")
                    .Width(UiSizing.Fit(40))
                    .FitHeight()
                    .AlignChildrenX(UiAlignX.Center)
                    .Open())
                {
                    ui.Text($"{state.CurrentPage + 1} / {state.TotalPages}", new UiTextStyle
                    {
                        Color = t.TextSecondary,
                        FontSize = t.FontSizeCaption
                    });
                }

                var canNext = state.CurrentPage < state.TotalPages - 1;
                {
                    var nextBtn = Ui.Button(ctx, "NextPage", "")
                        .Color(UiColor.Transparent, t.Hover, t.Active)
                        .Padding(6, 4)
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
    }

    private const float FolderIndentSize = 10;
    private const float FolderChevronWidth = 20;

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
                var leftWidth = depth * FolderIndentSize + FolderChevronWidth + FolderIconWidth + 2;
                using (ui.Panel("FLeft_" + idx)
                    .Horizontal()
                    .Width(leftWidth)
                    .Height(24)
                    .AlignChildrenY(UiAlignY.Center)
                    .Gap(2)
                    .Open())
                {
                    if (depth > 0)
                    {
                        using (ui.Panel("FGs_" + idx)
                            .Horizontal()
                            .Width(depth * FolderIndentSize)
                            .Height(24)
                            .Open())
                        {
                            for (var i = 0; i < depth; i++)
                            {
                                using (ui.Panel("FG_" + idx + "_" + i)
                                    .Width(FolderIndentSize)
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
                        var chevEl = ui.Panel("FE_" + idx)
                            .Width(FolderChevronWidth)
                            .Height(24)
                            .AlignChildren(UiAlignX.Center, UiAlignY.Center);

                        using (chevEl.Open())
                        {
                            var chevIcon = child.IsExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;
                            ui.Icon(chevIcon, t.TextSecondary, t.IconSizeSmall);
                        }

                        if (chevEl.WasClicked())
                        {
                            child.IsExpanded = !child.IsExpanded;
                            chevronClicked = true;
                        }
                    }
                    else
                    {
                        Ui.HorizontalSpacer(ctx, FolderChevronWidth);
                    }

                    using (ui.Panel("FI_" + idx)
                        .Width(FolderIconWidth)
                        .Height(FolderIconWidth)
                        .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                        .Open())
                    {
                        ui.Icon(FontAwesome.Folder,
                            isSelected ? t.AccentLight : t.TextMuted, t.IconSizeSmall);
                    }
                }

                using (ui.Panel("FT_" + idx)
                    .GrowWidth()
                    .Height(24)
                    .AlignChildrenY(UiAlignY.Center)
                    .Open())
                {
                    ui.Text(child.Name, new UiTextStyle
                    {
                        Color = isSelected ? UiColor.White : t.TextPrimary,
                        FontSize = t.FontSizeCaption
                    });
                }
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

        // Expand only top-level folders by default
        foreach (var child in root.Children)
        {
            child.IsExpanded = true;
        }

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
