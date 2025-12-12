using System.Runtime.CompilerServices;
using DenOfIz;

namespace UIFramework;

/// <summary>
/// High-level UI components built on top of UIElement.
/// </summary>
public static class Ui
{
    /// <summary>
    /// Creates a button with built-in hover state and click detection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiButton Button(UiContext ctx, string id, string text) => new(ctx, id, text);

    /// <summary>
    /// Creates a card container with rounded corners and optional shadow.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCard Card(UiContext ctx, string id) => new(ctx, id);

    /// <summary>
    /// Creates a spacer element for adding gaps.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Spacer(UiContext ctx, float size)
    {
        var decl = new ClayElementDeclaration
        {
            Id = ctx.StringCache.GetId("Spacer")
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(size);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(size);
        ctx.Clay.OpenElement(decl);
        ctx.Clay.CloseElement();
    }

    /// <summary>
    /// Creates a flexible spacer that grows to fill available space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlexSpacer(UiContext ctx)
    {
        var decl = new ClayElementDeclaration
        {
            Id = ctx.StringCache.GetId("FlexSpacer")
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        ctx.Clay.OpenElement(decl);
        ctx.Clay.CloseElement();
    }

    /// <summary>
    /// Creates a horizontal divider line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Divider(UiContext ctx, UiColor color, float thickness = 1)
    {
        var decl = new ClayElementDeclaration
        {
            Id = ctx.StringCache.GetId("Divider")
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(thickness);
        decl.BackgroundColor = color.ToClayColor();
        ctx.Clay.OpenElement(decl);
        ctx.Clay.CloseElement();
    }

    /// <summary>
    /// Creates a vertical divider line.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VerticalDivider(UiContext ctx, UiColor color, float thickness = 1)
    {
        var decl = new ClayElementDeclaration
        {
            Id = ctx.StringCache.GetId("VDivider")
        };
        decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(thickness);
        decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        decl.BackgroundColor = color.ToClayColor();
        ctx.Clay.OpenElement(decl);
        ctx.Clay.CloseElement();
    }
}

/// <summary>
/// Button component with hover/press states and click detection.
/// </summary>
public ref struct UiButton
{
    private readonly UiContext _context;
    private readonly string _text;
    private readonly uint _id;

    private UiColor _normalColor;
    private UiColor _hoverColor;
    private UiColor _pressedColor;
    private UiColor _textColor;
    private ushort _fontSize;
    private float _width;
    private float _height;
    private float _cornerRadius;
    private UiPadding _padding;
    private bool _fitContent;

    internal UiButton(UiContext ctx, string id, string text)
    {
        _context = ctx;
        _text = text;
        _id = ctx.StringCache.GetId(id);

        _normalColor = UiColor.Rgb(100, 149, 237); // Cornflower blue
        _hoverColor = UiColor.Rgb(80, 129, 217);
        _pressedColor = UiColor.Rgb(60, 109, 197);
        _textColor = UiColor.White;
        _fontSize = 14;
        _width = 120;
        _height = 40;
        _cornerRadius = 8;
        _padding = UiPadding.Symmetric(16, 8);
        _fitContent = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Color(UiColor normal, UiColor hover, UiColor pressed)
    {
        _normalColor = normal;
        _hoverColor = hover;
        _pressedColor = pressed;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Color(UiColor normal)
    {
        _normalColor = normal;
        _hoverColor = new UiColor(
            (byte)Math.Max(0, normal.R - 20),
            (byte)Math.Max(0, normal.G - 20),
            (byte)Math.Max(0, normal.B - 20),
            normal.A
        );
        _pressedColor = new UiColor(
            (byte)Math.Max(0, normal.R - 40),
            (byte)Math.Max(0, normal.G - 40),
            (byte)Math.Max(0, normal.B - 40),
            normal.A
        );
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton TextColor(UiColor color)
    {
        _textColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton FontSize(ushort size)
    {
        _fontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Size(float width, float height)
    {
        _width = width;
        _height = height;
        _fitContent = false;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton FitContent()
    {
        _fitContent = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton CornerRadius(float radius)
    {
        _cornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Padding(float horizontal, float vertical)
    {
        _padding = UiPadding.Symmetric(horizontal, vertical);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Padding(UiPadding padding)
    {
        _padding = padding;
        return this;
    }

    /// <summary>
    /// Renders the button and returns true if it was clicked.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Render()
    {
        var interaction = _context.GetInteraction(_id);

        var bgColor = interaction.IsPressed ? _pressedColor
            : interaction.IsHovered ? _hoverColor
            : _normalColor;

        var decl = new ClayElementDeclaration { Id = _id };
        decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.BackgroundColor = bgColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_fitContent)
        {
            decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
            decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        }
        else
        {
            decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_width);
            decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_height);
        }

        _context.Clay.OpenElement(decl);
        {
            var textDesc = new ClayTextDesc
            {
                TextColor = _textColor.ToClayColor(),
                FontSize = _fontSize,
                TextAlignment = ClayTextAlignment.Center
            };
            _context.Clay.Text(StringView.Intern(_text), textDesc);
        }
        _context.Clay.CloseElement();

        return interaction.WasClicked;
    }
}

/// <summary>
/// Card component with rounded corners and styling.
/// </summary>
public ref struct UiCard
{
    private readonly UiContext _context;
    private readonly uint _id;

    private UiColor _backgroundColor;
    private UiColor _borderColor;
    private float _borderWidth;
    private float _cornerRadius;
    private UiPadding _padding;
    private float _gap;
    private UiDirection _direction;
    private UiSizing _width;
    private UiSizing _height;

    internal UiCard(UiContext ctx, string id)
    {
        _context = ctx;
        _id = ctx.StringCache.GetId(id);

        // Defaults
        _backgroundColor = UiColor.White;
        _borderColor = UiColor.LightGray;
        _borderWidth = 1;
        _cornerRadius = 12;
        _padding = UiPadding.All(20);
        _gap = 12;
        _direction = UiDirection.Vertical;
        _width = UiSizing.Fit();
        _height = UiSizing.Fit();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Background(UiColor color)
    {
        _backgroundColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Border(UiColor color, float width = 1)
    {
        _borderColor = color;
        _borderWidth = width;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard NoBorder()
    {
        _borderWidth = 0;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard CornerRadius(float radius)
    {
        _cornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Padding(float all)
    {
        _padding = UiPadding.All(all);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Padding(UiPadding padding)
    {
        _padding = padding;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Gap(float gap)
    {
        _gap = gap;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Direction(UiDirection dir)
    {
        _direction = dir;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Horizontal()
    {
        _direction = UiDirection.Horizontal;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Vertical()
    {
        _direction = UiDirection.Vertical;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Width(UiSizing sizing)
    {
        _width = sizing;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard FixedWidth(float width)
    {
        _width = UiSizing.Fixed(width);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Height(UiSizing sizing)
    {
        _height = sizing;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard FixedHeight(float height)
    {
        _height = UiSizing.Fixed(height);
        return this;
    }

    /// <summary>
    /// Opens the card and returns a scope that closes it when disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElementScope Open()
    {
        var decl = new ClayElementDeclaration { Id = _id };
        decl.Layout.LayoutDirection = _direction == UiDirection.Horizontal
            ? ClayLayoutDirection.LeftToRight
            : ClayLayoutDirection.TopToBottom;
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.Layout.ChildGap = (ushort)_gap;
        decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        decl.Layout.Sizing.Width = _width.ToClayAxis();
        decl.Layout.Sizing.Height = _height.ToClayAxis();
        decl.BackgroundColor = _backgroundColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_borderWidth > 0)
        {
            decl.Border = new ClayBorderDesc()
            {
                Width = ClayBorderWidth.CreateUniform((uint)_borderWidth),
                Color = _borderColor.ToClayColor()
            };
        }

        _context.Clay.OpenElement(decl);
        return new UiElementScope(_context);
    }

    /// <summary>
    /// Opens the card, executes content, then closes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Content(Action content)
    {
        using var _ = Open();
        content();
    }
}

/// <summary>
/// Extensions for UIElementScope to add component builders.
/// </summary>
public static class UiElementScopeExtensions
{
    /// <summary>
    /// Creates a button within this scope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiButton Button(this ref UiElementScope scope, UiContext ctx, string id, string text)
        => new(ctx, id, text);

    /// <summary>
    /// Creates a card within this scope.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCard Card(this ref UiElementScope scope, UiContext ctx, string id)
        => new(ctx, id);
}
