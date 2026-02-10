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
}

public static class AssetPickerDialogBuilder
{
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
        }

        using var overlay = EditorUi.DialogOverlay(ui, "AssetPickerOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "AssetPickerDialog",
            "Select " + vm.AssetPickerAssetType, 700, 500);

        using (ui.Panel("AssetPickerContent")
            .Horizontal()
            .Grow()
            .Open())
        {
            using (ui.Panel("AssetFolderTree")
                .Vertical()
                .Width(UiSizing.Fixed(180))
                .GrowHeight()
                .Background(t.SurfaceInset)
                .ScrollVertical()
                .Padding(4)
                .Open())
            {
                if (Ui.Button(ctx, "FolderAll", "All Assets")
                    .Color(state.SelectedFolder == null ? t.Selected : UiColor.Transparent, t.Hover, t.Active)
                    .TextColor(t.TextPrimary)
                    .FontSize(t.FontSizeCaption)
                    .Padding(6, 4)
                    .CornerRadius(t.RadiusSmall)
                    .Border(0, UiColor.Transparent)
                    .GrowWidth()
                    .Show())
                {
                    state.SelectedFolder = null;
                }

                if (state.RootFolder != null)
                {
                    RenderFolderNode(ui, ctx, state, state.RootFolder, 0);
                }
            }

            using (ui.Panel("AssetListPanel")
                .Vertical()
                .Grow()
                .Open())
            {
                using (ui.Panel("AssetPickerList")
                    .Vertical()
                    .Grow()
                    .ScrollVertical()
                    .Gap(1)
                    .Open())
                {
                    var noneSelected = vm.AssetPickerCurrentAssetPath == null;
                    if (Ui.Button(ctx, "AssetPick_None", "(None)")
                        .Color(noneSelected ? t.Selected : UiColor.Transparent, t.Hover, t.Active)
                        .TextColor(t.TextMuted)
                        .FontSize(t.FontSizeCaption)
                        .Padding(8, 6)
                        .CornerRadius(0)
                        .Border(0, UiColor.Transparent)
                        .GrowWidth()
                        .Show())
                    {
                        vm.OnAssetPickerSelected(null);
                    }

                    var filteredAssets = GetFilteredAssets(state.CachedAssets, state.SelectedFolder);
                    for (var i = 0; i < filteredAssets.Count; i++)
                    {
                        var asset = filteredAssets[i];
                        var isSelected = vm.AssetPickerCurrentAssetPath == asset.Path;
                        var bg = isSelected ? t.Selected : UiColor.Transparent;

                        var btn = Ui.Button(ctx, "AssetPick_" + i, "")
                            .Color(bg, t.Hover, t.Active)
                            .Padding(8, 6)
                            .CornerRadius(0)
                            .Border(0, UiColor.Transparent)
                            .GrowWidth()
                            .Horizontal()
                            .Gap(8);

                        using var scope = btn.Open();
                        scope.Icon(GetAssetIcon(vm.AssetPickerAssetType), t.Accent, t.IconSizeSmall);
                        scope.Text(asset.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeCaption });

                        if (btn.WasClicked())
                        {
                            vm.OnAssetPickerSelected(asset);
                        }
                    }
                }
            }
        }

        using (ui.Panel("AssetPickerFooter")
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
            if (EditorUi.GhostButton(ctx, "AssetPickerCancel", "Cancel"))
            {
                vm.CloseAssetPicker();
            }
        }
    }

    private static void RenderFolderNode(UiFrame ui, UiContext ctx,
        AssetPickerState state, FolderNode node, int depth)
    {
        var t = EditorTheme.Current;
        foreach (var child in node.Children)
        {
            var hasChildren = child.Children.Count > 0;
            var isSelected = state.SelectedFolder == child.FullPath;
            var indent = depth * 12;

            using (ui.Panel("FolderRow_" + child.FullPath.GetHashCode())
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                if (indent > 0)
                {
                    Ui.HorizontalSpacer(ctx, indent);
                }

                if (hasChildren)
                {
                    var chevron = child.IsExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;
                    if (EditorUi.IconButton(ctx, "FolderExp_" + child.FullPath.GetHashCode(), chevron))
                    {
                        child.IsExpanded = !child.IsExpanded;
                    }
                }
                else
                {
                    Ui.HorizontalSpacer(ctx, 20);
                }

                if (Ui.Button(ctx, "Folder_" + child.FullPath.GetHashCode(), child.Name)
                    .Color(isSelected ? t.Selected : UiColor.Transparent, t.Hover, t.Active)
                    .TextColor(t.TextPrimary)
                    .FontSize(t.FontSizeCaption)
                    .Padding(4, 3)
                    .CornerRadius(t.RadiusSmall)
                    .Border(0, UiColor.Transparent)
                    .GrowWidth()
                    .Show())
                {
                    state.SelectedFolder = child.FullPath;
                }
            }

            if (hasChildren && child.IsExpanded)
            {
                RenderFolderNode(ui, ctx, state, child, depth + 1);
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

    private static List<AssetInfo> GetFilteredAssets(List<AssetInfo> assets, string? selectedFolder)
    {
        if (string.IsNullOrEmpty(selectedFolder))
        {
            return assets;
        }

        var prefix = selectedFolder + "/";
        return assets.Where(a => a.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                                  a.Path.StartsWith(selectedFolder, StringComparison.OrdinalIgnoreCase) &&
                                  a.Path.LastIndexOfAny(['/', '\\']) == selectedFolder.Length - 1)
            .ToList();
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
