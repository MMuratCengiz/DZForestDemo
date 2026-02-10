using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Dialogs;

public class AssetPickerState
{
    public string SearchText { get; set; } = "";
    public AssetRefType? CachedAssetType { get; set; }
    public List<AssetInfo> CachedAssets { get; set; } = new();
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
        }

        using var overlay = EditorUi.DialogOverlay(ui, "AssetPickerOverlay");
        using var dialog = EditorUi.DialogContainer(ui, ctx, "AssetPickerDialog",
            $"Select {vm.AssetPickerAssetType}", 550, 500);

        var assets = state.CachedAssets;

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

            for (var i = 0; i < assets.Count; i++)
            {
                var asset = assets[i];
                var isSelected = vm.AssetPickerCurrentAssetPath == asset.Path;
                var bg = isSelected ? t.Selected : UiColor.Transparent;

                if (Ui.Button(ctx, "AssetPick_" + i, asset.Name)
                    .Color(bg, t.Hover, t.Active)
                    .TextColor(t.TextPrimary)
                    .FontSize(t.FontSizeCaption)
                    .Padding(8, 6)
                    .CornerRadius(0)
                    .Border(0, UiColor.Transparent)
                    .GrowWidth()
                    .Show())
                {
                    vm.OnAssetPickerSelected(asset);
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
}
