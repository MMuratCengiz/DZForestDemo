using System.Runtime.CompilerServices;

namespace NiziKit.UI;

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(UiContext ctx, string id, ref float x, ref float y,
        float sensitivity = 0.5f, string format = "F2")
        => Vec2Editor(ctx, id, ref x, ref y, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(UiContext ctx, string id, ref float x, ref float y,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(ctx, id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(ctx, id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(UiContext ctx, string id, ref float x, ref float y, ref float z,
        float sensitivity = 0.5f, string format = "F2")
        => Vec3Editor(ctx, id, ref x, ref y, ref z, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), UiColor.Rgb(70, 120, 220),
            null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec3Editor(UiContext ctx, string id, ref float x, ref float y, ref float z,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(ctx, id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(ctx, id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }

            var dvZ = DraggableValue(ctx, id + "_Z")
                .LabelWidth(0)
                .Prefix("Z", axisZ)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvZ = dvZ.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvZ = dvZ.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvZ = dvZ.ValueTextColor(valueText.Value);
            }

            if (dvZ.Show(ref z))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(UiContext ctx, string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity = 0.5f, string format = "F2")
        => Vec4Editor(ctx, id, ref x, ref y, ref z, ref w, sensitivity, format,
            UiColor.Rgb(200, 70, 70), UiColor.Rgb(70, 180, 70), UiColor.Rgb(70, 120, 220), UiColor.Rgb(200, 180, 50),
            null, null, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec4Editor(UiContext ctx, string id, ref float x, ref float y, ref float z, ref float w,
        float sensitivity, string format,
        UiColor axisX, UiColor axisY, UiColor axisZ, UiColor axisW,
        UiColor? valueBg, UiColor? valueEditBg, UiColor? valueText)
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.OpenElement(containerDecl);
        {
            var dvX = DraggableValue(ctx, id + "_X")
                .LabelWidth(0)
                .Prefix("X", axisX)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvX = dvX.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvX = dvX.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvX = dvX.ValueTextColor(valueText.Value);
            }

            if (dvX.Show(ref x))
            {
                changed = true;
            }

            var dvY = DraggableValue(ctx, id + "_Y")
                .LabelWidth(0)
                .Prefix("Y", axisY)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvY = dvY.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvY = dvY.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvY = dvY.ValueTextColor(valueText.Value);
            }

            if (dvY.Show(ref y))
            {
                changed = true;
            }

            var dvZ = DraggableValue(ctx, id + "_Z")
                .LabelWidth(0)
                .Prefix("Z", axisZ)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvZ = dvZ.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvZ = dvZ.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvZ = dvZ.ValueTextColor(valueText.Value);
            }

            if (dvZ.Show(ref z))
            {
                changed = true;
            }

            var dvW = DraggableValue(ctx, id + "_W")
                .LabelWidth(0)
                .Prefix("W", axisW)
                .Sensitivity(sensitivity)
                .Format(format);
            if (valueBg.HasValue)
            {
                dvW = dvW.ValueColor(valueBg.Value);
            }

            if (valueEditBg.HasValue)
            {
                dvW = dvW.ValueEditColor(valueEditBg.Value);
            }

            if (valueText.HasValue)
            {
                dvW = dvW.ValueTextColor(valueText.Value);
            }

            if (dvW.Show(ref w))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }
}
