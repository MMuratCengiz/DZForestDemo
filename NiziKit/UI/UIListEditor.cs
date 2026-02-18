using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public sealed class UiListEditorState
{
    public int SelectedIndex { get; set; } = -1;
}

public struct UiListEditorResult
{
    public bool Added;
    public bool Removed;
    public int RemovedIndex;
    public bool ActionClicked;
    public int ActionClickedIndex;
}

public ref struct UiListEditor
{
    private readonly UiContext _context;
    private readonly UiListEditorState _state;

    private string _title;
    private UiColor _bgColor;
    private UiColor _selectedColor;
    private UiColor _hoverColor;
    private UiColor _textColor;
    private UiColor _headerBgColor;
    private bool _showAdd;
    private bool _showRemove;
    private ushort _fontSize;
    private float _itemHeight;
    private UiSizing _width;
    private UiSizing _height;
    private string? _itemActionIcon;
    private UiColor _itemActionColor;
    private UiColor _itemActionHoverColor;

    internal UiListEditor(UiContext ctx, string id, UiListEditorState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _title = "Items";
        _bgColor = UiColor.Rgb(30, 30, 34);
        _selectedColor = UiColor.Rgb(50, 80, 120);
        _hoverColor = UiColor.Rgb(45, 45, 50);
        _textColor = UiColor.Rgb(210, 210, 210);
        _headerBgColor = UiColor.Rgb(40, 40, 45);
        _showAdd = true;
        _showRemove = true;
        _fontSize = 13;
        _itemHeight = 26;
        _width = UiSizing.Grow();
        _height = UiSizing.Fixed(200);
        _itemActionIcon = null;
        _itemActionColor = UiColor.Rgb(180, 180, 180);
        _itemActionHoverColor = UiColor.Rgb(60, 60, 65);
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Title(string title) { _title = title; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Background(UiColor color) { _bgColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor SelectedColor(UiColor color) { _selectedColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor HoverColor(UiColor color) { _hoverColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor TextColor(UiColor color) { _textColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor ShowAdd(bool show) { _showAdd = show; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor ShowRemove(bool show) { _showRemove = show; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor ItemHeight(float h) { _itemHeight = h; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Width(float w) { _width = UiSizing.Fixed(w); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Height(float h) { _height = UiSizing.Fixed(h); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor Height(UiSizing sizing) { _height = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiListEditor ItemAction(string icon, UiColor color, UiColor? hoverColor = null)
    {
        _itemActionIcon = icon;
        _itemActionColor = color;
        _itemActionHoverColor = hoverColor ?? UiColor.Rgb(60, 60, 65);
        return this;
    }

    private string TruncateText(string text, float maxWidth)
    {
        if (maxWidth <= 0) return text;
        var measured = _context.Clay.MeasureText(text, 0, _fontSize);
        if (measured.Width <= maxWidth) return text;

        const string ellipsis = "...";
        var ellipsisDims = _context.Clay.MeasureText(ellipsis, 0, _fontSize);
        var remaining = maxWidth - ellipsisDims.Width;
        if (remaining <= 0) return ellipsis;

        var fitChars = _context.Clay.GetCharIndexAtOffset(text, remaining, 0, _fontSize);
        if (fitChars > 0 && fitChars < text.Length)
            return text[..(int)fitChars] + ellipsis;

        return ellipsis;
    }

    public UiListEditorResult Show(string[] items, ref int selectedIndex)
    {
        var result = new UiListEditorResult();
        _state.SelectedIndex = selectedIndex;

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        containerDecl.Layout.Sizing.Width = _width.ToClayAxis();
        containerDecl.Layout.Sizing.Height = _height.ToClayAxis();
        containerDecl.BackgroundColor = _bgColor.ToClayColor();
        containerDecl.BorderRadius = ClayBorderRadius.CreateUniform(4);
        containerDecl.Border = new ClayBorderDesc
        {
            Width = ClayBorderWidth.CreateUniform(1),
            Color = UiColor.Rgb(55, 55, 60).ToClayColor()
        };

        _context.OpenElement(containerDecl);
        {
            var headerId = _context.StringCache.GetId("LEHead", Id);
            var headerDecl = new ClayElementDeclaration { Id = headerId };
            headerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            headerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            headerDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(30);
            headerDecl.Layout.Padding = new ClayPadding { Left = 8, Right = 4, Top = 4, Bottom = 4 };
            headerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            headerDecl.Layout.ChildGap = 4;
            headerDecl.BackgroundColor = _headerBgColor.ToClayColor();
            headerDecl.BorderRadius = new ClayBorderRadius { TopLeft = 4, TopRight = 4 };

            _context.OpenElement(headerDecl);
            {
                _context.Clay.Text(_title, new ClayTextDesc
                {
                    TextColor = _textColor.ToClayColor(),
                    FontSize = _fontSize
                });

                var spacerId = _context.StringCache.GetId("LESpc", Id);
                var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                _context.OpenElement(spacerDecl);
                _context.Clay.CloseElement();

                if (_showAdd)
                {
                    var addId = _context.StringCache.GetId("LEAdd", Id);
                    var addInteraction = _context.GetInteraction(addId);
                    var addBg = addInteraction.IsHovered ? UiColor.Rgb(60, 60, 65) : UiColor.Transparent;

                    var addDecl = new ClayElementDeclaration { Id = addId };
                    addDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(22);
                    addDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(22);
                    addDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                    addDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                    addDecl.BackgroundColor = addBg.ToClayColor();
                    addDecl.BorderRadius = ClayBorderRadius.CreateUniform(3);

                    _context.OpenElement(addDecl);
                    _context.Clay.Text(FontAwesome.Plus, new ClayTextDesc
                    {
                        TextColor = UiColor.Rgb(100, 200, 100).ToClayColor(),
                        FontSize = 12,
                        FontId = FontAwesome.FontId,
                        TextAlignment = ClayTextAlignment.Center
                    });
                    _context.Clay.CloseElement();

                    if (addInteraction.WasClicked)
                    {
                        result.Added = true;
                    }
                }

                if (_showRemove)
                {
                    var removeId = _context.StringCache.GetId("LEDel", Id);
                    var removeInteraction = _context.GetInteraction(removeId);
                    var removeBg = removeInteraction.IsHovered ? UiColor.Rgb(60, 60, 65) : UiColor.Transparent;

                    var removeDecl = new ClayElementDeclaration { Id = removeId };
                    removeDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(22);
                    removeDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(22);
                    removeDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                    removeDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                    removeDecl.BackgroundColor = removeBg.ToClayColor();
                    removeDecl.BorderRadius = ClayBorderRadius.CreateUniform(3);

                    _context.OpenElement(removeDecl);
                    _context.Clay.Text(FontAwesome.Minus, new ClayTextDesc
                    {
                        TextColor = UiColor.Rgb(200, 100, 100).ToClayColor(),
                        FontSize = 12,
                        FontId = FontAwesome.FontId,
                        TextAlignment = ClayTextAlignment.Center
                    });
                    _context.Clay.CloseElement();

                    if (removeInteraction.WasClicked && _state.SelectedIndex >= 0 && _state.SelectedIndex < items.Length)
                    {
                        result.Removed = true;
                        result.RemovedIndex = _state.SelectedIndex;
                    }
                }
            }
            _context.Clay.CloseElement();

            var listId = _context.StringCache.GetId("LEList", Id);
            var listDecl = new ClayElementDeclaration { Id = listId };
            listDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            listDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            listDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            listDecl.Scroll.Vertical = true;

            _context.OpenElement(listDecl);
            {
                for (var i = 0; i < items.Length; i++)
                {
                    var itemId = _context.StringCache.GetId("LEItem", Id, (uint)i);
                    var interaction = _context.GetInteraction(itemId);
                    var isSelected = i == _state.SelectedIndex;

                    var itemBg = isSelected ? _selectedColor
                        : interaction.IsHovered ? _hoverColor
                        : _bgColor;

                    var itemDecl = new ClayElementDeclaration { Id = itemId };
                    itemDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
                    itemDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                    itemDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                    itemDecl.Layout.Padding = new ClayPadding { Left = 8, Right = 8 };
                    itemDecl.Layout.ChildGap = 4;
                    itemDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                    itemDecl.BackgroundColor = itemBg.ToClayColor();

                    _context.OpenElement(itemDecl);

                    if (_itemActionIcon != null)
                    {
                        var displayText = items[i];
                        var itemBbox = _context.Clay.GetElementBoundingBox(itemId);
                        if (itemBbox.Width > 0)
                        {
                            var availTextWidth = itemBbox.Width - 16 - 20 - 4 - 6;
                            displayText = TruncateText(displayText, availTextWidth);
                        }

                        var textWrapperId = _context.StringCache.GetId("LEITxt", Id, (uint)i);
                        var textWrapperDecl = new ClayElementDeclaration { Id = textWrapperId };
                        textWrapperDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                        textWrapperDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
                        textWrapperDecl.Layout.Padding = new ClayPadding { Right = 6 };
                        textWrapperDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                        _context.OpenElement(textWrapperDecl);
                        _context.Clay.Text(displayText, new ClayTextDesc
                        {
                            TextColor = _textColor.ToClayColor(),
                            FontSize = _fontSize,
                            WrapMode = ClayTextWrapMode.None
                        });
                        _context.Clay.CloseElement();

                        var actionId = _context.StringCache.GetId("LEIAct", Id, (uint)i);
                        var actionInteraction = _context.GetInteraction(actionId);
                        var actionBg = actionInteraction.IsHovered ? _itemActionHoverColor : UiColor.Transparent;

                        var actionDecl = new ClayElementDeclaration { Id = actionId };
                        actionDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(20);
                        actionDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(20);
                        actionDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                        actionDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                        actionDecl.BackgroundColor = actionBg.ToClayColor();
                        actionDecl.BorderRadius = ClayBorderRadius.CreateUniform(3);

                        _context.OpenElement(actionDecl);
                        _context.Clay.Text(_itemActionIcon, new ClayTextDesc
                        {
                            TextColor = _itemActionColor.ToClayColor(),
                            FontSize = (ushort)(_fontSize - 1),
                            FontId = FontAwesome.FontId,
                            TextAlignment = ClayTextAlignment.Center
                        });
                        _context.Clay.CloseElement();

                        if (actionInteraction.WasClicked)
                        {
                            result.ActionClicked = true;
                            result.ActionClickedIndex = i;
                        }
                    }
                    else
                    {
                        _context.Clay.Text(items[i], new ClayTextDesc
                        {
                            TextColor = _textColor.ToClayColor(),
                            FontSize = _fontSize,
                            WrapMode = ClayTextWrapMode.None
                        });
                    }

                    _context.Clay.CloseElement();

                    if (interaction.WasClicked && !result.ActionClicked)
                    {
                        _state.SelectedIndex = i;
                        selectedIndex = i;
                    }
                }
            }
            _context.Clay.CloseElement();
        }
        _context.Clay.CloseElement();

        return result;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiListEditor ListEditor(UiContext ctx, string id)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiListEditorState>(elementId);
        return new UiListEditor(ctx, id, state);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiListEditor ListEditor(this ref UiElementScope scope, UiContext ctx, string id)
    {
        return Ui.ListEditor(ctx, id);
    }
}
