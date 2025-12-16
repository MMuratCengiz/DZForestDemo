using System.Runtime.CompilerServices;
using System.Text;
using DenOfIz;

namespace UIFramework;

[Flags]
public enum UiKeyMod : uint
{
    None = 0x0000,
    LShift = 0x0001,
    RShift = 0x0002,
    LCtrl = 0x0040,
    RCtrl = 0x0080,
    LAlt = 0x0100,
    RAlt = 0x0200,
    LGui = 0x0400,
    RGui = 0x0800,
    Num = 0x1000,
    Caps = 0x2000,
    Mode = 0x4000,
    Scroll = 0x8000,

    Ctrl = LCtrl | RCtrl,
    Shift = LShift | RShift,
    Alt = LAlt | RAlt,
    Gui = LGui | RGui
}

public sealed class UiTextFieldState
{
    private readonly StringBuilder _text = new();
    private List<int>? _lineStarts;
    private bool _lineStartsDirty = true;

    public int CursorPosition { get; set; }
    public int SelectionAnchor { get; set; } = -1;
    public bool IsSelecting { get; set; }
    public bool ShiftHeld { get; set; }
    public float CursorBlinkTime { get; set; }
    public bool CursorVisible { get; set; } = true;

    public bool HasSelection => SelectionAnchor >= 0 && SelectionAnchor != CursorPosition;
    public int SelectionStart => HasSelection ? Math.Min(SelectionAnchor, CursorPosition) : CursorPosition;
    public int SelectionEnd => HasSelection ? Math.Max(SelectionAnchor, CursorPosition) : CursorPosition;
    public int Length => _text.Length;

    public string Text
    {
        get => _text.ToString();
        set
        {
            _text.Clear();
            _text.Append(value ?? "");
            CursorPosition = Math.Min(CursorPosition, _text.Length);
            ClearSelection();
            _lineStartsDirty = true;
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

            var start = Math.Min(SelectionStart, SelectionEnd);
            var end = Math.Max(SelectionStart, SelectionEnd);
            return _text.ToString(start, end - start);
        }
    }

    public float PaddingLeft { get; set; }
    public float FontSize { get; set; }

    internal int LineCount => GetLineStarts().Count;

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
        _lineStartsDirty = true;
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
        _lineStartsDirty = true;
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
            _lineStartsDirty = true;
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
            _lineStartsDirty = true;
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

    internal List<int> GetLineStarts()
    {
        if (!_lineStartsDirty && _lineStarts != null)
        {
            return _lineStarts;
        }

        _lineStarts ??= new List<int>();
        _lineStarts.Clear();
        _lineStarts.Add(0);
        for (var i = 0; i < _text.Length; i++)
            if (_text[i] == '\n')
            {
                _lineStarts.Add(i + 1);
            }

        _lineStartsDirty = false;
        return _lineStarts;
    }

    internal (int line, int column) GetLineAndColumn(int position)
    {
        var starts = GetLineStarts();
        var line = 0;
        for (var i = 1; i < starts.Count; i++)
            if (starts[i] > position)
            {
                break;
            }
            else
            {
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

    internal UiTextField(UiContext ctx, string id, UiTextFieldState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _backgroundColor = UiColor.Rgb(45, 45, 48);
        _focusedBackgroundColor = UiColor.Rgb(55, 55, 58);
        _textColor = UiColor.White;
        _placeholderColor = UiColor.Gray;
        _cursorColor = UiColor.White;
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
        _cursorBlinkRate = 0.5f;
    }

    public uint Id { get; }

    public bool IsFocused => _context.FocusedTextFieldId == Id;

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

        var decl = new ClayElementDeclaration { Id = Id };
        decl.Layout.Sizing.Width = _width.ToClayAxis();
        var minHeight = _fontSize + _padding.Top + _padding.Bottom + 4;
        decl.Layout.Sizing.Height = ClaySizingAxis.Fit(minHeight, float.MaxValue);
        decl.Layout.Padding = _padding.ToClayPadding();
        decl.Layout.LayoutDirection = _multiline ? ClayLayoutDirection.TopToBottom : ClayLayoutDirection.LeftToRight;
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        decl.BackgroundColor = bgColor.ToClayColor();
        decl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);

        if (_borderWidth > 0)
        {
            decl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform((uint)_borderWidth),
                Color = borderColor.ToClayColor()
            };
        }

        _context.Clay.OpenElement(decl);
        {
            var text = _state.Text;
            var isEmpty = string.IsNullOrEmpty(text);

            if (isEmpty && !isFocused)
            {
                RenderPlaceholderOrEmpty();
            }
            else if (_multiline)
            {
                RenderMultilineText(text, isFocused);
            }
            else
            {
                RenderSingleLineText(text, isFocused);
            }
        }
        _context.Clay.CloseElement();
    }

    private void RenderPlaceholderOrEmpty()
    {
        var displayText = !string.IsNullOrEmpty(_placeholder) ? _placeholder : " ";
        _context.Clay.Text(StringView.Intern(displayText),
            new ClayTextDesc { TextColor = _placeholderColor.ToClayColor(), FontSize = _fontSize });
    }

    private void RenderSingleLineText(string text, bool isFocused)
    {
        var cursorPos = _state.CursorPosition;
        var hasSelection = _state.HasSelection;
        var selStart = Math.Min(_state.SelectionStart, _state.SelectionEnd);
        var selEnd = Math.Max(_state.SelectionStart, _state.SelectionEnd);
        var rowDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFRow", Id) };
        rowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        rowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        _context.Clay.OpenElement(rowDecl);

        if (hasSelection && isFocused)
        {
            RenderTextWithSelection(text, selStart, selEnd, cursorPos, isFocused);
        }
        else
        {
            RenderTextWithCursor(text, cursorPos, isFocused);
        }

        _context.Clay.CloseElement();
    }

    private void RenderTextWithCursor(string text, int cursorPos, bool isFocused)
    {
        var beforeCursor = cursorPos > 0 ? text[..cursorPos] : "";
        var afterCursor = cursorPos < text.Length ? text[cursorPos..] : "";

        if (text.Length == 0)
        {
            _context.Clay.Text(StringView.Intern(" "),
                new ClayTextDesc { TextColor = UiColor.Transparent.ToClayColor(), FontSize = _fontSize });
        }

        if (beforeCursor.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(beforeCursor),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }

        if (isFocused && _state.CursorVisible)
        {
            var cursorDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFCursor", Id) };
            cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(2);
            cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            cursorDecl.BackgroundColor = _cursorColor.ToClayColor();
            _context.Clay.OpenElement(cursorDecl);
            _context.Clay.CloseElement();
        }

        if (afterCursor.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(afterCursor),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }
    }

    private void RenderTextWithSelection(string text, int selStart, int selEnd, int cursorPos, bool isFocused)
    {
        var beforeSel = selStart > 0 ? text[..selStart] : "";
        var selected = text[selStart..selEnd];
        var afterSel = selEnd < text.Length ? text[selEnd..] : "";
        if (beforeSel.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(beforeSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }

        if (isFocused && _state.CursorVisible && cursorPos == selStart)
        {
            RenderCursor();
        }

        var selBgDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFSelBg", Id) };
        selBgDecl.BackgroundColor = UiColor.Rgb(51, 153, 255).ToClayColor();
        selBgDecl.Layout.Padding = new ClayPadding { Left = 1, Right = 1 };
        _context.Clay.OpenElement(selBgDecl);
        _context.Clay.Text(StringView.Intern(selected),
            new ClayTextDesc { TextColor = UiColor.White.ToClayColor(), FontSize = _fontSize });
        _context.Clay.CloseElement();
        if (isFocused && _state.CursorVisible && cursorPos == selEnd)
        {
            RenderCursor();
        }

        if (afterSel.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(afterSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }
    }

    private void RenderCursor()
    {
        var cursorDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFCursor", Id) };
        cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(2);
        cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        cursorDecl.BackgroundColor = _cursorColor.ToClayColor();
        _context.Clay.OpenElement(cursorDecl);
        _context.Clay.CloseElement();
    }

    private void RenderMultilineText(string text, bool isFocused)
    {
        var lines = text.Split('\n');
        var cursorPos = _state.CursorPosition;
        var (cursorLine, cursorCol) = _state.GetLineAndColumn(cursorPos);
        var hasSelection = _state.HasSelection;
        var selStart = Math.Min(_state.SelectionStart, _state.SelectionEnd);
        var selEnd = Math.Max(_state.SelectionStart, _state.SelectionEnd);

        var charOffset = 0;
        for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            var line = lines[lineIdx];
            var lineStart = charOffset;
            var lineEnd = charOffset + line.Length;

            var lineDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFLine", Id + (uint)lineIdx) };
            lineDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            lineDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            lineDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            _context.Clay.OpenElement(lineDecl);

            var cursorOnThisLine = isFocused && cursorLine == lineIdx;
            var selectionOnThisLine = hasSelection && isFocused && selStart < lineEnd + 1 && selEnd > lineStart;

            if (selectionOnThisLine)
            {
                var lineSelStart = Math.Max(0, selStart - lineStart);
                var lineSelEnd = Math.Min(line.Length, selEnd - lineStart);
                var lineCursorPos = cursorOnThisLine ? cursorCol : -1;
                RenderLineWithSelection(line, lineSelStart, lineSelEnd, lineCursorPos, lineIdx);
            }
            else if (cursorOnThisLine)
            {
                RenderLineWithCursor(line, cursorCol, lineIdx);
            }
            else
            {
                var displayLine = line.Length > 0 ? line : " ";
                _context.Clay.Text(StringView.Intern(displayLine),
                    new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
            }

            _context.Clay.CloseElement();

            charOffset = lineEnd + 1;
        }

        if (isFocused && (text.EndsWith('\n') || text.Length == 0) && cursorPos == text.Length)
        {
            var emptyLineDecl = new ClayElementDeclaration { Id = _context.StringCache.GetId("TFLineEmpty", Id) };
            emptyLineDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            emptyLineDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            _context.Clay.OpenElement(emptyLineDecl);

            _context.Clay.Text(StringView.Intern(" "),
                new ClayTextDesc { TextColor = UiColor.Transparent.ToClayColor(), FontSize = _fontSize });

            if (_state.CursorVisible)
            {
                RenderCursor();
            }

            _context.Clay.CloseElement();
        }
    }

    private void RenderLineWithCursor(string line, int cursorCol, int lineIdx)
    {
        var beforeCursor = cursorCol > 0 && cursorCol <= line.Length ? line[..cursorCol] : "";
        var afterCursor = cursorCol < line.Length ? line[cursorCol..] : "";

        if (line.Length == 0)
        {
            _context.Clay.Text(StringView.Intern(" "),
                new ClayTextDesc { TextColor = UiColor.Transparent.ToClayColor(), FontSize = _fontSize });
        }

        if (beforeCursor.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(beforeCursor),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }

        if (_state.CursorVisible)
        {
            var cursorDecl = new ClayElementDeclaration
                { Id = _context.StringCache.GetId("TFLineCursor", Id + (uint)lineIdx) };
            cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(2);
            cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            cursorDecl.BackgroundColor = _cursorColor.ToClayColor();
            _context.Clay.OpenElement(cursorDecl);
            _context.Clay.CloseElement();
        }

        if (afterCursor.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(afterCursor),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }
    }

    private void RenderLineWithSelection(string line, int selStart, int selEnd, int cursorCol, int lineIdx)
    {
        var beforeSel = selStart > 0 ? line[..selStart] : "";
        var selected = selEnd > selStart ? line[selStart..Math.Min(selEnd, line.Length)] : "";
        var afterSel = selEnd < line.Length ? line[selEnd..] : "";

        if (line.Length == 0)
        {
            _context.Clay.Text(StringView.Intern(" "),
                new ClayTextDesc { TextColor = UiColor.Transparent.ToClayColor(), FontSize = _fontSize });
        }

        if (beforeSel.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(beforeSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }

        if (cursorCol == selStart && _state.CursorVisible)
        {
            var cursorDecl = new ClayElementDeclaration
                { Id = _context.StringCache.GetId("TFLineSelCursorS", Id + (uint)lineIdx) };
            cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(2);
            cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            cursorDecl.BackgroundColor = _cursorColor.ToClayColor();
            _context.Clay.OpenElement(cursorDecl);
            _context.Clay.CloseElement();
        }

        if (selected.Length > 0)
        {
            var selBgDecl = new ClayElementDeclaration
                { Id = _context.StringCache.GetId("TFLineSelBg", Id + (uint)lineIdx) };
            selBgDecl.BackgroundColor = UiColor.Rgb(51, 153, 255).ToClayColor();
            _context.Clay.OpenElement(selBgDecl);
            _context.Clay.Text(StringView.Intern(selected),
                new ClayTextDesc { TextColor = UiColor.White.ToClayColor(), FontSize = _fontSize });
            _context.Clay.CloseElement();
        }

        if (cursorCol == selEnd && cursorCol != selStart && _state.CursorVisible)
        {
            var cursorDecl = new ClayElementDeclaration
                { Id = _context.StringCache.GetId("TFLineSelCursorE", Id + (uint)lineIdx) };
            cursorDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(2);
            cursorDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            cursorDecl.BackgroundColor = _cursorColor.ToClayColor();
            _context.Clay.OpenElement(cursorDecl);
            _context.Clay.CloseElement();
        }

        if (afterSel.Length > 0)
        {
            _context.Clay.Text(StringView.Intern(afterSel),
                new ClayTextDesc { TextColor = _textColor.ToClayColor(), FontSize = _fontSize });
        }
    }

    private bool ProcessEvent(Event ev)
    {
        switch (ev.Type)
        {
            case EventType.MouseButtonDown when ev.MouseButton.Button == MouseButton.Left:
                return HandleMouseDown(ev);

            case EventType.MouseButtonUp when ev.MouseButton.Button == MouseButton.Left:
                _state.IsSelecting = false;
                break;

            case EventType.KeyDown:
                if (ev.Key.KeyCode is KeyCode.Lshift or KeyCode.Rshift)
                {
                    _state.ShiftHeld = true;
                }
                return HandleKeyDown(ev);

            case EventType.KeyUp:
                if (ev.Key.KeyCode is KeyCode.Lshift or KeyCode.Rshift)
                {
                    _state.ShiftHeld = false;
                }
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
            _state.CursorPosition = _state.Length;
            _state.ClearSelection();
            _state.ResetCursorBlink();
            return false;
        }

        _context.FocusedTextFieldId = 0;
        _state.ClearSelection();
        InputSystem.StopTextInput();
        return false;
    }

    private bool HandleKeyDown(Event ev)
    {
        var key = ev.Key.KeyCode;
        var mod = (UiKeyMod)ev.Key.Mod;
        var ctrl = (mod & UiKeyMod.Ctrl) != 0;
        var shift = _state.ShiftHeld;
        var changed = false;

        switch (key)
        {
            case KeyCode.Left:
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

                _state.ResetCursorBlink();
                break;

            case KeyCode.Right:
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

                _state.ResetCursorBlink();
                break;

            case KeyCode.Up when _multiline:
                if (shift)
                {
                    _state.StartSelection();
                }
                else
                {
                    _state.ClearSelection();
                }

                MoveCursorUp();
                _state.ResetCursorBlink();
                break;

            case KeyCode.Down when _multiline:
                if (shift)
                {
                    _state.StartSelection();
                }
                else
                {
                    _state.ClearSelection();
                }

                MoveCursorDown();
                _state.ResetCursorBlink();
                break;

            case KeyCode.Home:
                if (shift)
                {
                    _state.StartSelection();
                    _state.CursorPosition = ctrl ? 0 : GetLineStart(_state.CursorPosition);
                }
                else
                {
                    _state.ClearSelection();
                    _state.CursorPosition = ctrl ? 0 : GetLineStart(_state.CursorPosition);
                }

                _state.ResetCursorBlink();
                break;

            case KeyCode.End:
                if (shift)
                {
                    _state.StartSelection();
                    _state.CursorPosition = ctrl ? _state.Length : GetLineEnd(_state.CursorPosition);
                }
                else
                {
                    _state.ClearSelection();
                    _state.CursorPosition = ctrl ? _state.Length : GetLineEnd(_state.CursorPosition);
                }

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
                _state.InsertText("\t", _maxLength > 0 ? _maxLength : null);
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
                break;

            case KeyCode.X when ctrl && !_readOnly:
                if (_state.HasSelection)
                {
                    _state.DeleteSelection();
                    changed = true;
                }

                _state.ResetCursorBlink();
                break;

            case KeyCode.V when ctrl && !_readOnly:
                _state.ResetCursorBlink();
                break;
        }

        return changed;
    }

    private int GetLineStart(int position)
    {
        var (line, _) = _state.GetLineAndColumn(position);
        return _state.GetPositionFromLineColumn(line, 0);
    }

    private int GetLineEnd(int position)
    {
        var (line, _) = _state.GetLineAndColumn(position);
        return _state.GetPositionFromLineColumn(line, _state.GetLineLength(line));
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
            _state.CursorPosition = _state.GetPositionFromLineColumn(line - 1, col);
        }
    }

    private void MoveCursorDown()
    {
        var (line, col) = _state.GetLineAndColumn(_state.CursorPosition);
        if (line < _state.LineCount - 1)
        {
            _state.CursorPosition = _state.GetPositionFromLineColumn(line + 1, col);
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