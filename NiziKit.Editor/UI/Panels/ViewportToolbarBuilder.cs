using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class ViewportToolbarBuilder
{
    private static readonly string[] ViewPresets = ["Free", "Top", "Bottom", "Front", "Back", "Right", "Left"];

    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var bar = ui.Panel("ViewportToolbar")
            .Horizontal()
            .Background(t.PanelBackground.WithAlpha(220))
            .Border(UiBorder.All(1, t.Border))
            .CornerRadius(t.RadiusMedium)
            .Padding(8, 4)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Gap(8)
            .Open();

        // Gizmo mode indicator
        EditorUi.Badge(ui, vm.GizmoModeText);

        Ui.VerticalDivider(ctx, t.Border);

        // Grid toggle
        if (EditorUi.ToolbarToggle(ctx, "GridToggle", FontAwesome.BorderAll, vm.GridStatusText, vm.ShowGrid))
        {
            vm.ToggleGrid();
        }

        // Snap toggle
        if (EditorUi.ToolbarToggle(ctx, "SnapToggle", FontAwesome.Magnet, vm.SnapStatusText, vm.SnapEnabled))
        {
            vm.ToggleSnap();
        }

        Ui.VerticalDivider(ctx, t.Border);

        // View preset dropdown
        var presetIndex = (int)vm.CurrentViewPreset;
        if (Ui.Dropdown(ctx, "ViewPreset", ViewPresets)
            .Background(t.PanelElevated, t.Hover)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(8, 4)
            .Width(90)
            .ItemHoverColor(t.Hover)
            .DropdownBackground(t.PanelBackground)
            .Show(ref presetIndex))
        {
            vm.SetViewPreset(ViewPresets[presetIndex]);
        }

        // 2D/3D toggle
        if (EditorUi.ToolbarToggle(ctx, "2DToggle", FontAwesome.Cube, vm.ProjectionModeText, vm.Is2DMode))
        {
            vm.Toggle2DMode();
        }

        // Spacer
        Ui.FlexSpacer(ctx);

        // FPS counter
        if (vm.ShowStatistics)
        {
            using (ui.Panel("FpsCounter")
                .Horizontal()
                .Gap(4)
                .FitWidth()
                .FitHeight()
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                ui.Text($"{vm.Fps:F0} FPS", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
                ui.Text($"({vm.FrameTime:F1}ms)", new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }

        // Stats toggle
        if (EditorUi.IconButton(ctx, "StatsToggle", FontAwesome.ChartBar))
        {
            vm.ToggleStatistics();
        }
    }
}
