using NiziKit.Editor.Services;
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
            .Gap(4)
            .Padding(4, 0)
            .Open();

        BuildObjectHeader(ui, ctx, selected);

        TransformSectionBuilder.Build(ui, ctx, selected);

        for (var i = 0; i < selected.Components.Count; i++)
        {
            ComponentEditorBuilder.Build(ui, ctx, selected.Components[i], vm, i);
        }

        if (!selected.IsAddingComponent)
        {
            using (ui.Panel("AddCompBtnRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(8, 6)
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
            .Padding(8, 6)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open();

        ui.Icon(obj.TypeIcon, obj.TypeIconColor, t.IconSizeSmall);

        var oldName = obj.Name;
        var name = oldName;
        if (Ui.TextField(ctx, "ObjName", ref name)
            .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeBody)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .GrowWidth()
            .Show(ref name))
        {
            obj.Name = name;
            obj.Editor.MarkDirty();
            obj.Editor.UndoSystem.Execute(
                new NameChangeAction(obj, oldName, name),
                $"Name_{obj.GameObject.GetHashCode()}");
        }

        var isActive = obj.IsActive;
        var newActive = Ui.Checkbox(ctx, "ObjActive", "", isActive)
            .BoxColor(t.SurfaceInset, t.Hover)
            .CheckColor(t.Accent)
            .BorderColor(t.Border)
            .BoxSize(14)
            .CornerRadius(t.RadiusSmall)
            .Show();

        if (newActive != isActive)
        {
            obj.IsActive = newActive;
            obj.Editor.MarkDirty();
            obj.Editor.UndoSystem.Execute(new ActiveToggleAction(obj, isActive, newActive));
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

        using (ui.Panel("SceneHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(8, 6)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            ui.Icon(FontAwesome.Film, t.Accent, t.IconSizeSmall);
            ui.Text(vm.SceneDisplayName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });
        }

        using var section = Ui.CollapsibleSection(ctx, "GridSettings", "Grid Settings", true)
            .HeaderBackground(t.SectionHeaderBg, t.Hover)
            .HeaderTextColor(t.TextPrimary)
            .BodyBackground(t.PanelBackground)
            .ChevronColor(t.TextMuted)
            .FontSize(t.FontSizeBody)
            .Padding(8)
            .Gap(4)
            .Open();

        if (!section.IsExpanded)
        {
            return;
        }

        using var grid = Ui.PropertyGrid(ctx, "GridSettingsGrid")
            .LabelWidth(65)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

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

        {
            using var row = grid.Row("Grid Size");
            var gridSize = vm.GridSize;
            if (Ui.DraggableValue(ctx, "GridSizeVal")
                .LabelWidth(0)
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

        {
            using var row = grid.Row("Spacing");
            var spacing = vm.GridSpacing;
            if (Ui.DraggableValue(ctx, "GridSpacingVal")
                .LabelWidth(0)
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

        {
            using var row = grid.Row("Pos Snap");
            var posSnap = vm.PositionSnapIncrement;
            if (Ui.DraggableValue(ctx, "PosSnapVal")
                .LabelWidth(0)
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

        {
            using var row = grid.Row("Rot Snap");
            var rotSnap = vm.RotationSnapIncrement;
            if (Ui.DraggableValue(ctx, "RotSnapVal")
                .LabelWidth(0)
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

        if (!string.IsNullOrEmpty(vm.AutoSaveStatus))
        {
            using (ui.Panel("AutoSaveRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(8, 4)
                .Open())
            {
                ui.Text(vm.AutoSaveStatus, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }
    }
}
