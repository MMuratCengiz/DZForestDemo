using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

[Obsolete("Use NiziUi static methods instead")]
public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiButton Button(UiContext ctx, string id, string text)
    {
        return new UiButton(ctx, id, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCard Card(UiContext ctx, string id)
    {
        return new UiCard(ctx, id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCheckbox Checkbox(UiContext ctx, string id, string label, bool isChecked)
    {
        return new UiCheckbox(ctx, id, label, isChecked);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Spacer(UiContext ctx, float size)
    {
        NiziUi.Spacer(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void HorizontalSpacer(UiContext ctx, float width)
    {
        NiziUi.HorizontalSpacer(width);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VerticalSpacer(UiContext ctx, float height)
    {
        NiziUi.VerticalSpacer(height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FlexSpacer(UiContext ctx)
    {
        NiziUi.FlexSpacer();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Divider(UiContext ctx, UiColor color, float thickness = 1)
    {
        NiziUi.Divider(color, thickness);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void VerticalDivider(UiContext ctx, UiColor color, float thickness = 1)
    {
        NiziUi.VerticalDivider(color, thickness);
    }
}

public ref struct UiButton
{
    private readonly string _text;

    private UiColor _normalColor;
    private UiColor _hoverColor;
    private UiColor _pressedColor;
    private UiColor _textColor;
    private ushort _fontSize;
    private UiSizing _width;
    private UiSizing _height;
    private float _cornerRadius;
    private UiPadding _padding;
    private float _gap;
    private UiDirection _direction;
    private UiBorder _border;
    private bool _hasBorder;

    internal UiButton(UiContext ctx, string id, string text)
    {
        _text = text;
        Id = ctx.StringCache.GetId(id);

        _normalColor = UiColor.Rgb(100, 149, 237);
        _hoverColor = UiColor.Rgb(80, 129, 217);
        _pressedColor = UiColor.Rgb(60, 109, 197);
        _textColor = UiColor.White;
        _fontSize = 14;
        _width = UiSizing.Fit();
        _height = UiSizing.Fit();
        _cornerRadius = 8;
        _padding = UiPadding.Symmetric(16, 8);
        _gap = 8;
        _direction = UiDirection.Horizontal;
        _border = UiBorder.None;
        _hasBorder = false;
    }

    public uint Id { get; }

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
    public UiButton TextColor(UiColor color) { _textColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Size(float width, float height) { _width = UiSizing.Fixed(width); _height = UiSizing.Fixed(height); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Size(UiSizing width, UiSizing height) { _width = width; _height = height; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Width(float width) { _width = UiSizing.Fixed(width); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Height(float height) { _height = UiSizing.Fixed(height); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Height(UiSizing sizing) { _height = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton GrowWidth() { _width = UiSizing.Grow(); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton GrowHeight() { _height = UiSizing.Grow(); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Direction(UiDirection dir) { _direction = dir; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Horizontal() { _direction = UiDirection.Horizontal; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Vertical() { _direction = UiDirection.Vertical; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Gap(float gap) { _gap = gap; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton CornerRadius(float radius) { _cornerRadius = radius; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Border(float width, UiColor color) { _border = UiBorder.All(width, color); _hasBorder = true; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Border(UiBorder border) { _border = border; _hasBorder = true; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Padding(float all) { _padding = UiPadding.All(all); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Padding(float horizontal, float vertical) { _padding = UiPadding.Symmetric(horizontal, vertical); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiButton Padding(UiPadding padding) { _padding = padding; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private UiColor GetCurrentColor()
    {
        var interaction = NiziUi.Ctx.GetInteraction(Id);
        return interaction.IsPressed ? _pressedColor
            : interaction.IsHovered ? _hoverColor
            : _normalColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiInteraction GetInteraction() => NiziUi.Ctx.GetInteraction(Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasClicked() => NiziUi.Ctx.GetInteraction(Id).WasClicked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElementScope Open()
    {
        var decl = CreateDeclaration();
        NiziUi.Ctx.OpenElement(decl);
        return new UiElementScope();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Content(Action content)
    {
        using var _ = Open();
        content();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Show()
    {
        var interaction = NiziUi.Ctx.GetInteraction(Id);
        var decl = CreateDeclaration();

        NiziUi.Ctx.OpenElement(decl);
        {
            var textDesc = new ClayTextDesc
            {
                TextColor = _textColor.ToClayColor(),
                FontSize = _fontSize,
                TextAlignment = ClayTextAlignment.Center,
                WrapMode = ClayTextWrapMode.None
            };
            NiziUi.Ctx.Clay.Text(_text, textDesc);
        }
        NiziUi.Ctx.Clay.CloseElement();

        return interaction.WasClicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ClayElementDeclaration CreateDeclaration()
    {
        var bgColor = GetCurrentColor();

        var decl = new ClayElementDeclaration { Id = Id };
        decl.Layout.LayoutDirection = _direction == UiDirection.Horizontal
            ? ClayLayoutDirection.LeftToRight
            : ClayLayoutDirection.TopToBottom;
        decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        decl.Layout.ChildGap = (ushort)_gap;
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.Layout.Sizing.Width = _width.ToClayAxis();
        decl.Layout.Sizing.Height = _height.ToClayAxis();
        decl.BackgroundColor = bgColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_hasBorder)
        {
            decl.Border = _border.ToClayBorder();
        }

        return decl;
    }

    public UiTextStyle TextStyle => new()
    {
        Color = _textColor,
        FontSize = _fontSize,
        Alignment = UiTextAlign.Center
    };
}

public ref struct UiCard
{
    private UiColor _backgroundColor;
    private UiColor _borderColor;
    private float _borderWidth;
    private float _cornerRadius;
    private UiPadding _padding;
    private float _gap;
    private UiDirection _direction;
    private UiSizing _width;
    private UiSizing _height;
    private UiAlignX _alignX;
    private UiAlignY _alignY;

    internal UiCard(UiContext ctx, string id)
    {
        Id = ctx.StringCache.GetId(id);
        _backgroundColor = UiColor.White;
        _borderColor = UiColor.LightGray;
        _borderWidth = 1;
        _cornerRadius = 12;
        _padding = UiPadding.All(20);
        _gap = 12;
        _direction = UiDirection.Vertical;
        _width = UiSizing.Fit();
        _height = UiSizing.Fit();
        _alignX = UiAlignX.Center;
        _alignY = UiAlignY.Top;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Background(UiColor color) { _backgroundColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Border(float width, UiColor color) { _borderWidth = width; _borderColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Border(UiBorder border) { _borderWidth = border.Left; _borderColor = border.Color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard NoBorder() { _borderWidth = 0; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard CornerRadius(float radius) { _cornerRadius = radius; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Padding(float all) { _padding = UiPadding.All(all); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Padding(float horizontal, float vertical) { _padding = UiPadding.Symmetric(horizontal, vertical); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Padding(UiPadding padding) { _padding = padding; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Gap(float gap) { _gap = gap; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Direction(UiDirection dir) { _direction = dir; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Horizontal() { _direction = UiDirection.Horizontal; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Vertical() { _direction = UiDirection.Vertical; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard AlignChildren(UiAlignX x, UiAlignY y) { _alignX = x; _alignY = y; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard CenterChildren() { _alignX = UiAlignX.Center; _alignY = UiAlignY.Center; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Size(float width, float height) { _width = UiSizing.Fixed(width); _height = UiSizing.Fixed(height); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Size(UiSizing width, UiSizing height) { _width = width; _height = height; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Width(float width) { _width = UiSizing.Fixed(width); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Height(float height) { _height = UiSizing.Fixed(height); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Height(UiSizing sizing) { _height = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard GrowWidth() { _width = UiSizing.Grow(); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard GrowHeight() { _height = UiSizing.Grow(); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCard Grow() { _width = UiSizing.Grow(); _height = UiSizing.Grow(); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElementScope Open()
    {
        var decl = CreateDeclaration();
        NiziUi.Ctx.OpenElement(decl);
        return new UiElementScope();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Content(Action content)
    {
        using var _ = Open();
        content();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ClayElementDeclaration CreateDeclaration()
    {
        var decl = new ClayElementDeclaration { Id = Id };
        decl.Layout.LayoutDirection = _direction == UiDirection.Horizontal
            ? ClayLayoutDirection.LeftToRight
            : ClayLayoutDirection.TopToBottom;
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.Layout.ChildGap = (ushort)_gap;
        decl.Layout.ChildAlignment.X = _alignX switch
        {
            UiAlignX.Center => ClayAlignmentX.Center,
            UiAlignX.Right => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
        decl.Layout.ChildAlignment.Y = _alignY switch
        {
            UiAlignY.Center => ClayAlignmentY.Center,
            UiAlignY.Bottom => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
        decl.Layout.Sizing.Width = _width.ToClayAxis();
        decl.Layout.Sizing.Height = _height.ToClayAxis();
        decl.BackgroundColor = _backgroundColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_borderWidth > 0)
        {
            decl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_borderWidth),
                Color = _borderColor.ToClayColor()
            };
        }

        return decl;
    }
}

public ref struct UiCheckbox
{
    private readonly string _label;
    private readonly bool _isChecked;

    private UiColor _boxColor;
    private UiColor _boxHoverColor;
    private UiColor _checkColor;
    private UiColor _labelColor;
    private ushort _fontSize;
    private float _boxSize;
    private float _cornerRadius;
    private float _gap;
    private float _borderWidth;
    private UiColor _borderColor;

    internal UiCheckbox(UiContext ctx, string id, string label, bool isChecked)
    {
        _label = label;
        Id = ctx.StringCache.GetId(id);
        _isChecked = isChecked;

        _boxColor = UiColor.White;
        _boxHoverColor = UiColor.LightGray;
        _checkColor = UiColor.Rgb(100, 149, 237);
        _labelColor = UiColor.White;
        _fontSize = 12;
        _boxSize = 14;
        _cornerRadius = 3;
        _gap = 6;
        _borderWidth = 1;
        _borderColor = UiColor.Gray;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox BoxColor(UiColor color) { _boxColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox BoxColor(UiColor normal, UiColor hover) { _boxColor = normal; _boxHoverColor = hover; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox CheckColor(UiColor color) { _checkColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox LabelColor(UiColor color) { _labelColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox BorderColor(UiColor color) { _borderColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox BoxSize(float size) { _boxSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox Gap(float gap) { _gap = gap; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox CornerRadius(float radius) { _cornerRadius = radius; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCheckbox Border(float width, UiColor color) { _borderWidth = width; _borderColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiInteraction GetInteraction() => NiziUi.Ctx.GetInteraction(Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasClicked() => NiziUi.Ctx.GetInteraction(Id).WasClicked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Show()
    {
        var ctx = NiziUi.Ctx;
        var interaction = ctx.GetInteraction(Id);

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        containerDecl.Layout.ChildGap = (ushort)_gap;

        ctx.OpenElement(containerDecl);
        {
            var boxColor = interaction.IsHovered ? _boxHoverColor : _boxColor;
            var boxId = ctx.StringCache.GetId("ChkBox", Id);
            var boxDecl = new ClayElementDeclaration { Id = boxId };
            boxDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_boxSize);
            boxDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_boxSize);
            boxDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            boxDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            boxDecl.BackgroundColor = boxColor.ToClayColor();
            boxDecl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);
            boxDecl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_borderWidth),
                Color = _borderColor.ToClayColor()
            };

            ctx.OpenElement(boxDecl);
            {
                if (_isChecked)
                {
                    var checkSize = _boxSize * 0.6f;
                    var checkDecl = new ClayElementDeclaration { Id = ctx.StringCache.GetId("ChkMark", Id) };
                    checkDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(checkSize);
                    checkDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(checkSize);
                    checkDecl.BackgroundColor = _checkColor.ToClayColor();
                    checkDecl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius * 0.5f);
                    ctx.OpenElement(checkDecl);
                    ctx.Clay.CloseElement();
                }
            }
            ctx.Clay.CloseElement();
            if (!string.IsNullOrEmpty(_label))
            {
                var textDesc = new ClayTextDesc
                {
                    TextColor = _labelColor.ToClayColor(),
                    FontSize = _fontSize,
                    WrapMode = ClayTextWrapMode.None
                };
                ctx.Clay.Text(_label, textDesc);
            }
        }
        ctx.Clay.CloseElement();

        return interaction.WasClicked ? !_isChecked : _isChecked;
    }

    public UiTextStyle LabelStyle => new()
    {
        Color = _labelColor,
        FontSize = _fontSize,
        Alignment = UiTextAlign.Left
    };
}
