using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public sealed class UiCollapsibleSectionState
{
    public bool IsExpanded { get; set; } = true;
}

public ref struct UiCollapsibleSection
{
    private readonly UiCollapsibleSectionState _state;
    private readonly string _title;

    private UiColor _headerBgColor;
    private UiColor _headerHoverColor;
    private UiColor _headerTextColor;
    private UiColor _bodyBgColor;
    private UiColor _chevronColor;
    private ushort _fontSize;
    private float _padding;
    private float _gap;
    private float _cornerRadius;
    private float _borderWidth;
    private UiColor _borderColor;
    private string? _badge;
    private string? _headerActionIcon;
    private UiColor _headerActionColor;
    private UiColor _headerActionHoverColor;

    internal UiCollapsibleSection(UiContext ctx, string id, UiCollapsibleSectionState state, string title)
    {
        _state = state;
        _title = title;
        Id = ctx.StringCache.GetId(id);

        _headerBgColor = UiColor.Rgb(40, 40, 45);
        _headerHoverColor = UiColor.Rgb(50, 50, 55);
        _headerTextColor = UiColor.White;
        _bodyBgColor = UiColor.Rgb(35, 35, 40);
        _chevronColor = UiColor.Gray;
        _fontSize = 14;
        _padding = 10;
        _gap = 0;
        _cornerRadius = 4;
        _borderWidth = 0;
        _borderColor = UiColor.Transparent;
        _badge = null;
        _headerActionIcon = null;
        _headerActionColor = UiColor.Gray;
        _headerActionHoverColor = UiColor.White;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection HeaderBackground(UiColor color)
    {
        _headerBgColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection HeaderBackground(UiColor normal, UiColor hover)
    {
        _headerBgColor = normal;
        _headerHoverColor = hover;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection HeaderTextColor(UiColor color)
    {
        _headerTextColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection BodyBackground(UiColor color)
    {
        _bodyBgColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection ChevronColor(UiColor color)
    {
        _chevronColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection FontSize(ushort size)
    {
        _fontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection Padding(float padding)
    {
        _padding = padding;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection Gap(float gap)
    {
        _gap = gap;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection CornerRadius(float radius)
    {
        _cornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection Border(float width, UiColor color)
    {
        _borderWidth = width;
        _borderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection Badge(string text)
    {
        _badge = text;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiCollapsibleSection HeaderAction(string icon, UiColor color, UiColor hoverColor)
    {
        _headerActionIcon = icon;
        _headerActionColor = color;
        _headerActionHoverColor = hoverColor;
        return this;
    }

    public bool HeaderActionClicked { get; private set; }

    public UiCollapsibleSectionScope Open()
    {
        var headerId = NiziUi.Ctx.StringCache.GetId("CSHeader", Id);
        var interaction = NiziUi.Ctx.GetInteraction(headerId);

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        containerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.ChildGap = (ushort)_gap;
        containerDecl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);
        if (_borderWidth > 0)
        {
            containerDecl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((int)_borderWidth),
                Color = _borderColor.ToClayColor()
            };
        }
        NiziUi.Ctx.OpenElement(containerDecl);

        var headerBg = interaction.IsHovered ? _headerHoverColor : _headerBgColor;
        var headerDecl = new ClayElementDeclaration { Id = headerId };
        headerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        headerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        headerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        headerDecl.Layout.Padding = UiPadding.All(_padding).ToClayPadding();
        headerDecl.Layout.ChildGap = 8;
        headerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        headerDecl.BackgroundColor = headerBg.ToClayColor();
        headerDecl.BorderRadius = _state.IsExpanded
            ? new ClayBorderRadius { TopLeft = _cornerRadius, TopRight = _cornerRadius }
            : ClayBorderRadius.CreateUniform(_cornerRadius);

        NiziUi.Ctx.OpenElement(headerDecl);
        {
            var chevronIcon = _state.IsExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;
            NiziUi.Ctx.Clay.Text(chevronIcon, new ClayTextDesc
            {
                TextColor = _chevronColor.ToClayColor(),
                FontSize = (ushort)(_fontSize - 2),
                FontId = FontAwesome.FontId,
                TextAlignment = ClayTextAlignment.Center
            });

            NiziUi.Ctx.Clay.Text(_title, new ClayTextDesc
            {
                TextColor = _headerTextColor.ToClayColor(),
                FontSize = _fontSize,
                WrapMode = ClayTextWrapMode.None
            });

            // Spacer to push badge/action to the right
            if (!string.IsNullOrEmpty(_badge) || _headerActionIcon != null)
            {
                var spacerDecl = new ClayElementDeclaration { Id = NiziUi.Ctx.StringCache.GetId("CSSpacer", Id) };
                spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                NiziUi.Ctx.OpenElement(spacerDecl);
                NiziUi.Ctx.Clay.CloseElement();
            }

            if (!string.IsNullOrEmpty(_badge))
            {
                var badgeDecl = new ClayElementDeclaration { Id = NiziUi.Ctx.StringCache.GetId("CSBadge", Id) };
                badgeDecl.Layout.Padding = new ClayPadding { Left = 6, Right = 6, Top = 2, Bottom = 2 };
                badgeDecl.BackgroundColor = UiColor.Rgb(60, 60, 65).ToClayColor();
                badgeDecl.BorderRadius = ClayBorderRadius.CreateUniform(3);
                NiziUi.Ctx.OpenElement(badgeDecl);
                NiziUi.Ctx.Clay.Text(_badge, new ClayTextDesc
                {
                    TextColor = UiColor.Gray.ToClayColor(),
                    FontSize = (ushort)(_fontSize - 2)
                });
                NiziUi.Ctx.Clay.CloseElement();
            }

            if (_headerActionIcon != null)
            {
                var actionId = NiziUi.Ctx.StringCache.GetId("CSAction", Id);
                var actionInteraction = NiziUi.Ctx.GetInteraction(actionId);
                var actionColor = actionInteraction.IsHovered ? _headerActionHoverColor : _headerActionColor;

                var actionDecl = new ClayElementDeclaration { Id = actionId };
                actionDecl.Layout.Padding = new ClayPadding { Left = 4, Right = 4, Top = 2, Bottom = 2 };
                actionDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                NiziUi.Ctx.OpenElement(actionDecl);
                NiziUi.Ctx.Clay.Text(_headerActionIcon, new ClayTextDesc
                {
                    TextColor = actionColor.ToClayColor(),
                    FontSize = (ushort)(_fontSize - 2),
                    FontId = FontAwesome.FontId,
                    TextAlignment = ClayTextAlignment.Center
                });
                NiziUi.Ctx.Clay.CloseElement();

                HeaderActionClicked = actionInteraction.WasClicked;
            }
        }
        NiziUi.Ctx.Clay.CloseElement();

        if (interaction.WasClicked && !HeaderActionClicked)
        {
            _state.IsExpanded = !_state.IsExpanded;
        }

        if (_state.IsExpanded)
        {
            var bodyDecl = new ClayElementDeclaration { Id = NiziUi.Ctx.StringCache.GetId("CSBody", Id) };
            bodyDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            bodyDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            bodyDecl.Layout.Padding = UiPadding.All(_padding).ToClayPadding();
            bodyDecl.Layout.ChildGap = 8;
            bodyDecl.BackgroundColor = _bodyBgColor.ToClayColor();
            bodyDecl.BorderRadius = new ClayBorderRadius
            {
                BottomLeft = _cornerRadius,
                BottomRight = _cornerRadius
            };
            NiziUi.Ctx.OpenElement(bodyDecl);
            return new UiCollapsibleSectionScope(true);
        }

        NiziUi.Ctx.Clay.CloseElement();
        return new UiCollapsibleSectionScope(false);
    }

    public bool IsExpanded => _state.IsExpanded;
}

public readonly ref struct UiCollapsibleSectionScope
{
    private readonly bool _isExpanded;

    internal UiCollapsibleSectionScope(bool isExpanded)
    {
        _isExpanded = isExpanded;
    }

    public bool IsExpanded => _isExpanded;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Panel(string id) => new(NiziUi.Ctx, id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Row(string id = "Row") => new UiElement(NiziUi.Ctx, id).Horizontal();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiElement Column(string id = "Column") => new UiElement(NiziUi.Ctx, id).Vertical();

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
        NiziUi.Ctx.Clay.Text(text, desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Icon(string icon, UiColor color, ushort size = 14)
    {
        NiziUi.Ctx.Clay.Text(icon, new ClayTextDesc
        {
            TextColor = color.ToClayColor(),
            FontSize = size,
            FontId = FontAwesome.FontId,
            TextAlignment = ClayTextAlignment.Center
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_isExpanded)
        {
            NiziUi.Ctx.Clay.CloseElement();
            NiziUi.Ctx.Clay.CloseElement();
        }
    }
}

public static partial class Ui
{
    [Obsolete("Use NiziUi static methods instead")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCollapsibleSection CollapsibleSection(UiContext ctx, string id, string title)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiCollapsibleSectionState>(elementId);
        return new UiCollapsibleSection(ctx, id, state, title);
    }

    [Obsolete("Use NiziUi static methods instead")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCollapsibleSection CollapsibleSection(UiContext ctx, string id, string title, bool initialExpanded)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState(() => new UiCollapsibleSectionState { IsExpanded = initialExpanded }, elementId);
        return new UiCollapsibleSection(ctx, id, state, title);
    }
}
