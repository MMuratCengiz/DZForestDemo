using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class ViewportToolbarBuilder
{
    private static readonly string[] ViewPresets = ["Free", "Top", "Bottom", "Front", "Back", "Right", "Left"];

    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var row = NiziUi.Panel("ToolbarRow")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(6)
            .Open();

        using (NiziUi.Panel("ToolbarControls")
            .Horizontal()
            .Background(t.PanelBackground.WithAlpha(220))
            .Border(UiBorder.All(1, t.Border))
            .CornerRadius(t.RadiusMedium)
            .Padding(6, 3)
            .FitWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(6)
            .Open())
        {
            EditorUi.Badge(vm.GizmoModeText);

            NiziUi.VerticalDivider(t.Border);

            if (EditorUi.ToolbarToggle("GridToggle", FontAwesome.BorderAll, vm.GridStatusText, vm.ShowGrid))
            {
                vm.ToggleGrid();
            }

            if (EditorUi.ToolbarToggle("SnapToggle", FontAwesome.Magnet, vm.SnapStatusText, vm.SnapEnabled))
            {
                vm.ToggleSnap();
            }

            NiziUi.VerticalDivider(t.Border);

            var presetIndex = (int)vm.CurrentViewPreset;
            if (NiziUi.Dropdown("ViewPreset", ViewPresets)
                .Background(t.PanelElevated, t.Hover)
                .TextColor(t.TextPrimary)
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .Padding(6, 3)
                .Width(80)
                .ItemHoverColor(t.Hover)
                .DropdownBackground(t.PanelBackground)
                .Show(ref presetIndex))
            {
                vm.SetViewPreset(ViewPresets[presetIndex]);
            }

            if (EditorUi.ToolbarToggle("2DToggle", FontAwesome.Cube, vm.ProjectionModeText, vm.Is2DMode))
            {
                vm.Toggle2DMode();
            }
        }

        NiziUi.FlexSpacer();

        using (NiziUi.Panel("ToolbarStats")
            .Horizontal()
            .Background(t.PanelBackground.WithAlpha(220))
            .Border(UiBorder.All(1, t.Border))
            .CornerRadius(t.RadiusMedium)
            .Padding(6, 3)
            .Width(UiSizing.Fit(vm.ShowStatistics ? 140 : 0))
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(6)
            .Open())
        {
        if (vm.ShowStatistics)
            {
                NiziUi.Text($"{vm.Fps:F0} FPS", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
                NiziUi.Text($"({vm.FrameTime:F1}ms)", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }

            if (EditorUi.IconButton("StatsToggle", FontAwesome.ChartBar))
            {
                vm.ToggleStatistics();
            }
        }
    }
}
