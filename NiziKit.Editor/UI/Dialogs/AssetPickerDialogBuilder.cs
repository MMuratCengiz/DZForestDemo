using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public class AssetPickerState
{
    public string SearchText { get; set; } = "";
}

public static class AssetPickerDialogBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var state = ctx.GetOrCreateState<AssetPickerState>("AssetPickerDialog");

        using var overlay = EditorUi.DialogOverlay(ui, "AssetPickerOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "AssetPickerDialog",
            $"Select {vm.AssetPickerAssetType}", 550, 500);

        // Search bar
        using (ui.Panel("AssetPickerSearch")
            .Horizontal()
            .Padding(16, 10)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            ui.Icon(FontAwesome.MagnifyingGlass, t.TextMuted, t.IconSizeSmall);

            var searchText = state.SearchText;
            Ui.TextField(ctx, "AssetSearchField", ref searchText)
                .BackgroundColor(t.SurfaceInset, t.PanelBackground)
                .TextColor(t.TextPrimary)
                .PlaceholderColor(t.TextMuted)
                .BorderColor(t.Border, t.Accent)
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .Padding(8, 6)
                .GrowWidth()
                .Placeholder("Search assets...")
                .Show(ref searchText);
            state.SearchText = searchText;
        }

        // Divider
        using (ui.Panel("AssetPickerDiv").GrowWidth().Height(1).Background(t.Border).Open()) { }

        // Asset list
        var assets = vm.AssetBrowser.GetAllAssetsOfType(vm.AssetPickerAssetType);

        using (ui.Panel("AssetPickerList")
            .Vertical()
            .Grow()
            .ScrollVertical()
            .Gap(1)
            .Open())
        {
            // "(None)" option
            var noneSelected = vm.AssetPickerCurrentAssetPath == null;
            if (Ui.Button(ctx, "AssetPick_None", "(None)")
                .Color(noneSelected ? t.Selected : UiColor.Transparent, t.Hover, t.Active)
                .TextColor(t.TextMuted)
                .FontSize(t.FontSizeBody)
                .Padding(12, 8)
                .CornerRadius(0)
                .Border(0, UiColor.Transparent)
                .Show())
            {
                vm.OnAssetPickerSelected(null);
                state.SearchText = "";
            }

            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];

                if (!string.IsNullOrEmpty(state.SearchText) &&
                    !asset.Name.Contains(state.SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isSelected = vm.AssetPickerCurrentAssetPath == asset.Path;
                var bg = isSelected ? t.Selected : UiColor.Transparent;

                var btn = Ui.Button(ctx, "AssetPick_" + i, "")
                    .Color(bg, t.Hover, t.Active)
                    .Padding(12, 8)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .Horizontal()
                    .Gap(10);

                using var scope = btn.Open();
                scope.Icon(GetAssetIcon(vm.AssetPickerAssetType), t.Accent, t.IconSizeSmall);

                using (scope.Panel("AssetPickInfo_" + i).Vertical().Gap(2).GrowWidth().Open())
                {
                    scope.Text(asset.Name, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });
                    if (asset.Pack != null)
                    {
                        scope.Text(asset.Pack, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
                    }
                }

                if (btn.WasClicked())
                {
                    vm.OnAssetPickerSelected(asset);
                    state.SearchText = "";
                }
            }
        }

        // Footer
        using (ui.Panel("AssetPickerFooter")
            .Horizontal()
            .Padding(16, 12)
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
                state.SearchText = "";
            }
        }
    }

    private static string GetAssetIcon(AssetRefType type)
    {
        return type switch
        {
            AssetRefType.Mesh => FontAwesome.Cube,
            AssetRefType.Texture => FontAwesome.Image,
            AssetRefType.Skeleton => FontAwesome.Bone,
            AssetRefType.Animation => FontAwesome.Film,
            _ => FontAwesome.File
        };
    }
}
