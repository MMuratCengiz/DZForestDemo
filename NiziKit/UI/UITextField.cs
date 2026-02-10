using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

public enum UiTextOverflow
{
    Clip,
    Scroll,
    Wrap
}

public sealed class UiTextFieldState
{
    private readonly StringBuilder _text = new();
    private List<int>? _lineStarts;
    private bool _lineStartsDirty = true;

    private string? _cachedFullText;
    private bool _textDirty = true;

    private string?[] _cachedLineStrings = new string?[16];
    private int _cachedLineCount;
    private bool _lineStringsDirty = true;

    private int _cursorPosition;
    private int _selectionAnchor = -1;

    private int _cachedCursorSegmentPos = -1;
    private string _cachedBeforeCursor = "";
    private string _cachedAfterCursor = "";

    private int _cachedSelStart = -1;
    private int _cachedSelEnd = -1;
    private string _cachedBeforeSel = "";
    private string _cachedSelected = "";
    private string _cachedAfterSel = "";

    private int _cachedLineCursorLine = -1;
    private int _cachedLineCursorCol = -1;
    private string _cachedLineBeforeCursor = "";
    private string _cachedLineAfterCursor = "";

    private int _cachedLineSelLine = -1;
    private int _cachedLineSelStart = -1;
    private int _cachedLineSelEnd = -1;
    private string _cachedLineBeforeSel = "";
    private string _cachedLineSelected = "";
    private string _cachedLineAfterSel = "";

    public float LastMouseX { get; set; }
    public float LastMouseY { get; set; }
    public bool IsDragging { get; set; }

    public float ScrollOffsetX { get; set; }

    public float ContentBoundingBoxX { get; set; }
    public float ContentBoundingBoxY { get; set; }
    public float ContentBoundingBoxWidth { get; set; }
    public float ContentBoundingBoxHeight { get; set; }

    public float BoundingBoxX { get; set; }
    public float BoundingBoxY { get; set; }
    public float BoundingBoxWidth { get; set; }
    public float BoundingBoxHeight { get; set; }

    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                InvalidateSegmentCache();
            }
        }
    }

    public int SelectionAnchor
    {
        get => _selectionAnchor;
        set
        {
            if (_selectionAnchor != value)
            {
                _selectionAnchor = value;
                InvalidateSegmentCache();
            }
        }
    }

    public bool IsSelecting { get; set; }
    public float CursorBlinkTime { get; set; }
    public bool CursorVisible { get; set; } = true;
    public int PreferredColumn { get; set; } = -1;

    public bool HasSelection => _selectionAnchor >= 0 && _selectionAnchor != _cursorPosition;
    public int SelectionStart => HasSelection ? Math.Min(_selectionAnchor, _cursorPosition) : _cursorPosition;
    public int SelectionEnd => HasSelection ? Math.Max(_selectionAnchor, _cursorPosition) : _cursorPosition;

    private void InvalidateSegmentCache()
    {
        _cachedCursorSegmentPos = -1;
        _cachedSelStart = -1;
        _cachedSelEnd = -1;
        _cachedLineCursorLine = -1;
        _cachedLineSelLine = -1;
    }

    public int Length => _text.Length;

    public string Text
    {
        get
        {
            if (_textDirty || _cachedFullText == null)
            {
                _cachedFullText = _text.ToString();
                _textDirty = false;
            }
            return _cachedFullText;
        }
        set
        {
            _text.Clear();
            _text.Append(value ?? "");
            CursorPosition = Math.Min(CursorPosition, _text.Length);
            ClearSelection();
            MarkDirty();
        }
    }

    public string SelectedText
    {
        get
        {
            if (!HasSelection)
            {
                return "";
            }

            var start = SelectionStart;
            var end = SelectionEnd;
            return _text.ToString(start, end - start);
        }
    }

    public float PaddingLeft { get; set; }
    public float FontSize { get; set; }

    internal int LineCount
    {
        get
        {
            EnsureLineStarts();
            return _lineStarts!.Count;
        }
    }

    private void MarkDirty()
    {
        _lineStartsDirty = true;
        _textDirty = true;
        _lineStringsDirty = true;
        InvalidateSegmentCache();
    }

    public void ClearSelection()
    {
        SelectionAnchor = -1;
        IsSelecting = false;
    }

    public void StartSelection()
    {
        if (SelectionAnchor < 0)
        {
            SelectionAnchor = CursorPosition;
        }
    }

    public void SelectAll()
    {
        SelectionAnchor = 0;
        CursorPosition = _text.Length;
    }

    public void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var start = SelectionStart;
        var length = SelectionEnd - SelectionStart;
        _text.Remove(start, length);
        CursorPosition = start;
        ClearSelection();
        MarkDirty();
    }

    public void InsertText(string text, int? maxLength = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (HasSelection)
        {
            DeleteSelection();
        }

        if (maxLength.HasValue && _text.Length + text.Length > maxLength.Value)
        {
            var available = maxLength.Value - _text.Length;
            if (available <= 0)
            {
                return;
            }

            text = text[..available];
        }

        _text.Insert(CursorPosition, text);
        CursorPosition += text.Length;
        PreferredColumn = -1;
        MarkDirty();
    }

    public void DeleteBack()
    {
        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (CursorPosition > 0)
        {
            _text.Remove(CursorPosition - 1, 1);
            CursorPosition--;
            PreferredColumn = -1;
            MarkDirty();
        }
    }

    public void DeleteForward()
    {
        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (CursorPosition < _text.Length)
        {
            _text.Remove(CursorPosition, 1);
            PreferredColumn = -1;
            MarkDirty();
        }
    }

    public char GetChar(int index)
    {
        return index >= 0 && index < _text.Length ? _text[index] : '\0';
    }

    public void ResetCursorBlink()
    {
        CursorBlinkTime = 0;
        CursorVisible = true;
    }

    private void EnsureLineStarts()
    {
        if (!_lineStartsDirty && _lineStarts != null)
        {
            return;
        }

        _lineStarts ??= [];
        _lineStarts.Clear();
        _lineStarts.Add(0);
        for (var i = 0; i < _text.Length; i++)
        {
            if (_text[i] == '\n')
            {
                _lineStarts.Add(i + 1);
            }
        }

        _lineStartsDirty = false;
    }

    internal List<int> GetLineStarts()
    {
        EnsureLineStarts();
        return _lineStarts!;
    }

    internal (int line, int column) GetLineAndColumn(int position)
    {
        var starts = GetLineStarts();
        var line = 0;
        for (var i = 1; i < starts.Count; i++)
        {
            if (starts[i] > position)
            {
                break;
            }
            line = i;
        }

        return (line, position - starts[line]);
    }

    internal int GetPositionFromLineColumn(int line, int column)
    {
        var starts = GetLineStarts();
        line = Math.Clamp(line, 0, starts.Count - 1);
        var lineStart = starts[line];
        var lineEnd = line + 1 < starts.Count ? starts[line + 1] - 1 : _text.Length;
        column = Math.Clamp(column, 0, lineEnd - lineStart);
        return lineStart + column;
    }

    internal int GetLineLength(int line)
    {
        var starts = GetLineStarts();
        if (line < 0 || line >= starts.Count)
        {
            return 0;
        }

        var lineStart = starts[line];
        var lineEnd = line + 1 < starts.Count ? starts[line + 1] - 1 : _text.Length;
        return lineEnd - lineStart;
    }

    internal int GetLineStart(int line)
    {
        var starts = GetLineStarts();
        if (line < 0 || line >= starts.Count)
        {
            return 0;
        }
        return starts[line];
    }

    internal int GetLineEnd(int line)
    {
        var starts = GetLineStarts();
        if (line < 0 || line >= starts.Count)
        {
            return _text.Length;
        }
        return line + 1 < starts.Count ? starts[line + 1] - 1 : _text.Length;
    }

    internal string GetLineString(int line)
    {
        EnsureLineStrings();
        if (line < 0 || line >= _cachedLineCount)
        {
            return "";
        }
        return _cachedLineStrings[line] ?? "";
    }

    private void EnsureLineStrings()
    {
        if (!_lineStringsDirty)
        {
            return;
        }

        var starts = GetLineStarts();
        _cachedLineCount = starts.Count;

        if (_cachedLineStrings.Length < _cachedLineCount)
        {
            Array.Resize(ref _cachedLineStrings, Math.Max(_cachedLineCount, _cachedLineStrings.Length * 2));
        }

        for (var i = 0; i < _cachedLineCount; i++)
        {
            var lineStart = starts[i];
            var lineEnd = i + 1 < starts.Count ? starts[i + 1] - 1 : _text.Length;
            var lineLength = lineEnd - lineStart;
            _cachedLineStrings[i] = lineLength > 0 ? _text.ToString(lineStart, lineLength) : "";
        }

        for (var i = _cachedLineCount; i < _cachedLineStrings.Length; i++)
        {
            _cachedLineStrings[i] = null;
        }

        _lineStringsDirty = false;
    }

    internal void GetTextSegments(int splitPos, out string before, out string after)
    {
        if (_cachedCursorSegmentPos == splitPos)
        {
            before = _cachedBeforeCursor;
            after = _cachedAfterCursor;
            return;
        }

        var text = Text;
        if (splitPos <= 0)
        {
            _cachedBeforeCursor = "";
            _cachedAfterCursor = text;
        }
        else if (splitPos >= text.Length)
        {
            _cachedBeforeCursor = text;
            _cachedAfterCursor = "";
        }
        else
        {
            _cachedBeforeCursor = text[..splitPos];
            _cachedAfterCursor = text[splitPos..];
        }

        _cachedCursorSegmentPos = splitPos;
        before = _cachedBeforeCursor;
        after = _cachedAfterCursor;
    }

    internal void GetSelectionSegments(int selStart, int selEnd, out string beforeSel, out string selected, out string afterSel)
    {
        if (_cachedSelStart == selStart && _cachedSelEnd == selEnd)
        {
            beforeSel = _cachedBeforeSel;
            selected = _cachedSelected;
            afterSel = _cachedAfterSel;
            return;
        }

        var text = Text;
        _cachedBeforeSel = selStart > 0 ? text[..selStart] : "";
        _cachedSelected = selEnd > selStart ? text[selStart..selEnd] : "";
        _cachedAfterSel = selEnd < text.Length ? text[selEnd..] : "";

        _cachedSelStart = selStart;
        _cachedSelEnd = selEnd;
        beforeSel = _cachedBeforeSel;
        selected = _cachedSelected;
        afterSel = _cachedAfterSel;
    }

    internal void GetLineSegments(int line, int splitCol, out string before, out string after)
    {
        if (_cachedLineCursorLine == line && _cachedLineCursorCol == splitCol)
        {
            before = _cachedLineBeforeCursor;
            after = _cachedLineAfterCursor;
            return;
        }

        var lineStr = GetLineString(line);
        if (splitCol <= 0)
        {
            _cachedLineBeforeCursor = "";
            _cachedLineAfterCursor = lineStr;
        }
        else if (splitCol >= lineStr.Length)
        {
            _cachedLineBeforeCursor = lineStr;
            _cachedLineAfterCursor = "";
        }
        else
        {
            _cachedLineBeforeCursor = lineStr[..splitCol];
            _cachedLineAfterCursor = lineStr[splitCol..];
        }

        _cachedLineCursorLine = line;
        _cachedLineCursorCol = splitCol;
        before = _cachedLineBeforeCursor;
        after = _cachedLineAfterCursor;
    }

    internal void GetLineSelectionSegments(int line, int selStart, int selEnd, out string beforeSel, out string selected, out string afterSel)
    {
        if (_cachedLineSelLine == line && _cachedLineSelStart == selStart && _cachedLineSelEnd == selEnd)
        {
            beforeSel = _cachedLineBeforeSel;
            selected = _cachedLineSelected;
            afterSel = _cachedLineAfterSel;
            return;
        }

        var lineStr = GetLineString(line);
        _cachedLineBeforeSel = selStart > 0 && selStart <= lineStr.Length ? lineStr[..selStart] : "";
        _cachedLineSelected = selEnd > selStart ? lineStr[Math.Max(0, selStart)..Math.Min(selEnd, lineStr.Length)] : "";
        _cachedLineAfterSel = selEnd < lineStr.Length ? lineStr[selEnd..] : "";

        _cachedLineSelLine = line;
        _cachedLineSelStart = selStart;
        _cachedLineSelEnd = selEnd;
        beforeSel = _cachedLineBeforeSel;
        selected = _cachedLineSelected;
        afterSel = _cachedLineAfterSel;
    }
}

public ref struct UiTextField
{
    private readonly UiContext _context;
    private readonly UiTextFieldState _state;

    private UiColor _backgroundColor;
    private UiColor _focusedBackgroundColor;
    private UiColor _textColor;
    private UiColor _placeholderColor;
    private UiColor _cursorColor;
    private UiColor _selectionColor;
    private UiColor _selectionTextColor;
    private UiColor _borderColor;
    private UiColor _focusedBorderColor;
    private float _borderWidth;
    private float _cornerRadius;
    private UiPadding _padding;
    private ushort _fontSize;
    private UiSizing _width;
    private UiSizing _height;
    private bool _multiline;
    private int _maxLength;
    private bool _readOnly;
    private string _placeholder;
    private readonly float _cursorBlinkRate;
    private float _cursorWidth;
    private UiTextOverflow _overflow;

    internal UiTextField(UiContext ctx, string id, UiTextFieldState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _backgroundColor = UiColor.Rgb(45, 45, 48);
        _focusedBackgroundColor = UiColor.Rgb(55, 55, 58);
        _textColor = UiColor.White;
        _placeholderColor = UiColor.Rgb(128, 128, 128);
        _cursorColor = UiColor.White;
        _selectionColor = UiColor.Rgb(51, 153, 255);
        _selectionTextColor = UiColor.White;
        _borderColor = UiColor.Rgb(70, 70, 75);
        _focusedBorderColor = UiColor.Rgb(100, 149, 237);
        _borderWidth = 1;
        _cornerRadius = 4;
        _padding = UiPadding.Symmetric(8, 6);
        _fontSize = 14;
        _width = UiSizing.Fixed(200);
        _height = UiSizing.Fit();
        _multiline = false;
        _maxLength = 0;
        _readOnly = false;
        _placeholder = "";
        _cursorBlinkRate = 0.53f;
        _cursorWidth = 2;
        _overflow = UiTextOverflow.Clip;
    }

    public uint Id { get; }

    public bool IsFocused => _context.FocusedTextFieldId == Id;

    private float GetLineHeight()
    {
        return _fontSize + 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField BackgroundColor(UiColor color)
    {
        _backgroundColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField BackgroundColor(UiColor normal, UiColor focused)
    {
        _backgroundColor = normal;
        _focusedBackgroundColor = focused;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField TextColor(UiColor color)
    {
        _textColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField PlaceholderColor(UiColor color)
    {
        _placeholderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField CursorColor(UiColor color)
    {
        _cursorColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField SelectionColor(UiColor background, UiColor text)
    {
        _selectionColor = background;
        _selectionTextColor = text;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField BorderColor(UiColor color)
    {
        _borderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField BorderColor(UiColor normal, UiColor focused)
    {
        _borderColor = normal;
        _focusedBorderColor = focused;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Border(float width, UiColor color)
    {
        _borderWidth = width;
        _borderColor = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField CornerRadius(float radius)
    {
        _cornerRadius = radius;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField FontSize(ushort size)
    {
        _fontSize = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Padding(float all)
    {
        _padding = UiPadding.All(all);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Padding(float h, float v)
    {
        _padding = UiPadding.Symmetric(h, v);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Size(float w, float h)
    {
        _width = UiSizing.Fixed(w);
        _height = UiSizing.Fixed(h);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Width(float w)
    {
        _width = UiSizing.Fixed(w);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Width(UiSizing s)
    {
        _width = s;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Height(float h)
    {
        _height = UiSizing.Fixed(h);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Height(UiSizing s)
    {
        _height = s;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField GrowWidth()
    {
        _width = UiSizing.Grow();
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Multiline(bool m = true)
    {
        _multiline = m;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField MaxLength(int max)
    {
        _maxLength = max;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField ReadOnly(bool r = true)
    {
        _readOnly = r;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Placeholder(string p)
    {
        _placeholder = p;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField CursorWidth(float w)
    {
        _cursorWidth = w;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiTextField Overflow(UiTextOverflow overflow)
    {
        _overflow = overflow;
        return this;
    }

    public bool Show(ref string text, float deltaTime = 0)
    {
        if (_state.Text != text)
        {
            _state.Text = text;
        }

        var textChanged = false;
        var interaction = _context.GetInteraction(Id);
        var isFocused = _context.FocusedTextFieldId == Id;

        if (interaction.WasClicked)
        {
            if (!isFocused)
            {
                _context.FocusedTextFieldId = Id;
                isFocused = true;
                _state.ResetCursorBlink();
                InputSystem.StartTextInput();
            }
        }

        if (isFocused)
        {
            foreach (var ev in _context.FrameEvents)
            {
                if (ProcessEvent(ev))
                {
                    textChanged = true;
                }
            }

            if (deltaTime > 0)
            {
                _state.CursorBlinkTime += deltaTime;
                if (_state.CursorBlinkTime >= _cursorBlinkRate)
                {
                    _state.CursorVisible = !_state.CursorVisible;
                    _state.CursorBlinkTime = 0;
                }
            }
        }

        RenderTextField(isFocused);

        if (textChanged)
        {
            text = _state.Text;
        }

        return textChanged;
    }

    public bool Show(ref string text)
    {
        return Show(ref text, 0);
    }

    private void RenderTextField(bool isFocused)
    {
        var bgColor = isFocused ? _focusedBackgroundColor : _backgroundColor;
        var borderColor = isFocused ? _focusedBorderColor : _borderColor;
        _state.PaddingLeft = _padding.Left;
        _state.FontSize = _fontSize;

        var lineHeight = GetLineHeight();

        var decl = new ClayElementDeclaration { Id = Id };
        decl.Layout.Sizing.Width = _width.ToClayAxis();
        decl.Layout.Sizing.Height = _height.ToClayAxis();
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.BackgroundColor = bgColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_multiline)
        {
            decl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Top;
        }
        else
        {
            decl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        }

        if (_borderWidth > 0)
        {
            decl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_borderWidth),
                Color = borderColor.ToClayColor()
            };
        }

        _context.OpenElement(decl);
        {
            var text = _state.Text;
            var isEmpty = string.IsNullOrEmpty(text);

            if (isEmpty && !isFocused)
            {
                RenderPlaceholder(lineHeight);
            }
            else if (_multiline)
            {
                RenderMultilineText(isFocused, lineHeight);
            }
            else
            {
                RenderSingleLineText(isFocused, lineHeight);
            }
        }
        _context.Clay.CloseElement();

        var boundingBox = _context.Clay.GetElementBoundingBox(Id);
        _state.BoundingBoxX = boundingBox.X;
        _state.BoundingBoxY = boundingBox.Y;
        _state.BoundingBoxWidth = boundingBox.Width;
        _state.BoundingBoxHeight = boundingBox.Height;

        var contentId = _context.StringCache.GetId("TFContent", Id);
        var contentBox = _context.Clay.GetElementBoundingBox(contentId);
        _state.ContentBoundingBoxX = contentBox.X;
        _state.ContentBoundingBoxY = contentBox.Y;
        _state.ContentBoundingBoxWidth = contentBox.Width;
        _state.ContentBoundingBoxHeight = contentBox.Height;
    }

    private void RenderPlaceholder(float lineHeight)
    {
        var contentId = _context.StringCache.GetId("TFContent", Id);
        var wrapperDecl = new ClayElementDeclaration { Id = contentId };
        wrapperDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        wrapperDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(lineHeight, float.MaxValue);
        wrapperDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        _context.OpenElement(wrapperDecl);
        {
            if (!string.IsNullOrEmpty(_placeholder))
            {
                _context.Clay.Text(StringView.Intern(_placeholder),
                    new ClayTextDesc { TextColor = _placeholderColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
            }
        }
        _context.Clay.CloseElement();
    }

    private void RenderSingleLineText(bool isFocused, float lineHeight)
    {
        var text = _state.Text;
        var cursorPos = _state.CursorPosition;
        var hasSelection = _state.HasSelection && isFocused;

        var contentId = _context.StringCache.GetId("TFContent", Id);
        var contentDecl = new ClayElementDeclaration { Id = contentId };
        contentDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        contentDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        contentDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        contentDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(lineHeight, float.MaxValue);

        if (_overflow == UiTextOverflow.Scroll)
        {
            contentDecl.Scroll.Horizontal = true;
        }

        _context.OpenElement(contentDecl);

        if (hasSelection)
        {
            RenderTextWithSelection(text);
        }
        else if (!string.IsNullOrEmpty(text))
        {
            _context.Clay.Text(StringView.Intern(text),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
        }

        if (isFocused)
        {
            RenderCursor(text, cursorPos, lineHeight);
        }

        _context.Clay.CloseElement();
    }

    private void RenderTextWithSelection(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var selStart = _state.SelectionStart;
        var selEnd = _state.SelectionEnd;

        _state.GetSelectionSegments(selStart, selEnd, out var beforeSel, out var selected, out var afterSel);

        if (!string.IsNullOrEmpty(beforeSel))
        {
            _context.Clay.Text(StringView.Intern(beforeSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
        }

        if (!string.IsNullOrEmpty(selected))
        {
            var selDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFSelBox", Id) };
            selDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            selDecl.BackgroundColor = _selectionColor.ToClayColor();
            _context.OpenElement(selDecl);
            _context.Clay.Text(StringView.Intern(selected),
                new ClayTextDesc { TextColor = _selectionTextColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
            _context.Clay.CloseElement();
        }

        if (!string.IsNullOrEmpty(afterSel))
        {
            _context.Clay.Text(StringView.Intern(afterSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
        }
    }

    private void RenderCursor(string text, int cursorPos, float lineHeight)
    {
        var textView = StringView.Intern(text ?? "");
        var cursorPixelX = _context.Clay.GetCursorOffsetAtIndex(textView, (uint)cursorPos, 0, _fontSize);
        var cursorOffsetX = _context.Clay.PixelsToPoints(cursorPixelX);

        var cursorColor = _state.CursorVisible ? _cursorColor : UiColor.Transparent;

        var cursorDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFCursor", Id) };
        cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_cursorWidth);
        cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(lineHeight);
        cursorDecl.BackgroundColor = cursorColor.ToClayColor();
        cursorDecl.Floating = new ClayFloatingDesc
        {
            AttachTo = ClayFloatingAttachTo.Parent,
            ParentAttachPoint = ClayFloatingAttachPoint.LeftCenter,
            ElementAttachPoint = ClayFloatingAttachPoint.LeftCenter,
            Offset = new Vector2 { X = cursorOffsetX, Y = 0 },
            ZIndex = 100
        };

        _context.OpenElement(cursorDecl);
        _context.Clay.CloseElement();
    }

    private void RenderMultilineText(bool isFocused, float lineHeight)
    {
        var cursorPos = _state.CursorPosition;
        var (cursorLine, cursorCol) = _state.GetLineAndColumn(cursorPos);
        var hasSelection = _state.HasSelection && isFocused;
        var selStart = _state.SelectionStart;
        var selEnd = _state.SelectionEnd;
        var lineCount = Math.Max(1, _state.LineCount);

        var contentId = _context.StringCache.GetId("TFContent", Id);
        var contentDecl = new ClayElementDeclaration { Id = contentId };
        contentDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        contentDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        contentDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        _context.OpenElement(contentDecl);

        for (var lineIdx = 0; lineIdx < lineCount; lineIdx++)
        {
            var lineStart = _state.GetLineStart(lineIdx);
            var lineEnd = _state.GetLineEnd(lineIdx);
            var lineStr = _state.GetLineString(lineIdx);

            var cursorOnThisLine = cursorLine == lineIdx && isFocused;
            var selectionOnThisLine = hasSelection && selStart <= lineEnd && selEnd >= lineStart;

            var lineId = _context.StringCache.GetId("TFLine", Id, (uint)lineIdx);
            var lineDecl = new ClayElementDeclaration { Id = lineId };
            lineDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            lineDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            lineDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(lineHeight, float.MaxValue);
            lineDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            _context.OpenElement(lineDecl);

            if (selectionOnThisLine)
            {
                var lineSelStart = Math.Max(0, selStart - lineStart);
                var lineSelEnd = Math.Min(lineStr.Length, selEnd - lineStart);
                RenderLineWithInlineSelection(lineStr, lineSelStart, lineSelEnd, lineIdx);
            }
            else if (!string.IsNullOrEmpty(lineStr))
            {
                _context.Clay.Text(StringView.Intern(lineStr),
                    new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
            }

            _context.Clay.CloseElement();

            if (cursorOnThisLine)
            {
                RenderFloatingLineCursor(lineId, lineStr, cursorCol, lineHeight, lineIdx);
            }
        }

        _context.Clay.CloseElement();
    }

    private void RenderFloatingLineCursor(uint lineId, string lineStr, int cursorCol, float lineHeight, int lineIdx)
    {
        var textView = StringView.Intern(lineStr ?? "");
        var cursorPixelX = _context.Clay.GetCursorOffsetAtIndex(textView, (uint)cursorCol, 0, _fontSize);
        var cursorOffsetX = _context.Clay.PixelsToPoints(cursorPixelX);
        var cursorColor = _state.CursorVisible ? _cursorColor : UiColor.Transparent;

        var cursorDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFLineCur", Id, (uint)lineIdx) };
        cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_cursorWidth);
        cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(lineHeight);
        cursorDecl.BackgroundColor = cursorColor.ToClayColor();
        cursorDecl.Floating = new ClayFloatingDesc
        {
            AttachTo = ClayFloatingAttachTo.ElementWithId,
            ParentId = lineId,
            ParentAttachPoint = ClayFloatingAttachPoint.LeftCenter,
            ElementAttachPoint = ClayFloatingAttachPoint.LeftCenter,
            Offset = new Vector2 { X = cursorOffsetX, Y = 0 },
            ZIndex = 100
        };

        _context.OpenElement(cursorDecl);
        _context.Clay.CloseElement();
    }

    private void RenderLineWithInlineSelection(string lineStr, int selStart, int selEnd, int lineIdx)
    {
        _state.GetLineSelectionSegments(lineIdx, selStart, selEnd, out var beforeSel, out var selected, out var afterSel);

        if (!string.IsNullOrEmpty(beforeSel))
        {
            _context.Clay.Text(StringView.Intern(beforeSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
        }

        if (!string.IsNullOrEmpty(selected))
        {
            var selDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFLineSelBox", Id, (uint)lineIdx) };
            selDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            selDecl.BackgroundColor = _selectionColor.ToClayColor();
            _context.OpenElement(selDecl);
            _context.Clay.Text(StringView.Intern(selected),
                new ClayTextDesc { TextColor = _selectionTextColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
            _context.Clay.CloseElement();
        }

        if (!string.IsNullOrEmpty(afterSel))
        {
            _context.Clay.Text(StringView.Intern(afterSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize, WrapMode = ClayTextWrapMode.None });
        }
    }

    private bool ProcessEvent(Event ev)
    {
        switch (ev.Type)
        {
            case EventType.MouseButtonDown when ev.MouseButton.Button == MouseButton.Left:
                return HandleMouseDown(ev);

            case EventType.MouseButtonUp when ev.MouseButton.Button == MouseButton.Left:
                _state.IsDragging = false;
                _state.IsSelecting = false;
                break;

            case EventType.MouseMotion:
                if (_state.IsDragging)
                {
                    HandleMouseDrag(ev);
                }
                _state.LastMouseX = ev.MouseMotion.X;
                _state.LastMouseY = ev.MouseMotion.Y;
                break;

            case EventType.KeyDown:
                return HandleKeyDown(ev);

            case EventType.KeyUp:
                break;

            case EventType.TextInput when !_readOnly:
                var inputText = ev.Text.Text.ToString();
                if (!string.IsNullOrEmpty(inputText) && !char.IsControl(inputText[0]))
                {
                    _state.InsertText(inputText, _maxLength > 0 ? _maxLength : null);
                    _state.ResetCursorBlink();
                    return true;
                }
                break;
        }

        return false;
    }

    private bool HandleMouseDown(Event ev)
    {
        var isOverTextField = _context.Clay.PointerOver(Id);

        if (isOverTextField)
        {
            _state.LastMouseX = ev.MouseButton.X;
            _state.LastMouseY = ev.MouseButton.Y;
            _state.IsDragging = true;

            var clickPos = GetCursorPositionFromMouse(ev.MouseButton.X, ev.MouseButton.Y);
            var shiftHeld = InputSystem.IsKeyPressed(KeyCode.Lshift) || InputSystem.IsKeyPressed(KeyCode.Rshift);

            if (shiftHeld)
            {
                _state.StartSelection();
                _state.CursorPosition = clickPos;
            }
            else
            {
                _state.ClearSelection();
                _state.CursorPosition = clickPos;
            }

            _state.ResetCursorBlink();
            return false;
        }

        _context.FocusedTextFieldId = 0;
        _state.ClearSelection();
        _state.IsDragging = false;
        InputSystem.StopTextInput();
        return false;
    }

    private void HandleMouseDrag(Event ev)
    {
        _state.LastMouseX = ev.MouseMotion.X;
        _state.LastMouseY = ev.MouseMotion.Y;

        var dragPos = GetCursorPositionFromMouse(ev.MouseMotion.X, ev.MouseMotion.Y);

        if (!_state.HasSelection && dragPos != _state.CursorPosition)
        {
            _state.SelectionAnchor = _state.CursorPosition;
        }

        _state.CursorPosition = dragPos;
        _state.ResetCursorBlink();
    }

    private bool HandleKeyDown(Event ev)
    {
        var key = ev.Key.KeyCode;
        var ctrl = InputSystem.IsKeyPressed(KeyCode.Lctrl) || InputSystem.IsKeyPressed(KeyCode.Rctrl);
        var shift = InputSystem.IsKeyPressed(KeyCode.Lshift) || InputSystem.IsKeyPressed(KeyCode.Rshift);
        var changed = false;

        switch (key)
        {
            case KeyCode.Left:
                HandleLeftKey(ctrl, shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.Right:
                HandleRightKey(ctrl, shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.Up when _multiline:
                HandleUpKey(shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.Down when _multiline:
                HandleDownKey(shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.Home:
                HandleHomeKey(ctrl, shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.End:
                HandleEndKey(ctrl, shift);
                _state.ResetCursorBlink();
                break;

            case KeyCode.Backspace when !_readOnly:
                if (_state.HasSelection)
                {
                    _state.DeleteSelection();
                }
                else if (_state.CursorPosition > 0)
                {
                    _state.DeleteBack();
                }
                _state.ResetCursorBlink();
                changed = true;
                break;

            case KeyCode.Delete when !_readOnly:
                if (_state.HasSelection)
                {
                    _state.DeleteSelection();
                }
                else
                {
                    _state.DeleteForward();
                }
                _state.ResetCursorBlink();
                changed = true;
                break;

            case KeyCode.Return when !_readOnly:
                if (_multiline)
                {
                    _state.InsertText("\n", _maxLength > 0 ? _maxLength : null);
                    changed = true;
                }
                else
                {
                    _context.FocusedTextFieldId = 0;
                    InputSystem.StopTextInput();
                }
                _state.ResetCursorBlink();
                break;

            case KeyCode.Tab when !_readOnly && _multiline:
                _state.InsertText("    ", _maxLength > 0 ? _maxLength : null);
                _state.ResetCursorBlink();
                changed = true;
                break;

            case KeyCode.Escape:
                _context.FocusedTextFieldId = 0;
                _state.ClearSelection();
                InputSystem.StopTextInput();
                break;

            case KeyCode.A when ctrl:
                _state.SelectAll();
                _state.ResetCursorBlink();
                break;

            case KeyCode.C when ctrl:
                if (_state.HasSelection)
                {
                    Clipboard.SetText(StringView.Intern(_state.SelectedText));
                }
                break;

            case KeyCode.X when ctrl && !_readOnly:
                if (_state.HasSelection)
                {
                    Clipboard.SetText(StringView.Intern(_state.SelectedText));
                    _state.DeleteSelection();
                    changed = true;
                }
                _state.ResetCursorBlink();
                break;

            case KeyCode.V when ctrl && !_readOnly:
                if (Clipboard.HasText())
                {
                    var clipText = Clipboard.GetText().ToString();
                    if (!string.IsNullOrEmpty(clipText))
                    {
                        if (!_multiline)
                        {
                            clipText = clipText.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        }
                        _state.InsertText(clipText, _maxLength > 0 ? _maxLength : null);
                        changed = true;
                    }
                }
                _state.ResetCursorBlink();
                break;
        }

        return changed;
    }

    private void HandleLeftKey(bool ctrl, bool shift)
    {
        if (shift)
        {
            _state.StartSelection();
            if (ctrl)
            {
                MoveCursorWordLeft();
            }
            else if (_state.CursorPosition > 0)
            {
                _state.CursorPosition--;
            }
        }
        else
        {
            if (_state.HasSelection)
            {
                _state.CursorPosition = _state.SelectionStart;
                _state.ClearSelection();
            }
            else if (ctrl)
            {
                MoveCursorWordLeft();
            }
            else if (_state.CursorPosition > 0)
            {
                _state.CursorPosition--;
            }
        }
        _state.PreferredColumn = -1;
    }

    private void HandleRightKey(bool ctrl, bool shift)
    {
        if (shift)
        {
            _state.StartSelection();
            if (ctrl)
            {
                MoveCursorWordRight();
            }
            else if (_state.CursorPosition < _state.Length)
            {
                _state.CursorPosition++;
            }
        }
        else
        {
            if (_state.HasSelection)
            {
                _state.CursorPosition = _state.SelectionEnd;
                _state.ClearSelection();
            }
            else if (ctrl)
            {
                MoveCursorWordRight();
            }
            else if (_state.CursorPosition < _state.Length)
            {
                _state.CursorPosition++;
            }
        }
        _state.PreferredColumn = -1;
    }

    private void HandleUpKey(bool shift)
    {
        if (shift)
        {
            _state.StartSelection();
        }
        else
        {
            _state.ClearSelection();
        }
        MoveCursorUp();
    }

    private void HandleDownKey(bool shift)
    {
        if (shift)
        {
            _state.StartSelection();
        }
        else
        {
            _state.ClearSelection();
        }
        MoveCursorDown();
    }

    private void HandleHomeKey(bool ctrl, bool shift)
    {
        var targetPos = ctrl ? 0 : GetLineStartPosition(_state.CursorPosition);
        if (shift)
        {
            _state.StartSelection();
            _state.CursorPosition = targetPos;
        }
        else
        {
            _state.ClearSelection();
            _state.CursorPosition = targetPos;
        }
        _state.PreferredColumn = -1;
    }

    private void HandleEndKey(bool ctrl, bool shift)
    {
        var targetPos = ctrl ? _state.Length : GetLineEndPosition(_state.CursorPosition);

        if (shift)
        {
            _state.StartSelection();
            _state.CursorPosition = targetPos;
        }
        else
        {
            _state.ClearSelection();
            _state.CursorPosition = targetPos;
        }
        _state.PreferredColumn = -1;
    }

    private int GetLineStartPosition(int position)
    {
        var (line, _) = _state.GetLineAndColumn(position);
        return _state.GetLineStart(line);
    }

    private int GetLineEndPosition(int position)
    {
        var (line, _) = _state.GetLineAndColumn(position);
        return _state.GetLineEnd(line);
    }

    private void MoveCursorWordLeft()
    {
        while (_state.CursorPosition > 0 && char.IsWhiteSpace(_state.GetChar(_state.CursorPosition - 1)))
        {
            _state.CursorPosition--;
        }

        while (_state.CursorPosition > 0 && !char.IsWhiteSpace(_state.GetChar(_state.CursorPosition - 1)))
        {
            _state.CursorPosition--;
        }
    }

    private void MoveCursorWordRight()
    {
        while (_state.CursorPosition < _state.Length && !char.IsWhiteSpace(_state.GetChar(_state.CursorPosition)))
        {
            _state.CursorPosition++;
        }

        while (_state.CursorPosition < _state.Length && char.IsWhiteSpace(_state.GetChar(_state.CursorPosition)))
        {
            _state.CursorPosition++;
        }
    }

    private void MoveCursorUp()
    {
        var (line, col) = _state.GetLineAndColumn(_state.CursorPosition);

        if (line > 0)
        {
            if (_state.PreferredColumn < 0)
            {
                _state.PreferredColumn = col;
            }
            _state.CursorPosition = _state.GetPositionFromLineColumn(line - 1, _state.PreferredColumn);
        }
    }

    private void MoveCursorDown()
    {
        var (line, col) = _state.GetLineAndColumn(_state.CursorPosition);

        if (line < _state.LineCount - 1)
        {
            if (_state.PreferredColumn < 0)
            {
                _state.PreferredColumn = col;
            }
            _state.CursorPosition = _state.GetPositionFromLineColumn(line + 1, _state.PreferredColumn);
        }
    }

    private int GetCursorPositionFromMouse(float mouseX, float mouseY)
    {
        var contentX = _state.ContentBoundingBoxX;
        var contentY = _state.ContentBoundingBoxY;

        if (contentX == 0 && contentY == 0)
        {
            var scaledPaddingLeft = _context.Clay.PointsToPixels(_padding.Left);
            var scaledPaddingTop = _context.Clay.PointsToPixels(_padding.Top);
            contentX = _state.BoundingBoxX + scaledPaddingLeft;
            contentY = _state.BoundingBoxY + scaledPaddingTop;
        }

        var clickX = mouseX - contentX + _state.ScrollOffsetX;
        var clickY = mouseY - contentY;

        if (_multiline)
        {
            var scaledLineHeight = _context.Clay.PointsToPixels(GetLineHeight());
            var lineIdx = Math.Max(0, (int)(clickY / scaledLineHeight));
            lineIdx = Math.Min(lineIdx, Math.Max(0, _state.LineCount - 1));

            var lineStr = _state.GetLineString(lineIdx);
            var textView = StringView.Intern(lineStr ?? "");
            var col = (int)_context.Clay.GetCharIndexAtOffset(textView, clickX, 0, _fontSize);
            return _state.GetPositionFromLineColumn(lineIdx, col);
        }
        else
        {
            var text = _state.Text;
            var textView = StringView.Intern(text ?? "");
            return (int)_context.Clay.GetCharIndexAtOffset(textView, clickX, 0, _fontSize);
        }
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTextField TextField(UiContext ctx, string id, ref string text)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiTextFieldState>(elementId);
        if (state.Text != text)
        {
            state.Text = text;
        }

        return new UiTextField(ctx, id, state);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiTextField TextField(this ref UiElementScope scope, UiContext ctx, string id, ref string text)
    {
        return Ui.TextField(ctx, id, ref text);
    }
}
