using System.Numerics;
using NiziKit.Assets;
using NiziKit.Assets.Pack;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Services;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class InspectorBuilder
{
    public static void Build(EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var selected = vm.SelectedGameObject;

        if (selected == null)
        {
            BuildSceneSettings(vm);
            return;
        }

        using var scroll = NiziUi.Panel("InspectorScroll")
            .Vertical()
            .GrowWidth()
            .GrowHeight()
            .ScrollVertical()
            .Gap(2)
            .Open();

        BuildObjectHeader(selected);

        TransformSectionBuilder.Build(selected);

        for (var i = 0; i < selected.Components.Count; i++)
        {
            ComponentEditorBuilder.Build(selected.Components[i], vm, i);
        }

        if (!selected.IsAddingComponent)
        {
            using (NiziUi.Panel("AddCompBtnRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(8, 12)
                .AlignChildrenX(UiAlignX.Center)
                .Open())
            {
                if (EditorUi.AccentButton("AddCompBtn", "+ Add Component"))
                {
                    selected.ToggleAddComponentPanel();
                }
            }
        }
        else
        {
            AddComponentBuilder.Build(selected);
        }
    }

    private static void BuildObjectHeader(GameObjectViewModel obj)
    {
        var t = EditorTheme.Current;

        using var header = NiziUi.Panel("ObjHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                with { Bottom = 1, Color = t.Border })
            .Open();

        NiziUi.Icon(obj.TypeIcon, obj.TypeIconColor, t.IconSizeSmall);

        var oldName = obj.Name;
        var name = oldName;
        if (NiziUi.TextField("ObjName", ref name)
            .BackgroundColor(t.InputBackground, t.InputBackgroundFocused)
            .TextColor(t.TextPrimary)
            .BorderColor(t.Border, t.Accent)
            .FontSize(t.FontSizeBody)
            .CornerRadius(t.RadiusSmall)
            .Padding(6, 4)
            .GrowWidth()
            .Show())
        {
            obj.Name = name;
            obj.Editor.MarkDirty();
            obj.Editor.UndoSystem.Execute(
                new NameChangeAction(obj, oldName, name),
                $"Name_{obj.GameObject.GetHashCode()}");
        }

        var isActive = obj.IsActive;
        var newActive = NiziUi.Checkbox("ObjActive", "", isActive)
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

    private static void BuildSceneSettings(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var scroll = NiziUi.Panel("SceneSettingsScroll")
            .Vertical()
            .GrowWidth()
            .GrowHeight()
            .ScrollVertical()
            .Gap(0)
            .Open();

        using (NiziUi.Panel("SceneHeader")
            .Horizontal()
            .Background(t.PanelElevated)
            .Padding(12, 8)
            .Gap(8)
            .GrowWidth()
            .FitHeight()
            .AlignChildrenY(UiAlignY.Center)
            .Border(UiBorder.Horizontal(0, UiColor.Transparent)
                with { Bottom = 1, Color = t.Border })
            .Open())
        {
            NiziUi.Icon(FontAwesome.Film, t.Accent, t.IconSizeSmall);
            NiziUi.Text(vm.SceneDisplayName, new UiTextStyle { Color = t.TextPrimary, FontSize = t.FontSizeBody });
        }

        BuildGridSettings(vm);
        BuildAmbientLightSettings(vm);
        BuildSkyboxSettings(vm);
    }

    private static void BuildGridSettings(EditorViewModel vm)
    {
        var t = EditorTheme.Current;

        using var section = NiziUi.CollapsibleSection("GridSettings", "Grid Settings", true)
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

        using var grid = NiziUi.PropertyGrid("GridSettingsGrid")
            .LabelWidth(75)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        {
            using var row = grid.Row("Show Grid");
            var showGrid = vm.ShowGrid;
            var newShowGrid = NiziUi.Checkbox("ShowGridCb", "", showGrid)
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
            if (NiziUi.DraggableValue("GridSizeVal")
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
            if (NiziUi.DraggableValue("GridSpacingVal")
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
            var newSnap = NiziUi.Checkbox("SnapEnabledCb", "", snapEnabled)
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
            if (NiziUi.DraggableValue("PosSnapVal")
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
            if (NiziUi.DraggableValue("RotSnapVal")
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
            using (NiziUi.Panel("AutoSaveRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Padding(8, 4)
                .Open())
            {
                NiziUi.Text(vm.AutoSaveStatus, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
            }
        }
    }

    private static void BuildAmbientLightSettings(EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var scene = World.CurrentScene;
        using var section = NiziUi.CollapsibleSection("AmbientLightSettings", "Ambient Light", false)
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

        using var grid = NiziUi.PropertyGrid("AmbientLightGrid")
            .LabelWidth(75)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        {
            using var row = grid.Row("Sky Color");
            var color = scene.AmbientSkyColor;
            var r = color.X;
            var g = color.Y;
            var b = color.Z;

            if (NiziUi.ColorPicker("AmbientSkyColor")
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .BorderColor(t.Border)
                .PanelBackground(t.PanelBackground)
                .LabelColor(t.TextSecondary)
                .ValueTextColor(t.TextMuted)
                .GrowWidth()
                .Show(ref r, ref g, ref b))
            {
                scene.AmbientSkyColor = new Vector3(r, g, b);
                vm.MarkDirty();
            }
        }

        {
            using var row = grid.Row("Ground");
            var color = scene.AmbientGroundColor;
            var r = color.X;
            var g = color.Y;
            var b = color.Z;

            if (NiziUi.ColorPicker("AmbientGroundColor")
                .FontSize(t.FontSizeCaption)
                .CornerRadius(t.RadiusSmall)
                .BorderColor(t.Border)
                .PanelBackground(t.PanelBackground)
                .LabelColor(t.TextSecondary)
                .ValueTextColor(t.TextMuted)
                .GrowWidth()
                .Show(ref r, ref g, ref b))
            {
                scene.AmbientGroundColor = new Vector3(r, g, b);
                vm.MarkDirty();
            }
        }

        {
            using var row = grid.Row("Intensity");
            var intensity = scene.AmbientIntensity;
            if (NiziUi.DraggableValue("AmbientIntensityVal")
                .LabelWidth(0)
                .Sensitivity(0.01f)
                .Format("F2")
                .FontSize(t.FontSizeCaption)
                .Width(UiSizing.Grow())
                .ValueColor(t.InputBackground)
                .ValueTextColor(t.InputText)
                .Show(ref intensity))
            {
                scene.AmbientIntensity = intensity;
                vm.MarkDirty();
            }
        }
    }

    private static void BuildSkyboxSettings(EditorViewModel vm)
    {
        var t = EditorTheme.Current;
        var scene = World.CurrentScene;
        using var section = NiziUi.CollapsibleSection("SkyboxSettings", "Skybox Settings", false)
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

        scene.Skybox ??= new SkyboxData();

        using var grid = NiziUi.PropertyGrid("SkyboxGrid")
            .LabelWidth(65)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        RenderSkyboxFaceEditor(grid, "Right", scene.Skybox.RightRef, vm, (tex, path) =>
        {
            scene.Skybox.Right = tex;
            scene.Skybox.RightRef = path;
        });

        RenderSkyboxFaceEditor(grid, "Left", scene.Skybox.LeftRef, vm, (tex, path) =>
        {
            scene.Skybox.Left = tex;
            scene.Skybox.LeftRef = path;
        });

        RenderSkyboxFaceEditor(grid, "Up", scene.Skybox.UpRef, vm, (tex, path) =>
        {
            scene.Skybox.Up = tex;
            scene.Skybox.UpRef = path;
        });

        RenderSkyboxFaceEditor(grid, "Down", scene.Skybox.DownRef, vm, (tex, path) =>
        {
            scene.Skybox.Down = tex;
            scene.Skybox.DownRef = path;
        });

        RenderSkyboxFaceEditor(grid, "Front", scene.Skybox.FrontRef, vm, (tex, path) =>
        {
            scene.Skybox.Front = tex;
            scene.Skybox.FrontRef = path;
        });

        RenderSkyboxFaceEditor(grid, "Back", scene.Skybox.BackRef, vm, (tex, path) =>
        {
            scene.Skybox.Back = tex;
            scene.Skybox.BackRef = path;
        });
    }

    private static void RenderSkyboxFaceEditor(UiPropertyGridScope grid,
        string label, string? currentPath, EditorViewModel vm, Action<Texture2d?, string?> onChanged)
    {
        var t = EditorTheme.Current;

        using var row = grid.Row(label);

        var displayText = string.IsNullOrEmpty(currentPath) ? "(none)" : Path.GetFileName(currentPath);

        if (NiziUi.Button($"Skybox{label}Btn", displayText)
            .Color(t.SurfaceInset, t.Hover, t.Active)
            .TextColor(t.TextPrimary)
            .FontSize(t.FontSizeCaption)
            .CornerRadius(t.RadiusSmall)
            .Padding(4, 3)
            .GrowWidth()
            .Show())
        {
            vm.OpenAssetPicker(AssetRefType.Texture, currentPath, asset =>
            {
                if (asset != null)
                {
                    var texture = AssetPacks.GetTextureByPath(asset.Path);
                    onChanged(texture, asset.Path);
                    vm.MarkDirty();
                }
            });
        }
    }
}
