using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public ref struct UiPropertyGrid
{
    private readonly UiContext _context;

    private float _labelWidth;
    private ushort _fontSize;
    private float _rowHeight;
    private float _gap;
    private UiColor _bgColor;
    private UiColor _altBgColor;
    private UiColor _labelColor;

    internal UiPropertyGrid(UiContext ctx, string id)
    {
        _context = ctx;
        Id = ctx.StringCache.GetId(id);

        _labelWidth = 120;
        _fontSize = 13;
        _rowHeight = 28;
        _gap = 2;
        _bgColor = UiColor.Rgb(35, 35, 40);
        _altBgColor = UiColor.Rgb(40, 40, 45);
        _labelColor = UiColor.Rgb(180, 180, 180);
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid LabelWidth(float w) { _labelWidth = w; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid RowHeight(float h) { _rowHeight = h; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid Gap(float gap) { _gap = gap; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid Background(UiColor bg, UiColor alt) { _bgColor = bg; _altBgColor = alt; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiPropertyGrid LabelColor(UiColor color) { _labelColor = color; return this; }

    public UiPropertyGridScope Open()
    {
        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        containerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = (ushort)_gap;

        _context.OpenElement(containerDecl);

        return new UiPropertyGridScope(_context, Id, _labelWidth, _fontSize, _rowHeight, _bgColor, _altBgColor, _labelColor);
    }
}

public ref struct UiPropertyGridScope
{
    private readonly UiContext _context;
    private readonly uint _parentId;
    private readonly float _labelWidth;
    private readonly ushort _fontSize;
    private readonly float _rowHeight;
    private readonly UiColor _bgColor;
    private readonly UiColor _altBgColor;
    private readonly UiColor _labelColor;
    // NOTE: _rowCounter is a uint[] (reference type) instead of a plain uint because
    // UiPropertyGridScope is a ref struct typically used with 'using var', which makes
    // the variable readonly. C# creates defensive copies when calling methods on readonly
    // structs, so a value-type counter would never actually increment. The array reference
    // survives defensive copies, ensuring Row() correctly increments across calls.
    private readonly uint[] _rowCounter;

    internal UiPropertyGridScope(UiContext context, uint parentId, float labelWidth, ushort fontSize,
        float rowHeight, UiColor bgColor, UiColor altBgColor, UiColor labelColor)
    {
        _context = context;
        _parentId = parentId;
        _labelWidth = labelWidth;
        _fontSize = fontSize;
        _rowHeight = rowHeight;
        _bgColor = bgColor;
        _altBgColor = altBgColor;
        _labelColor = labelColor;
        _rowCounter = new uint[1];
    }

    public UiPropertyRowScope Row(string label)
    {
        var isAlt = _rowCounter[0] % 2 == 1;
        var bg = isAlt ? _altBgColor : _bgColor;
        var rowIdx = _rowCounter[0]++;

        var rowId = _context.StringCache.GetId("PGRow", _parentId, rowIdx);
        var rowDecl = new ClayElementDeclaration { Id = rowId };
        rowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        rowDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        rowDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(_rowHeight, float.MaxValue);
        rowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        rowDecl.Layout.Padding = new ClayPadding { Left = 8, Right = 8 };
        rowDecl.Layout.ChildGap = 8;
        rowDecl.BackgroundColor = bg.ToClayColor();

        _context.OpenElement(rowDecl);

        var labelId = _context.StringCache.GetId("PGLbl", _parentId, rowIdx);
        var labelDecl = new ClayElementDeclaration { Id = labelId };
        labelDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_labelWidth);
        labelDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        _context.OpenElement(labelDecl);
        _context.Clay.Text(StringView.Intern(label), new ClayTextDesc
        {
            TextColor = _labelColor.ToClayColor(),
            FontSize = _fontSize
        });
        _context.Clay.CloseElement();

        var editorId = _context.StringCache.GetId("PGEd", _parentId, rowIdx);
        var editorDecl = new ClayElementDeclaration { Id = editorId };
        editorDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        editorDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        editorDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        _context.OpenElement(editorDecl);

        return new UiPropertyRowScope(_context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _context.Clay.CloseElement();
    }
}

public readonly ref struct UiPropertyRowScope
{
    private readonly UiContext _context;

    internal UiPropertyRowScope(UiContext context)
    {
        _context = context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _context.Clay.CloseElement();
        _context.Clay.CloseElement();
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPropertyGrid PropertyGrid(UiContext ctx, string id)
    {
        return new UiPropertyGrid(ctx, id);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPropertyGrid PropertyGrid(this ref UiElementScope scope, UiContext ctx, string id)
    {
        return Ui.PropertyGrid(ctx, id);
    }
}
