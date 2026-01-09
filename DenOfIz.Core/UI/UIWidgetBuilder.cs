using System.Runtime.CompilerServices;

namespace DenOfIz.World.UI;

public struct WidgetStyle
{
    public UiColor BackgroundColor;
    public UiColor HoverColor;
    public UiColor PressedColor;
    public UiColor DisabledColor;
    public UiColor TextColor;
    public UiColor BorderColor;
    public UiColor FocusedBorderColor;
    public float BorderWidth;
    public float CornerRadius;
    public UiPadding Padding;
    public ushort FontSize;

    public static WidgetStyle Default => new()
    {
        BackgroundColor = UiColor.Rgb(45, 45, 48),
        HoverColor = UiColor.Rgb(55, 55, 60),
        PressedColor = UiColor.Rgb(35, 35, 38),
        DisabledColor = UiColor.Rgb(35, 35, 38),
        TextColor = UiColor.White,
        BorderColor = UiColor.Rgb(70, 70, 75),
        FocusedBorderColor = UiColor.Rgb(100, 149, 237),
        BorderWidth = 1,
        CornerRadius = 4,
        Padding = UiPadding.Symmetric(8, 6),
        FontSize = 14
    };

    public static WidgetStyle Primary => Default with
    {
        BackgroundColor = UiColor.Rgb(60, 130, 200),
        HoverColor = UiColor.Rgb(70, 140, 210),
        PressedColor = UiColor.Rgb(50, 120, 190)
    };

    public static WidgetStyle Subtle => Default with
    {
        BackgroundColor = UiColor.Transparent,
        HoverColor = UiColor.Rgba(255, 255, 255, 20),
        PressedColor = UiColor.Rgba(255, 255, 255, 10),
        BorderWidth = 0
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UiColor GetBackgroundColor(bool isHovered, bool isPressed, bool isDisabled)
    {
        if (isDisabled)
        {
            return DisabledColor;
        }

        if (isPressed)
        {
            return PressedColor;
        }

        if (isHovered)
        {
            return HoverColor;
        }

        return BackgroundColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UiColor GetBorderColor(bool isFocused)
    {
        return isFocused ? FocusedBorderColor : BorderColor;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public ref struct WidgetBuilder(UiContext ctx, uint id)
{
    private ClayElementDeclaration _decl = new() { Id = id };
    private WidgetStyle _style = WidgetStyle.Default;
    private UiSizing _width = UiSizing.Fit();
    private UiSizing _height = UiSizing.Fit();
    private UiDirection _direction = UiDirection.Vertical;
    private float _gap = 0;
    private UiAlignX _alignX = UiAlignX.Left;
    private UiAlignY _alignY = UiAlignY.Top;
    private bool _isDisabled = false;

    public uint Id => _decl.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Style(WidgetStyle style)
    {
        _style = style;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Background(UiColor color)
    {
        _style.BackgroundColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Background(UiColor normal, UiColor hover, UiColor pressed)
    {
        _style.BackgroundColor = normal;
        _style.HoverColor = hover;
        _style.PressedColor = pressed;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder TextColor(UiColor color)
    {
        _style.TextColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Border(float width, UiColor color)
    {
        _style.BorderWidth = width;
        _style.BorderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Border(float width, UiColor normal, UiColor focused)
    {
        _style.BorderWidth = width;
        _style.BorderColor = normal;
        _style.FocusedBorderColor = focused;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder CornerRadius(float radius)
    {
        _style.CornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder FontSize(ushort size)
    {
        _style.FontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Padding(float all)
    {
        _style.Padding = UiPadding.All(all);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Padding(float h, float v)
    {
        _style.Padding = UiPadding.Symmetric(h, v);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Padding(UiPadding padding)
    {
        _style.Padding = padding;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Width(float w)
    {
        _width = UiSizing.Fixed(w);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Width(UiSizing sizing)
    {
        _width = sizing;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Height(float h)
    {
        _height = UiSizing.Fixed(h);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Height(UiSizing sizing)
    {
        _height = sizing;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Size(float w, float h)
    {
        _width = UiSizing.Fixed(w);
        _height = UiSizing.Fixed(h);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder GrowWidth()
    {
        _width = UiSizing.Grow();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder GrowHeight()
    {
        _height = UiSizing.Grow();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Grow()
    {
        _width = UiSizing.Grow();
        _height = UiSizing.Grow();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Direction(UiDirection dir)
    {
        _direction = dir;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Horizontal()
    {
        _direction = UiDirection.Horizontal;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Vertical()
    {
        _direction = UiDirection.Vertical;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Gap(float gap)
    {
        _gap = gap;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Align(UiAlignX x, UiAlignY y)
    {
        _alignX = x;
        _alignY = y;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Center()
    {
        _alignX = UiAlignX.Center;
        _alignY = UiAlignY.Center;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WidgetBuilder Disabled(bool disabled = true)
    {
        _isDisabled = disabled;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiInteraction GetInteraction()
    {
        if (_isDisabled)
        {
            return UiInteraction.None;
        }

        return ctx.GetInteraction(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasClicked()
    {
        if (_isDisabled)
        {
            return false;
        }

        return GetInteraction().WasClicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClayElementDeclaration Build(bool isFocused = false)
    {
        var interaction = _isDisabled ? UiInteraction.None : ctx.GetInteraction(Id);
        var bgColor = _style.GetBackgroundColor(interaction.IsHovered, interaction.IsPressed, _isDisabled);
        var borderColor = _style.GetBorderColor(isFocused);

        _decl.Layout.LayoutDirection = _direction == UiDirection.Horizontal
            ? ClayLayoutDirection.LeftToRight
            : ClayLayoutDirection.TopToBottom;
        _decl.Layout.Sizing.Width = _width.ToClayAxis();
        _decl.Layout.Sizing.Height = _height.ToClayAxis();
        _decl.Layout.Padding = _style.Padding.ToClayPadding();
        _decl.Layout.ChildGap = (ushort)_gap;
        _decl.Layout.ChildAlignment.X = _alignX switch
        {
            UiAlignX.Center => ClayAlignmentX.Center,
            UiAlignX.Right => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
        _decl.Layout.ChildAlignment.Y = _alignY switch
        {
            UiAlignY.Center => ClayAlignmentY.Center,
            UiAlignY.Bottom => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
        _decl.BackgroundColor = bgColor.ToClayColor();
        _decl.BorderRadius = ClayBorderRadius.CreateUniform(_style.CornerRadius);

        if (_style.BorderWidth > 0)
        {
            _decl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_style.BorderWidth),
                Color = borderColor.ToClayColor()
            };
        }

        return _decl;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElementScope Open(bool isFocused = false)
    {
        var decl = Build(isFocused);
        ctx.Clay.OpenElement(decl);
        return new UiElementScope(ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(string text)
    {
        var desc = new ClayTextDesc
        {
            TextColor = (_isDisabled ? _style.DisabledColor : _style.TextColor).ToClayColor(),
            FontSize = _style.FontSize
        };
        ctx.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(string text, UiColor color)
    {
        var desc = new ClayTextDesc
        {
            TextColor = color.ToClayColor(),
            FontSize = _style.FontSize
        };
        ctx.Clay.Text(StringView.Intern(text), desc);
    }

    public WidgetStyle CurrentStyle => _style;
    public UiContext Context => ctx;
}

public static class WidgetBuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WidgetBuilder Widget(this UiContext ctx, string id)
    {
        return new WidgetBuilder(ctx, ctx.StringCache.GetId(id));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WidgetBuilder Widget(this UiContext ctx, string id, uint index)
    {
        return new WidgetBuilder(ctx, ctx.StringCache.GetId(id, index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderText(this UiContext ctx, string text, WidgetStyle style)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.TextColor.ToClayColor(),
            FontSize = style.FontSize
        };
        ctx.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderText(this UiContext ctx, string text, UiColor color, ushort fontSize = 14)
    {
        var desc = new ClayTextDesc
        {
            TextColor = color.ToClayColor(),
            FontSize = fontSize
        };
        ctx.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClayElementDeclaration SimpleBox(this UiContext ctx, uint id, float width, float height, UiColor color)
    {
        var decl = new ClayElementDeclaration { Id = id };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        decl.BackgroundColor = color.ToClayColor();
        return decl;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderBox(this UiContext ctx, uint id, float width, float height, UiColor color)
    {
        var decl = ctx.SimpleBox(id, width, height, color);
        ctx.Clay.OpenElement(decl);
        ctx.Clay.CloseElement();
    }
}
