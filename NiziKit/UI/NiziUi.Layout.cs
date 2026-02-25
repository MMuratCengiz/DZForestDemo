using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public static partial class NiziUi
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiElement Root(string id = "Root")
    {
        return new UiElement(_ctx, id)
            .Width(UiSizing.Grow())
            .Height(UiSizing.Grow());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiElement Panel(string id)
    {
        return new UiElement(_ctx, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiElement Panel(string id, uint index)
    {
        return new UiElement(_ctx, id, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiElement Row(string id = "Row")
    {
        return new UiElement(_ctx, id).Horizontal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiElement Column(string id = "Column")
    {
        return new UiElement(_ctx, id).Vertical();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(string text, UiTextStyle style = default)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = style.FontId,
            WrapMode = ClayTextWrapMode.None,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _ctx.Clay.Text(text, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Image(Texture texture, uint width, uint height)
    {
        _ctx.Clay.Texture(texture, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Icon(string icon, UiColor color, ushort size = 14)
    {
        var desc = new ClayTextDesc
        {
            TextColor = color.ToClayColor(),
            FontSize = size,
            FontId = FontAwesome.FontId,
            WrapMode = ClayTextWrapMode.None,
            TextAlignment = ClayTextAlignment.Center
        };
        _ctx.Clay.Text(icon, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Icon(string icon, UiTextStyle style)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = FontAwesome.FontId,
            WrapMode = ClayTextWrapMode.None,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _ctx.Clay.Text(icon, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Spacer(float size)
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("Spacer", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(size);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(size);
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HorizontalSpacer(float width)
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("HSpacer", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VerticalSpacer(float height)
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("VSpacer", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlexSpacer()
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("FlexSpacer", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Divider(UiColor color, float thickness = 1)
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("Divider", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(thickness);
        decl.BackgroundColor = color.ToClayColor();
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VerticalDivider(UiColor color, float thickness = 1)
    {
        var decl = new ClayElementDeclaration
        {
            Id = _ctx.StringCache.GetId("VDivider", _ctx.NextElementIndex())
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(thickness);
        decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.BackgroundColor = color.ToClayColor();
        _ctx.OpenElement(decl);
        _ctx.Clay.CloseElement();
    }
}
