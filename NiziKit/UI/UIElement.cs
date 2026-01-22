using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public ref struct UiElement
{
    private readonly UiContext _context;
    private ClayElementDeclaration _decl;

    internal UiElement(UiContext context, string name)
    {
        _context = context;
        Id = context.StringCache.GetId(name);
        _decl = new ClayElementDeclaration { Id = Id };
    }

    internal UiElement(UiContext context, string name, uint index)
    {
        _context = context;
        Id = context.StringCache.GetId(name, index);
        _decl = new ClayElementDeclaration { Id = Id };
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Direction(UiDirection dir)
    {
        _decl.Layout.LayoutDirection = dir == UiDirection.Horizontal
            ? ClayLayoutDirection.LeftToRight
            : ClayLayoutDirection.TopToBottom;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Horizontal()
    {
        return Direction(UiDirection.Horizontal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Vertical()
    {
        return Direction(UiDirection.Vertical);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Width(UiSizing sizing)
    {
        _decl.Layout.Sizing.Width = sizing.ToClayAxis();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Width(float width)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Height(UiSizing sizing)
    {
        _decl.Layout.Sizing.Height = sizing.ToClayAxis();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Height(float height)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Size(UiSizing width, UiSizing height)
    {
        _decl.Layout.Sizing.Width = width.ToClayAxis();
        _decl.Layout.Sizing.Height = height.ToClayAxis();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Size(float width, float height)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement FixedWidth(float width)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement FixedHeight(float height)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement GrowWidth(float min = 0, float max = float.MaxValue)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(min, max);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement GrowHeight(float min = 0, float max = float.MaxValue)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(min, max);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Grow()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement FitWidth(float min = 0, float max = float.MaxValue)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(min, max);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement FitHeight(float min = 0, float max = float.MaxValue)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(min, max);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Fit()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Padding(UiPadding padding)
    {
        _decl.Layout.Padding = padding.ToClayPadding();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Padding(float all)
    {
        _decl.Layout.Padding = ClayPadding.CreateUniform((ushort)all);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Padding(float horizontal, float vertical)
    {
        _decl.Layout.Padding = new ClayPadding
        {
            Left = (ushort)horizontal,
            Right = (ushort)horizontal,
            Top = (ushort)vertical,
            Bottom = (ushort)vertical
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Padding(float left, float right, float top, float bottom)
    {
        _decl.Layout.Padding = new ClayPadding
        {
            Left = (ushort)left,
            Right = (ushort)right,
            Top = (ushort)top,
            Bottom = (ushort)bottom
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement AlignChildren(UiAlignX x, UiAlignY y)
    {
        _decl.Layout.ChildAlignment.X = x switch
        {
            UiAlignX.Center => ClayAlignmentX.Center,
            UiAlignX.Right => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
        _decl.Layout.ChildAlignment.Y = y switch
        {
            UiAlignY.Center => ClayAlignmentY.Center,
            UiAlignY.Bottom => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement CenterChildren()
    {
        _decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        _decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement AlignChildrenX(UiAlignX x)
    {
        _decl.Layout.ChildAlignment.X = x switch
        {
            UiAlignX.Center => ClayAlignmentX.Center,
            UiAlignX.Right => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement AlignChildrenY(UiAlignY y)
    {
        _decl.Layout.ChildAlignment.Y = y switch
        {
            UiAlignY.Center => ClayAlignmentY.Center,
            UiAlignY.Bottom => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Gap(float gap)
    {
        _decl.Layout.ChildGap = (ushort)gap;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Background(UiColor color)
    {
        _decl.BackgroundColor = color.ToClayColor();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Background(byte r, byte g, byte b, byte a = 255)
    {
        _decl.BackgroundColor = ClayColor.Create(r, g, b, a);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Border(UiBorder border)
    {
        _decl.Border = border.ToClayBorder();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Border(float width, UiColor color)
    {
        _decl.Border = new ClayBorderDesc
        {
            Width = ClayBorderWidth.CreateUniform((uint)width),
            Color = color.ToClayColor()
        };
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement CornerRadius(UiCornerRadius radius)
    {
        _decl.BorderRadius = radius.ToClayBorderRadius();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement CornerRadius(float radius)
    {
        _decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement ScrollVertical()
    {
        _decl.Scroll.Vertical = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement ScrollHorizontal()
    {
        _decl.Scroll.Horizontal = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Scroll(bool horizontal = false, bool vertical = true)
    {
        _decl.Scroll.Horizontal = horizontal;
        _decl.Scroll.Vertical = vertical;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiInteraction GetInteraction()
    {
        return _context.GetInteraction(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHovered()
    {
        return _context.IsHovered(Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasClicked()
    {
        return _context.IsHovered(Id) && _context.MouseJustReleased;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElementScope Open()
    {
        _context.Clay.OpenElement(_decl);
        return new UiElementScope(_context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Begin()
    {
        _context.Clay.OpenElement(_decl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Content(Action content)
    {
        _context.Clay.OpenElement(_decl);
        content();
        _context.Clay.CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id)
    {
        return new UiElement(_context, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Row(string id = "Row")
    {
        return new UiElement(_context, id).Horizontal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Column(string id = "Column")
    {
        return new UiElement(_context, id).Vertical();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(string text, UiTextStyle style = default)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = style.FontId,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _context.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Image(Texture texture, uint width, uint height)
    {
        _context.Clay.Texture(texture, width, height);
    }
}

public readonly ref struct UiElementScope
{
    private readonly UiContext _context;

    internal UiElementScope(UiContext context)
    {
        _context = context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id)
    {
        return new UiElement(_context, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id, uint index)
    {
        return new UiElement(_context, id, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Row(string id = "Row")
    {
        return new UiElement(_context, id).Horizontal();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Column(string id = "Column")
    {
        return new UiElement(_context, id).Vertical();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(string text, UiTextStyle style = default)
    {
        var desc = new ClayTextDesc
        {
            TextColor = style.Color.ToClayColor(),
            FontSize = style.FontSize > 0 ? style.FontSize : (ushort)14,
            FontId = style.FontId,
            TextAlignment = style.Alignment switch
            {
                UiTextAlign.Center => ClayTextAlignment.Center,
                UiTextAlign.Right => ClayTextAlignment.Right,
                _ => ClayTextAlignment.Left
            }
        };
        _context.Clay.Text(StringView.Intern(text), desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Image(Texture texture, uint width, uint height)
    {
        _context.Clay.Texture(texture, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _context.Clay.CloseElement();
    }
}
