using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public struct UiContextMenuItem
{
    public string Label;
    public string? Icon;
    public string? Shortcut;
    public bool IsSeparator;
    public bool IsDisabled;

    public static UiContextMenuItem Item(string label, string? icon = null, string? shortcut = null)
    {
        return new UiContextMenuItem { Label = label, Icon = icon, Shortcut = shortcut };
    }

    public static UiContextMenuItem Separator()
    {
        return new UiContextMenuItem { IsSeparator = true, Label = "" };
    }
}

public sealed class UiContextMenuState
{
    public bool IsOpen { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public uint AnchorElementId { get; set; }
    public int HoveredIndex { get; set; } = -1;
    public int SkipCloseFrames { get; set; }

    public void OpenAt(float x, float y)
    {
        IsOpen = true;
        PositionX = x;
        PositionY = y;
        AnchorElementId = 0;
        HoveredIndex = -1;
        SkipCloseFrames = 3;
    }

    public void OpenBelow(uint elementId)
    {
        IsOpen = true;
        AnchorElementId = elementId;
        PositionX = 0;
        PositionY = 0;
        HoveredIndex = -1;
        SkipCloseFrames = 3;
    }

    public void Close()
    {
        IsOpen = false;
        AnchorElementId = 0;
        HoveredIndex = -1;
        SkipCloseFrames = 0;
    }
}

public ref struct UiContextMenu
{
    private readonly UiContext _context;
    private readonly UiContextMenuState _state;
    private readonly UiContextMenuItem[] _items;

    private UiColor _bgColor;
    private UiColor _hoverColor;
    private UiColor _textColor;
    private UiColor _disabledTextColor;
    private UiColor _iconColor;
    private UiColor _separatorColor;
    private ushort _fontSize;
    private float _itemHeight;
    private float _minWidth;
    private float _cornerRadius;

    internal UiContextMenu(UiContext ctx, string id, UiContextMenuState state, UiContextMenuItem[] items)
    {
        _context = ctx;
        _state = state;
        _items = items;
        Id = ctx.StringCache.GetId(id);

        _bgColor = UiColor.Rgb(35, 35, 40);
        _hoverColor = UiColor.Rgb(55, 55, 65);
        _textColor = UiColor.Rgb(220, 220, 220);
        _disabledTextColor = UiColor.Rgb(100, 100, 100);
        _iconColor = UiColor.Rgb(160, 160, 160);
        _separatorColor = UiColor.Rgb(60, 60, 65);
        _fontSize = 13;
        _itemHeight = 28;
        _minWidth = 160;
        _cornerRadius = 6;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu Background(UiColor color)
    {
        _bgColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu HoverColor(UiColor color)
    {
        _hoverColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu TextColor(UiColor color)
    {
        _textColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu IconColor(UiColor color)
    {
        _iconColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu SeparatorColor(UiColor color)
    {
        _separatorColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu FontSize(ushort size)
    {
        _fontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu ItemHeight(float h)
    {
        _itemHeight = h;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu MinWidth(float w)
    {
        _minWidth = w;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiContextMenu CornerRadius(float r)
    {
        _cornerRadius = r;
        return this;
    }

    public int Show()
    {
        if (!_state.IsOpen)
        {
            return -1;
        }

        var clickedIndex = -1;
        var anyItemHovered = false;

        var menuDecl = new ClayElementDeclaration { Id = Id };
        menuDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        menuDecl.Layout.Sizing.Width = ClaySizingAxis.Fit(_minWidth, float.MaxValue);
        menuDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        menuDecl.Layout.Padding = new ClayPadding { Top = 4, Bottom = 4 };
        menuDecl.BackgroundColor = _bgColor.ToClayColor();
        menuDecl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);
        menuDecl.Border = new ClayBorderDesc
        {
            Width = ClayBorderWidth.CreateUniform(1),
            Color = UiColor.Rgb(60, 60, 65).ToClayColor()
        };
        if (_state.AnchorElementId != 0)
        {
            menuDecl.Floating = new ClayFloatingDesc
            {
                AttachTo = ClayFloatingAttachTo.ElementWithId,
                ParentId = _state.AnchorElementId,
                ParentAttachPoint = ClayFloatingAttachPoint.LeftBottom,
                ElementAttachPoint = ClayFloatingAttachPoint.LeftTop,
                Offset = new Vector2(0, 2),
                ZIndex = 2000
            };
        }
        else
        {
            menuDecl.Floating = new ClayFloatingDesc
            {
                AttachTo = ClayFloatingAttachTo.Root,
                Offset = new Vector2(_state.PositionX, _state.PositionY),
                ZIndex = 2000
            };
        }

        _context.OpenElement(menuDecl);
        {
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];

                if (item.IsSeparator)
                {
                    var sepId = _context.StringCache.GetId("CMSep", Id, (uint)i);
                    var sepDecl = new ClayElementDeclaration { Id = sepId };
                    sepDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                    sepDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(1);
                    sepDecl.Layout.Padding = new ClayPadding { Left = 8, Right = 8 };
                    sepDecl.BackgroundColor = _separatorColor.ToClayColor();
                    _context.OpenElement(sepDecl);
                    _context.Clay.CloseElement();

                    var gapId = _context.StringCache.GetId("CMGap", Id, (uint)i);
                    var gapDecl = new ClayElementDeclaration { Id = gapId };
                    gapDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(4);
                    _context.OpenElement(gapDecl);
                    _context.Clay.CloseElement();
                    continue;
                }

                var itemId = _context.StringCache.GetId("CMItem", Id, (uint)i);
                var interaction = item.IsDisabled ? UiInteraction.None : _context.GetInteraction(itemId);
                var isHovered = interaction.IsHovered;
                if (isHovered)
                {
                    anyItemHovered = true;
                }

                var itemBg = isHovered ? _hoverColor : _bgColor;
                var itemDecl = new ClayElementDeclaration { Id = itemId };
                itemDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
                itemDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                itemDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                itemDecl.Layout.Padding = new ClayPadding { Left = 10, Right = 10 };
                itemDecl.Layout.ChildGap = 8;
                itemDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                itemDecl.BackgroundColor = itemBg.ToClayColor();

                _context.OpenElement(itemDecl);
                {
                    if (!string.IsNullOrEmpty(item.Icon))
                    {
                        _context.Clay.Text(item.Icon, new ClayTextDesc
                        {
                            TextColor = (item.IsDisabled ? _disabledTextColor : _iconColor).ToClayColor(),
                            FontSize = _fontSize,
                            FontId = FontAwesome.FontId,
                            TextAlignment = ClayTextAlignment.Center
                        });
                    }
                    else
                    {
                        var iconSpacerId = _context.StringCache.GetId("CMIco", Id, (uint)i);
                        var iconSpacerDecl = new ClayElementDeclaration { Id = iconSpacerId };
                        iconSpacerDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_fontSize);
                        _context.OpenElement(iconSpacerDecl);
                        _context.Clay.CloseElement();
                    }

                    _context.Clay.Text(item.Label, new ClayTextDesc
                    {
                        TextColor = (item.IsDisabled ? _disabledTextColor : _textColor).ToClayColor(),
                        FontSize = _fontSize
                    });

                    if (!string.IsNullOrEmpty(item.Shortcut))
                    {
                        // Spacer to push shortcut to the right
                        var spacerId = _context.StringCache.GetId("CMSpc", Id, (uint)i);
                        var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                        spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                        _context.OpenElement(spacerDecl);
                        _context.Clay.CloseElement();

                        _context.Clay.Text(item.Shortcut, new ClayTextDesc
                        {
                            TextColor = _disabledTextColor.ToClayColor(),
                            FontSize = _fontSize
                        });
                    }
                }
                _context.Clay.CloseElement();

                if (interaction.WasClicked && !item.IsDisabled)
                {
                    clickedIndex = i;
                }
            }
        }
        _context.Clay.CloseElement();

        if (_state.SkipCloseFrames > 0)
        {
            _state.SkipCloseFrames--;
        }
        else if (_context.MouseJustReleased && !anyItemHovered && !_context.Clay.PointerOver(Id))
        {
            _state.Close();
        }

        if (clickedIndex >= 0)
        {
            _state.Close();
        }

        return clickedIndex;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiContextMenu ContextMenu(UiContext ctx, string id, UiContextMenuItem[] items)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiContextMenuState>(elementId);
        return new UiContextMenu(ctx, id, state, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiContextMenuState GetContextMenuState(UiContext ctx, string id)
    {
        var elementId = ctx.StringCache.GetId(id);
        return ctx.GetOrCreateState<UiContextMenuState>(elementId);
    }
}
