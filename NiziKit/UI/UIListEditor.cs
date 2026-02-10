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

    public UiListEditorResult Show(string[] items, ref int selectedIndex)
    {
        var result = new UiListEditorResult();
        _state.SelectedIndex = selectedIndex;

        // Container
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

        _context.Clay.OpenElement(containerDecl);
        {
            // Header
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

            _context.Clay.OpenElement(headerDecl);
            {
                _context.Clay.Text(StringView.Intern(_title), new ClayTextDesc
                {
                    TextColor = _textColor.ToClayColor(),
                    FontSize = _fontSize
                });

                // Flex spacer
                var spacerId = _context.StringCache.GetId("LESpc", Id);
                var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                _context.Clay.OpenElement(spacerDecl);
                _context.Clay.CloseElement();

                // Add button
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

                    _context.Clay.OpenElement(addDecl);
                    _context.Clay.Text(StringView.Intern(FontAwesome.Plus), new ClayTextDesc
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

                // Remove button
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

                    _context.Clay.OpenElement(removeDecl);
                    _context.Clay.Text(StringView.Intern(FontAwesome.Minus), new ClayTextDesc
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

            // Item list (scrollable)
            var listId = _context.StringCache.GetId("LEList", Id);
            var listDecl = new ClayElementDeclaration { Id = listId };
            listDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            listDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            listDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            listDecl.Scroll.Vertical = true;

            _context.Clay.OpenElement(listDecl);
            {
                for (var i = 0; i < items.Length; i++)
                {
                    var itemId = _context.StringCache.GetId("LEItem", Id + (uint)i);
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
                    itemDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                    itemDecl.BackgroundColor = itemBg.ToClayColor();

                    _context.Clay.OpenElement(itemDecl);
                    _context.Clay.Text(StringView.Intern(items[i]), new ClayTextDesc
                    {
                        TextColor = _textColor.ToClayColor(),
                        FontSize = _fontSize
                    });
                    _context.Clay.CloseElement();

                    if (interaction.WasClicked)
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
