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
    private ushort _fontSize;
    private float _indentSize;
    private float _itemHeight;
    private UiSizing _width;
    private UiSizing _height;

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
        _fontSize = 13;
        _indentSize = 18;
        _itemHeight = 26;
        _width = UiSizing.Grow();
        _height = UiSizing.Grow();
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

    private uint _nodeIndex;

    public bool Show(ref string selectedId)
    {
        var changed = false;
        _state.SelectedNodeId = selectedId;
        _nodeIndex = 0;

        // Scrollable container
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
        containerDecl.Scroll.Vertical = true;

        _context.Clay.OpenElement(containerDecl);
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
        var rowId = _context.StringCache.GetId("TVRow", Id + nodeIdx);
        var interaction = _context.GetInteraction(rowId);

        var bgColor = isSelected ? _selectedColor
            : interaction.IsHovered ? _hoverColor
            : _bgColor;

        // Row
        var rowDecl = new ClayElementDeclaration { Id = rowId };
        rowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        rowDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        rowDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_itemHeight);
        rowDecl.Layout.Padding = new ClayPadding { Left = (ushort)(4 + depth * _indentSize), Right = 4 };
        rowDecl.Layout.ChildGap = 6;
        rowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        rowDecl.BackgroundColor = bgColor.ToClayColor();

        _context.Clay.OpenElement(rowDecl);
        {
            // Chevron
            if (hasChildren)
            {
                var chevronId = _context.StringCache.GetId("TVChev", Id + nodeIdx);
                var chevronInteraction = _context.GetInteraction(chevronId);
                var chevronIcon = isExpanded ? FontAwesome.ChevronDown : FontAwesome.ChevronRight;

                var chevronDecl = new ClayElementDeclaration { Id = chevronId };
                chevronDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(14);
                chevronDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(14);
                chevronDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                chevronDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

                _context.Clay.OpenElement(chevronDecl);
                _context.Clay.Text(StringView.Intern(chevronIcon), new ClayTextDesc
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
                // Empty spacer for alignment
                var spacerId = _context.StringCache.GetId("TVSpc", Id + nodeIdx);
                var spacerDecl = new ClayElementDeclaration { Id = spacerId };
                spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(14);
                _context.Clay.OpenElement(spacerDecl);
                _context.Clay.CloseElement();
            }

            // Icon
            if (!string.IsNullOrEmpty(node.Icon))
            {
                _context.Clay.Text(StringView.Intern(node.Icon), new ClayTextDesc
                {
                    TextColor = _iconColor.ToClayColor(),
                    FontSize = (ushort)(_fontSize - 1),
                    FontId = FontAwesome.FontId,
                    TextAlignment = ClayTextAlignment.Center
                });
            }

            // Label
            _context.Clay.Text(StringView.Intern(node.Label), new ClayTextDesc
            {
                TextColor = _textColor.ToClayColor(),
                FontSize = _fontSize
            });
        }
        _context.Clay.CloseElement();

        // Row click selects
        if (interaction.WasClicked)
        {
            selectedId = node.Id;
            _state.SelectedNodeId = node.Id;
            changed = true;
        }

        // Render children if expanded
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
