using System.Runtime.CompilerServices;

namespace NiziKit.UI;

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Vec2Editor(UiContext ctx, string id, ref float x, ref float y,
        float sensitivity = 0.5f, string format = "F2")
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.Clay.OpenElement(containerDecl);
        {
            if (DraggableValue(ctx, id + "_X")
                    .Label("X")
                    .LabelColor(UiColor.Rgb(200, 70, 70))
                    .Sensitivity(sensitivity)
                    .Format(format)
                    .Show(ref x))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_Y")
                .Label("Y")
                .LabelColor(UiColor.Rgb(70, 180, 70))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref y))
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
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.Clay.OpenElement(containerDecl);
        {
            if (DraggableValue(ctx, id + "_X")
                    .Label("X")
                    .LabelColor(UiColor.Rgb(200, 70, 70))
                    .Sensitivity(sensitivity)
                    .Format(format)
                    .Show(ref x))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_Y")
                .Label("Y")
                .LabelColor(UiColor.Rgb(70, 180, 70))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref y))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_Z")
                .Label("Z")
                .LabelColor(UiColor.Rgb(70, 120, 220))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref z))
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
    {
        var changed = false;

        var containerId = ctx.StringCache.GetId(id);
        var containerDecl = new DenOfIz.ClayElementDeclaration { Id = containerId };
        containerDecl.Layout.LayoutDirection = DenOfIz.ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = DenOfIz.ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = DenOfIz.ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;

        ctx.Clay.OpenElement(containerDecl);
        {
            if (DraggableValue(ctx, id + "_X")
                    .Label("X")
                    .LabelColor(UiColor.Rgb(200, 70, 70))
                    .Sensitivity(sensitivity)
                    .Format(format)
                    .Show(ref x))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_Y")
                .Label("Y")
                .LabelColor(UiColor.Rgb(70, 180, 70))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref y))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_Z")
                .Label("Z")
                .LabelColor(UiColor.Rgb(70, 120, 220))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref z))
            {
                changed = true;
            }

            if (DraggableValue(ctx, id + "_W")
                .Label("W")
                .LabelColor(UiColor.Rgb(200, 180, 50))
                .Sensitivity(sensitivity)
                .Format(format)
                .Show(ref w))
            {
                changed = true;
            }
        }
        ctx.Clay.CloseElement();

        return changed;
    }
}
