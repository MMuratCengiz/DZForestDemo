using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public ref struct UiTabControl
{
    private readonly uint _id;

    private UiColor _tabBarBg;
    private UiColor _selectedBg;
    private UiColor _selectedText;
    private UiColor _defaultBg;
    private UiColor _defaultText;
    private UiColor _hoverBg;
    private UiColor _indicatorColor;
    private float _indicatorThickness;
    private UiColor _separatorColor;
    private float _separatorThickness;
    private ushort _fontSize;
    private UiPadding _tabPadding;

    internal UiTabControl(UiContext ctx, string id)
    {
        _id = ctx.StringCache.GetId(id);
        _tabBarBg = UiColor.DarkGray;
        _selectedBg = UiColor.Black;
        _selectedText = UiColor.White;
        _defaultBg = UiColor.DarkGray;
        _defaultText = UiColor.LightGray;
        _hoverBg = UiColor.Gray;
        _indicatorColor = UiColor.White;
        _indicatorThickness = 2;
        _separatorColor = UiColor.Gray;
        _separatorThickness = 1;
        _fontSize = 14;
        _tabPadding = UiPadding.Symmetric(12, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl TabBarBackground(UiColor color) { _tabBarBg = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl SelectedColor(UiColor bg, UiColor text) { _selectedBg = bg; _selectedText = text; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl DefaultColor(UiColor bg, UiColor text) { _defaultBg = bg; _defaultText = text; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl HoverColor(UiColor bg) { _hoverBg = bg; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl Indicator(UiColor color, float thickness) { _indicatorColor = color; _indicatorThickness = thickness; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl Separator(UiColor color, float thickness) { _separatorColor = color; _separatorThickness = thickness; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl TabPadding(UiPadding padding) { _tabPadding = padding; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTabControl TabPadding(float horizontal, float vertical) { _tabPadding = UiPadding.Symmetric(horizontal, vertical); return this; }

    public UiTabContentScope Show(string[] labels, ref int selectedIndex)
    {
        var ctx = NiziUi.Ctx;

        // Tab bar row
        var barId = ctx.StringCache.GetId("TabBar", _id);
        var barDecl = new ClayElementDeclaration { Id = barId };
        barDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        barDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        barDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        barDecl.BackgroundColor = _tabBarBg.ToClayColor();
        barDecl.Border = new ClayBorderDesc
        {
            Width = new ClayBorderWidth { Bottom = (uint)_separatorThickness },
            Color = _separatorColor.ToClayColor()
        };

        ctx.OpenElement(barDecl);
        {
            for (var i = 0; i < labels.Length; i++)
            {
                var isSelected = selectedIndex == i;
                var tabId = ctx.StringCache.GetId("Tab", _id, (uint)i);
                var interaction = ctx.GetInteraction(tabId);

                var bgColor = isSelected ? _selectedBg
                    : interaction.IsHovered ? _hoverBg
                    : _defaultBg;
                var textColor = isSelected ? _selectedText : _defaultText;

                var tabDecl = new ClayElementDeclaration { Id = tabId };
                tabDecl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
                tabDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
                tabDecl.Layout.Padding = _tabPadding.ToClayPadding();
                tabDecl.BackgroundColor = bgColor.ToClayColor();

                if (isSelected)
                {
                    tabDecl.Border = new ClayBorderDesc
                    {
                        Width = new ClayBorderWidth { Bottom = (uint)_indicatorThickness },
                        Color = _indicatorColor.ToClayColor()
                    };
                }

                ctx.OpenElement(tabDecl);
                {
                    var textDesc = new ClayTextDesc
                    {
                        TextColor = textColor.ToClayColor(),
                        FontSize = _fontSize,
                        WrapMode = ClayTextWrapMode.None
                    };
                    ctx.Clay.Text(labels[i], textDesc);
                }
                ctx.Clay.CloseElement();

                if (interaction.WasClicked)
                {
                    selectedIndex = i;
                }
            }
        }
        ctx.Clay.CloseElement();

        return new UiTabContentScope(selectedIndex);
    }
}

public readonly ref struct UiTabContentScope(int selectedIndex)
{
    public int SelectedIndex { get; } = selectedIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // No-op: the tab control itself doesn't open a container for content.
        // The caller renders content based on SelectedIndex.
    }
}
