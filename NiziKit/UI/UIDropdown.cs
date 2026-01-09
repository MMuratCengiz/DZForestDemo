using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public sealed class UiDropdownState
{
    public bool IsOpen { get; set; }
    public int SelectedIndex { get; set; } = -1;
    public int HoveredIndex { get; set; } = -1;

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Close()
    {
        IsOpen = false;
        HoveredIndex = -1;
    }

    public void Open()
    {
        IsOpen = true;
        HoveredIndex = -1;
    }
}

public ref struct UiDropdown
{
    private readonly UiContext _context;
    private readonly UiDropdownState _state;
    private readonly string[] _items;

    private WidgetStyle _style;
    private UiColor _itemHoverColor;
    private UiColor _selectedItemColor;
    private UiColor _dropdownBgColor;
    private UiColor _arrowColor;
    private UiSizing _width;
    private float _itemHeight;
    private float _maxDropdownHeight;
    private string _placeholder;

    internal UiDropdown(UiContext ctx, string id, UiDropdownState state, string[] items)
    {
        _context = ctx;
        _state = state;
        _items = items;
        Id = ctx.StringCache.GetId(id);

        _style = WidgetStyle.Default;
        _itemHoverColor = UiColor.Rgb(65, 65, 70);
        _selectedItemColor = UiColor.Rgb(60, 130, 200);
        _dropdownBgColor = UiColor.Rgb(35, 35, 40);
        _arrowColor = UiColor.Gray;
        _width = UiSizing.Fixed(200);
        _itemHeight = 28;
        _maxDropdownHeight = 200;
        _placeholder = "Select...";
    }

    public uint Id { get; }

    public bool IsOpen => _state.IsOpen;

    public int SelectedIndex => _state.SelectedIndex;

    public string? SelectedItem => _state.SelectedIndex >= 0 && _state.SelectedIndex < _items.Length
        ? _items[_state.SelectedIndex]
        : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Style(WidgetStyle style)
    {
        _style = style;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Background(UiColor color)
    {
        _style.BackgroundColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Background(UiColor normal, UiColor hover)
    {
        _style.BackgroundColor = normal;
        _style.HoverColor = hover;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown TextColor(UiColor color)
    {
        _style.TextColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Border(float width, UiColor color)
    {
        _style.BorderWidth = width;
        _style.BorderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Border(float width, UiColor normal, UiColor focused)
    {
        _style.BorderWidth = width;
        _style.BorderColor = normal;
        _style.FocusedBorderColor = focused;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown CornerRadius(float radius)
    {
        _style.CornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown FontSize(ushort size)
    {
        _style.FontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Padding(float h, float v)
    {
        _style.Padding = UiPadding.Symmetric(h, v);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Width(float w)
    {
        _width = UiSizing.Fixed(w);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Width(UiSizing sizing)
    {
        _width = sizing;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown GrowWidth()
    {
        _width = UiSizing.Grow();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown ItemHeight(float h)
    {
        _itemHeight = h;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown MaxDropdownHeight(float h)
    {
        _maxDropdownHeight = h;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown ItemHoverColor(UiColor color)
    {
        _itemHoverColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown SelectedItemColor(UiColor color)
    {
        _selectedItemColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown DropdownBackground(UiColor color)
    {
        _dropdownBgColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown ArrowColor(UiColor color)
    {
        _arrowColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDropdown Placeholder(string text)
    {
        _placeholder = text;
        return this;
    }

    public bool Show(ref int selectedIndex)
    {
        if (_state.SelectedIndex != selectedIndex)
        {
            _state.SelectedIndex = selectedIndex;
        }

        var changed = false;
        var headerId = _context.StringCache.GetId("DDHeader", Id);
        var interaction = _context.GetInteraction(headerId);

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        containerDecl.Layout.Sizing.Width = _width.ToClayAxis();

        _context.Clay.OpenElement(containerDecl);
        {
            RenderHeader(interaction);

            if (_state.IsOpen)
            {
                changed = RenderDropdownList(headerId);
            }
        }
        _context.Clay.CloseElement();

        if (interaction.WasClicked)
        {
            _state.Toggle();
        }

        if (_state.IsOpen && _context.MouseJustReleased && !interaction.IsHovered)
        {
            var anyItemHovered = false;
            for (var i = 0; i < _items.Length; i++)
            {
                var itemId = _context.StringCache.GetId("DDItem", Id + (uint)i);
                if (_context.Clay.PointerOver(itemId))
                {
                    anyItemHovered = true;
                    break;
                }
            }

            var listId = _context.StringCache.GetId("DDList", Id);
            var listHovered = _context.Clay.PointerOver(listId);

            if (!anyItemHovered && !listHovered)
            {
                _state.Close();
            }
        }

        if (changed)
        {
            selectedIndex = _state.SelectedIndex;
        }

        return changed;
    }

    private void RenderHeader(UiInteraction interaction)
    {
        var bgColor = _style.GetBackgroundColor(interaction.IsHovered, interaction.IsPressed, false);
        var borderColor = _state.IsOpen ? _style.FocusedBorderColor : _style.BorderColor;

        var headerDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("DDHeader", Id) };
        headerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        headerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        headerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        headerDecl.Layout.Padding = _style.Padding.ToClayPadding();
        headerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        headerDecl.BackgroundColor = bgColor.ToClayColor();
        headerDecl.BorderRadius = _state.IsOpen
            ? new ClayBorderRadius { TopLeft = _style.CornerRadius, TopRight = _style.CornerRadius }
            : ClayBorderRadius.CreateUniform(_style.CornerRadius);

        if (_style.BorderWidth > 0)
        {
            headerDecl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_style.BorderWidth),
                Color = borderColor.ToClayColor()
            };
        }

        _context.Clay.OpenElement(headerDecl);
        {
            var textDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("DDText", Id) };
            textDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            _context.Clay.OpenElement(textDecl);
            {
                var displayText = SelectedItem ?? _placeholder;
                var textColor = SelectedItem != null ? _style.TextColor : UiColor.Gray;
                _context.Clay.Text(StringView.Intern(displayText), new ClayTextDesc
                {
                    TextColor = textColor.ToClayColor(),
                    FontSize = _style.FontSize
                });
            }
            _context.Clay.CloseElement();

            RenderArrow();
        }
        _context.Clay.CloseElement();
    }

    private void RenderArrow()
    {
        var arrowDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("DDArrow", Id) };
        arrowDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(16);
        arrowDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(16);
        arrowDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        arrowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        _context.Clay.OpenElement(arrowDecl);
        {
            var arrowText = _state.IsOpen ? "^" : "v";
            _context.Clay.Text(StringView.Intern(arrowText), new ClayTextDesc
            {
                TextColor = _arrowColor.ToClayColor(),
                FontSize = 12
            });
        }
        _context.Clay.CloseElement();
    }

    private bool RenderDropdownList(uint headerId)
    {
        var changed = false;

        var listDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("DDList", Id) };
        listDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        listDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        listDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, _maxDropdownHeight);
        listDecl.BackgroundColor = _dropdownBgColor.ToClayColor();
        listDecl.BorderRadius = new ClayBorderRadius
        {
            BottomLeft = _style.CornerRadius,
            BottomRight = _style.CornerRadius
        };

        // Make this a floating element attached to the header
        listDecl.Floating = new ClayFloatingDesc
        {
            AttachTo = ClayFloatingAttachTo.ElementWithId,
            ParentId = headerId,
            ParentAttachPoint = ClayFloatingAttachPoint.LeftBottom,
            ElementAttachPoint = ClayFloatingAttachPoint.LeftTop,
            ZIndex = 1000
        };

        if (_style.BorderWidth > 0)
        {
            listDecl.Border = new ClayBorderDesc
            {
                Width = new ClayBorderWidth
                {
                    Left = (uint)_style.BorderWidth,
                    Right = (uint)_style.BorderWidth,
                    Bottom = (uint)_style.BorderWidth
                },
                Color = _style.FocusedBorderColor.ToClayColor()
            };
        }

        listDecl.Scroll.Vertical = true;

        _context.Clay.OpenElement(listDecl);
        {
            for (var i = 0; i < _items.Length; i++)
            {
                if (RenderItem(i))
                {
                    _state.SelectedIndex = i;
                    _state.Close();
                    changed = true;
                }
            }
        }
        _context.Clay.CloseElement();

        return changed;
    }

    private bool RenderItem(int index)
    {
        var itemId = _context.StringCache.GetId("DDItem", Id + (uint)index);
        var interaction = _context.GetInteraction(itemId);
        var isSelected = index == _state.SelectedIndex;

        var bgColor = isSelected ? _selectedItemColor
            : interaction.IsHovered ? _itemHoverColor
            : _dropdownBgColor;

        var itemDecl = new ClayElementDeclaration { Id = itemId };
        itemDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        itemDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        itemDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        itemDecl.Layout.Padding = _style.Padding.ToClayPadding();
        itemDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        itemDecl.BackgroundColor = bgColor.ToClayColor();

        _context.Clay.OpenElement(itemDecl);
        {
            _context.Clay.Text(StringView.Intern(_items[index]), new ClayTextDesc
            {
                TextColor = _style.TextColor.ToClayColor(),
                FontSize = _style.FontSize
            });
        }
        _context.Clay.CloseElement();

        return interaction.WasClicked;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDropdown Dropdown(UiContext ctx, string id, string[] items)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiDropdownState>(elementId);
        return new UiDropdown(ctx, id, state, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDropdown Dropdown(UiContext ctx, string id, string[] items, int initialSelectedIndex)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState(() => new UiDropdownState { SelectedIndex = initialSelectedIndex }, elementId);
        return new UiDropdown(ctx, id, state, items);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDropdown Dropdown(this ref UiElementScope scope, UiContext ctx, string id, string[] items)
    {
        return Ui.Dropdown(ctx, id, items);
    }
}
