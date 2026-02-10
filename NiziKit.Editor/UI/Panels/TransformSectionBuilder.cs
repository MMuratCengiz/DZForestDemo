using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class TransformSectionBuilder
{
    public static void Build(UiFrame ui, UiContext ctx, GameObjectViewModel obj)
    {
        var t = EditorTheme.Current;

        using var section = Ui.CollapsibleSection(ctx, "TransformSection", "Transform", true)
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

        using var grid = Ui.PropertyGrid(ctx, "TransformGrid")
            .LabelWidth(45)
            .FontSize(t.FontSizeCaption)
            .RowHeight(24)
            .Gap(2)
            .LabelColor(t.TextSecondary)
            .Open();

        {
            using var row = grid.Row("Position");
            var px = obj.PositionX;
            var py = obj.PositionY;
            var pz = obj.PositionZ;

            if (Ui.Vec3Editor(ctx, "Pos", ref px, ref py, ref pz, 0.1f, "F2",
                t.AxisX, t.AxisY, t.AxisZ, t.InputBackground, t.InputBackgroundFocused, t.InputText))
            {
                obj.PositionX = px;
                obj.PositionY = py;
                obj.PositionZ = pz;
                obj.Editor.MarkDirty();
            }
        }

        {
            using var row = grid.Row("Rotation");
            var rx = obj.RotationX;
            var ry = obj.RotationY;
            var rz = obj.RotationZ;

            if (Ui.Vec3Editor(ctx, "Rot", ref rx, ref ry, ref rz, 0.5f, "F2",
                t.AxisX, t.AxisY, t.AxisZ, t.InputBackground, t.InputBackgroundFocused, t.InputText))
            {
                obj.RotationX = rx;
                obj.RotationY = ry;
                obj.RotationZ = rz;
                obj.Editor.MarkDirty();
            }
        }

        {
            using var row = grid.Row("Scale");
            var sx = obj.ScaleX;
            var sy = obj.ScaleY;
            var sz = obj.ScaleZ;

            if (Ui.Vec3Editor(ctx, "Scl", ref sx, ref sy, ref sz, 0.01f, "F2",
                t.AxisX, t.AxisY, t.AxisZ, t.InputBackground, t.InputBackgroundFocused, t.InputText))
            {
                obj.ScaleX = sx;
                obj.ScaleY = sy;
                obj.ScaleZ = sz;
                obj.Editor.MarkDirty();
            }
        }
    }
}
