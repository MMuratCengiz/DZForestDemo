using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class InspectorBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var selected = vm.SelectedGameObject;

        if (selected == null)
        {
            BuildSceneSettings(ui, ctx, vm);
            return;
        }

        using var scroll = ui.Panel("InspectorScroll")
            .Vertical()
            .GrowWidth()
            .GrowHeight()
            .ScrollVertical()
            .Gap(6)
            .Padding(6, 0)
            .Open();

        // Object header
        BuildObjectHeader(ui, ctx, selected);

        // Transform section
        TransformSectionBuilder.Build(ui, ctx, selected);

        // Component sections
        for (var i = 0; i < selected.Components.Count; i++)
        {
            ComponentEditorBuilder.Build(ui, ctx, selected.Components[i], vm, i);
        }

        // Add Component button
        if (!selected.IsAddingComponent)
        {
            using (ui.Panel("AddCompBtnRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(12, 8)
                .AlignChildrenX(UiAlignX.Center)
                .Open())
            {
                if (EditorUi.AccentButton(ctx, "AddCompBtn", "+ Add Component"))
                {
                    selected.ToggleAddComponentPanel();
                }
            }
        }
        else
        {
            AddComponentBuilder.Build(ui, ctx, selected);
        }
    }

    private static void BuildObjectHeader(UiFrame ui, UiContext ctx, GameObjectViewModel obj)
    {
        var t = EditorTheme.Current;

        using var header = ui.Panel("ObjHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(14, 10)
            .Gap(10)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open();

        // Type icon
        ui.Icon(obj.TypeIcon, obj.TypeIconColor, t.IconSizeBase);

        // Name text field
        var name = obj.Name;
        if (Ui.TextField(ctx, "ObjName", ref name)
            .BackgroundColor(t.SurfaceInset, t.PanelBackground)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeBody)
            .CornerRadius(t.RadiusSmall)
            .Padding(8, 6)
            .GrowWidth()
            .Show(ref name))
        {
            obj.Name = name;
            obj.Editor.MarkDirty();
        }

        // Active checkbox
        var isActive = obj.IsActive;
        var newActive = Ui.Checkbox(ctx, "ObjActive", "", isActive)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(16)
            .CornerRadius(t.RadiusSmall)
            .Show();

        if (newActive != isActive)
        {
            obj.IsActive = newActive;
            obj.Editor.MarkDirty();
        }
    }

    private static void BuildSceneSettings(UiFrame ui, UiContext ctx, EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var scroll = ui.Panel("SceneSettingsScroll")
            .Vertical()
            .GrowWidth()
            .GrowHeight()
            .ScrollVertical()
            .Gap(0)
            .Open();

        // Scene header
        using (ui.Panel("SceneHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(14, 10)
            .Gap(10)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            ui.Icon(FontAwesome.Film, t.Accent, t.IconSizeBase);
            ui.Text(vm.SceneDisplayName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeSubtitle });
        }

        // Grid settings section
        using var section = Ui.CollapsibleSection(ctx, "GridSettings", "Grid Settings", true)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeBody)
            .Padding(12)
            .Gap(8)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        using var grid = Ui.PropertyGrid(ctx, "GridSettingsGrid")
            .LabelWidth(75)
            .FontSize(t.FontSizeCaption)
            .RowHeight(28)
            .Gap(4)
            .LabelColor(t.TextSecondary)
            .Open();

        // Show Grid
        {
            using var row = grid.Row("Show Grid");
            var showGrid = vm.ShowGrid;
            var newShowGrid = Ui.Checkbox(ctx, "ShowGridCb", "", showGrid)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .Show();

            if (newShowGrid != showGrid)
            {
                vm.ShowGrid = newShowGrid;
                vm.NotifyGridSettingsChanged();
            }
        }

        // Grid Size
        {
            using var row = grid.Row("Grid Size");
            var gridSize = vm.GridSize;
            if (Ui.DraggableValue(ctx, "GridSizeVal")
                .Sensitivity(1f)
                .Format("F0")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref gridSize))
            {
                vm.GridSize = gridSize;
                vm.NotifyGridSettingsChanged();
            }
        }

        // Grid Spacing
        {
            using var row = grid.Row("Spacing");
            var spacing = vm.GridSpacing;
            if (Ui.DraggableValue(ctx, "GridSpacingVal")
                .Sensitivity(0.1f)
                .Format("F1")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref spacing))
            {
                vm.GridSpacing = spacing;
                vm.NotifyGridSettingsChanged();
            }
        }

        // Snap settings
        {
            using var row = grid.Row("Snap");
            var snapEnabled = vm.SnapEnabled;
            var newSnap = Ui.Checkbox(ctx, "SnapEnabledCb", "", snapEnabled)
                .BoxColor(t.SurfaceInset, t.Hover)
                .CheckColor(t.Accent)
                .BorderColor(t.Border)
                .BoxSize(14)
                .CornerRadius(t.RadiusSmall)
                .Show();

            if (newSnap != snapEnabled)
            {
                vm.SnapEnabled = newSnap;
                vm.SyncSnapToGrid();
            }
        }

        // Position Snap Increment
        {
            using var row = grid.Row("Pos Snap");
            var posSnap = vm.PositionSnapIncrement;
            if (Ui.DraggableValue(ctx, "PosSnapVal")
                .Sensitivity(0.1f)
                .Format("F1")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref posSnap))
            {
                vm.PositionSnapIncrement = posSnap;
                vm.SyncSnapToGrid();
            }
        }

        // Rotation Snap Increment
        {
            using var row = grid.Row("Rot Snap");
            var rotSnap = vm.RotationSnapIncrement;
            if (Ui.DraggableValue(ctx, "RotSnapVal")
                .Sensitivity(1f)
                .Format("F0")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref rotSnap))
            {
                vm.RotationSnapIncrement = rotSnap;
                vm.SyncSnapToGrid();
            }
        }

        // Auto-save status
        if (!string.IsNullOrEmpty(vm.AutoSaveStatus))
        {
            using (ui.Panel("AutoSaveRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(12, 8)
                .Open())
            {
                ui.Text(vm.AutoSaveStatus, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }
    }
}
