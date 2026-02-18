using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public class UiTreeNode
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public List<UiTreeNode> Children { get; set; } = [];
    public object? Tag { get; set; }
}

public sealed class UiTreeViewState
{
    public HashSet<string> ExpandedNodes { get; } = [];
    public string SelectedNodeId { get; set; } = "";
}

public ref struct UiTreeView
{
    private readonly UiContext _context;
    private readonly UiTreeViewState _state;
    private readonly List<UiTreeNode> _roots;

    private UiColor _bgColor;
    private UiColor _selectedColor;
    private UiColor _hoverColor;
    private UiColor _textColor;
    private UiColor _iconColor;
    private UiColor _chevronColor;
    private UiColor _guideColor;
    private UiColor _borderColor;
    private ushort _fontSize;
    private float _indentSize;
    private float _itemHeight;
    private UiSizing _width;
    private UiSizing _height;
    private bool _showGuides;

    internal UiTreeView(UiContext ctx, string id, UiTreeViewState state, List<UiTreeNode> roots)
    {
        _context = ctx;
        _state = state;
        _roots = roots;
        Id = ctx.StringCache.GetId(id);

        _bgColor = UiColor.Rgb(30, 30, 34);
        _selectedColor = UiColor.Rgb(50, 80, 120);
        _hoverColor = UiColor.Rgb(45, 45, 50);
        _textColor = UiColor.Rgb(210, 210, 210);
        _iconColor = UiColor.Rgb(160, 160, 160);
        _chevronColor = UiColor.Rgb(140, 140, 140);
        _guideColor = UiColor.Rgb(50, 50, 55);
        _borderColor = UiColor.Rgb(55, 55, 60);
        _fontSize = 13;
        _indentSize = 16;
        _itemHeight = 26;
        _width = UiSizing.Grow();
        _height = UiSizing.Grow();
        _showGuides = true;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView Background(UiColor color) { _bgColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView SelectedColor(UiColor color) { _selectedColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView HoverColor(UiColor color) { _hoverColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView TextColor(UiColor color) { _textColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView IconColor(UiColor color) { _iconColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView ChevronColor(UiColor color) { _chevronColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView GuideColor(UiColor color) { _guideColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView BorderColor(UiColor color) { _borderColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView IndentSize(float size) { _indentSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView ItemHeight(float h) { _itemHeight = h; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView Width(float w) { _width = UiSizing.Fixed(w); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView Height(float h) { _height = UiSizing.Fixed(h); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView Height(UiSizing sizing) { _height = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTreeView ShowGuides(bool show) { _showGuides = show; return this; }

    private uint _nodeIndex;

    public bool Show(ref string selectedId)
    {
        var changed = false;
        _state.SelectedNodeId = selectedId;
        _nodeIndex = 0;

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        containerDecl.Layout.Sizing.Width = _width.ToClayAxis();
        containerDecl.Layout.Sizing.Height = _height.ToClayAxis();
        containerDecl.BackgroundColor = _bgColor.ToClayColor();
        containerDecl.BorderRadius = ClayBorderRadius.CreateUniform(4);
        containerDecl.Scroll.Vertical = true;

        _context.OpenElement(containerDecl);
        {
            foreach (var node in _roots)
            {
                if (RenderNode(node, 0, ref selectedId))
                {
                    changed = true;
                }
            }
        }
        _context.Clay.CloseElement();

        return changed;
    }

    private bool RenderNode(UiTreeNode node, int depth, ref string selectedId)
    {
        var changed = false;
        var hasChildren = node.Children.Count > 0;
        var isExpanded = _state.ExpandedNodes.Contains(node.Id);
        var isSelected = _state.SelectedNodeId == node.Id;

        var nodeIdx = _nodeIndex++;
        var rowId = _context.StringCache.GetId("TVRow", Id, nodeIdx);
        var interaction = _context.GetInteraction(rowId);

        var bgColor = isSelected ? _selectedColor
            : interaction.IsHovered ? _hoverColor
            : _bgColor;

        var rowDecl = new ClayElementDeclaration { Id = rowId };
        rowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        rowDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        rowDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
        rowDecl.Layout.Padding = new ClayPadding { Left = 4, Right = 4 };
        rowDecl.Layout.ChildGap = 4;
        rowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        rowDecl.BackgroundColor = bgColor.ToClayColor();

        _context.OpenElement(rowDecl);
        {
            if (depth > 0)
            {
                if (_showGuides)
                {
                    var guidesId = _context.StringCache.GetId("TVGs", Id, nodeIdx);
                    var guidesDecl = new ClayElementDeclaration { Id = guidesId };
                    guidesDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
                    guidesDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(depth * _indentSize);
                    guidesDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                    guidesDecl.Layout.ChildGap = 0;

                    _context.OpenElement(guidesDecl);
                    {
                        for (var i = 0; i < depth; i++)
                        {
                            var guideId = _context.StringCache.GetId("TVG", Id, nodeIdx * 32 + (uint)i);
                            var guideDecl = new ClayElementDeclaration { Id = guideId };
                            guideDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_indentSize);
                            guideDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                            guideDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                            guideDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

                            _context.OpenElement(guideDecl);
                            {
                                var lineId = _context.StringCache.GetId("TVL", Id, nodeIdx * 32 + (uint)i);
                                var lineDecl = new ClayElementDeclaration { Id = lineId };
                                lineDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(1);
                                lineDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                                lineDecl.BackgroundColor = _guideColor.ToClayColor();
                                _context.OpenElement(lineDecl);
                                _context.Clay.CloseElement();
                            }
                            _context.Clay.CloseElement();
                        }
                    }
                    _context.Clay.CloseElement();
                }
                else
                {
                    var spacerId = _context.StringCache.GetId("TVInd", Id, nodeIdx);
                    var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                    spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(depth * _indentSize);
                    _context.OpenElement(spacerDecl);
                    _context.Clay.CloseElement();
                }
            }

            if (hasChildren)
            {
                var chevronId = _context.StringCache.GetId("TVChev", Id, nodeIdx);
                var chevronInteraction = _context.GetInteraction(chevronId);
                var chevronIcon = isExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;

                var chevronDecl = new ClayElementDeclaration { Id = chevronId };
                chevronDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(14);
                chevronDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(14);
                chevronDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                chevronDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

                _context.OpenElement(chevronDecl);
                _context.Clay.Text(chevronIcon, new ClayTextDesc
                {
                    TextColor = _chevronColor.ToClayColor(),
                    FontSize = 10,
                    FontId = FontAwesome.FontId,
                    TextAlignment = ClayTextAlignment.Center
                });
                _context.Clay.CloseElement();

                if (chevronInteraction.WasClicked)
                {
                    if (isExpanded)
                    {
                        _state.ExpandedNodes.Remove(node.Id);
                    }
                    else
                    {
                        _state.ExpandedNodes.Add(node.Id);
                    }
                }
            }
            else
            {
                var spacerId = _context.StringCache.GetId("TVSpc", Id, nodeIdx);
                var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(14);
                _context.OpenElement(spacerDecl);
                _context.Clay.CloseElement();
            }

            if (!string.IsNullOrEmpty(node.Icon))
            {
                var iconId = _context.StringCache.GetId("TVIco", Id, nodeIdx);
                var iconDecl = new ClayElementDeclaration { Id = iconId };
                iconDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(16);
                iconDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
                iconDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                iconDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

                _context.OpenElement(iconDecl);
                _context.Clay.Text(node.Icon, new ClayTextDesc
                {
                    TextColor = _iconColor.ToClayColor(),
                    FontSize = (ushort)(_fontSize - 1),
                    FontId = FontAwesome.FontId,
                    TextAlignment = ClayTextAlignment.Center
                });
                _context.Clay.CloseElement();
            }

            _context.Clay.Text(node.Label, new ClayTextDesc
            {
                TextColor = _textColor.ToClayColor(),
                FontSize = _fontSize,
                WrapMode = ClayTextWrapMode.None
            });
        }
        _context.Clay.CloseElement();

        if (interaction.WasClicked)
        {
            selectedId = node.Id;
            _state.SelectedNodeId = node.Id;
            changed = true;
        }

        if (hasChildren && isExpanded)
        {
            foreach (var child in node.Children)
            {
                if (RenderNode(child, depth + 1, ref selectedId))
                {
                    changed = true;
                }
            }
        }

        return changed;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTreeView TreeView(UiContext ctx, string id, List<UiTreeNode> roots)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiTreeViewState>(elementId);
        return new UiTreeView(ctx, id, state, roots);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTreeView TreeView(this ref UiElementScope scope, UiContext ctx, string id, List<UiTreeNode> roots)
    {
        return Ui.TreeView(ctx, id, roots);
    }
}
