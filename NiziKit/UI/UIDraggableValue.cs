using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public sealed class UiDraggableValueState
{
    public bool IsDragging { get; set; }
    public float DragStartValue { get; set; }
    public float DragStartMouseX { get; set; }
    public bool IsEditing { get; set; }
    public string EditText { get; set; } = "";
    public bool ValueMouseDown { get; set; }
    public bool SelectAllOnNextRender { get; set; }
    public string? TextFieldId { get; set; }
}

public ref struct UiDraggableValue
{
    private readonly UiDraggableValueState _state;

    private string _label;
    private UiColor _labelColor;
    private UiColor _labelTextColor;
    private UiColor _valueColor;
    private UiColor _valueEditColor;
    private UiColor _valueTextColor;
    private UiColor? _labelAccent;
    private string? _prefix;
    private UiColor _prefixColor;
    private float _sensitivity;
    private string _format;
    private ushort _fontSize;
    private float _width;
    private UiSizing? _widthSizing;
    private float _labelWidth;
    private float _dragThreshold;

    internal UiDraggableValue(UiContext ctx, string id, UiDraggableValueState state)
    {
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _label = "V";
        _labelColor = UiColor.Rgb(60, 130, 200);
        _labelTextColor = UiColor.White;
        _valueColor = UiColor.Rgb(40, 40, 45);
        _valueEditColor = UiColor.Rgb(30, 30, 35);
        _valueTextColor = UiColor.Rgb(200, 200, 200);
        _sensitivity = 0.5f;
        _format = "F2";
        _fontSize = 13;
        _width = 0;
        _labelWidth = 20;
        _dragThreshold = 3;
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Label(string label) { _label = label; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue LabelColor(UiColor bg, UiColor text) { _labelColor = bg; _labelTextColor = text; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue LabelColor(UiColor bg) { _labelColor = bg; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Sensitivity(float s) { _sensitivity = s; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Format(string f) { _format = f; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Width(float w) { _width = w; _widthSizing = null; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Width(UiSizing sizing) { _widthSizing = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue LabelWidth(float w) { _labelWidth = w; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue LabelAccent(UiColor accentColor) { _labelAccent = accentColor; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue Prefix(string text, UiColor color) { _prefix = text; _prefixColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue ValueColor(UiColor bg) { _valueColor = bg; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue ValueEditColor(UiColor bg) { _valueEditColor = bg; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue ValueTextColor(UiColor color) { _valueTextColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue DragThreshold(float pixels) { _dragThreshold = pixels; return this; }

    public bool Show(ref float value)
    {
        var changed = false;
        var labelId = NiziUi.Ctx.StringCache.GetId("DVLabel", Id);
        var valueId = NiziUi.Ctx.StringCache.GetId("DVValue", Id);
        var labelInteraction = NiziUi.Ctx.GetInteraction(labelId);
        var valueInteraction = NiziUi.Ctx.GetInteraction(valueId);
        var textFieldIdStr = _state.TextFieldId!;
        var textFieldId = NiziUi.Ctx.StringCache.GetId(textFieldIdStr);

        if (_state.IsEditing && NiziUi.Ctx.FocusedTextFieldId != textFieldId)
        {
            if (float.TryParse(_state.EditText, out var parsed))
            {
                if (Math.Abs(parsed - value) > float.Epsilon)
                {
                    value = parsed;
                    changed = true;
                }
            }
            _state.IsEditing = false;
        }

        if (labelInteraction.IsPressed && !_state.IsDragging && !_state.IsEditing && NiziUi.Ctx.ActiveDragWidgetId == 0)
        {
            _state.IsDragging = true;
            _state.DragStartValue = value;
            _state.DragStartMouseX = NiziUi.Ctx.MouseX;
            NiziUi.Ctx.ActiveDragWidgetId = Id;
        }

        if (valueInteraction.IsPressed && !_state.IsDragging && !_state.IsEditing
            && !_state.ValueMouseDown && NiziUi.Ctx.ActiveDragWidgetId == 0)
        {
            _state.ValueMouseDown = true;
            _state.DragStartValue = value;
            _state.DragStartMouseX = NiziUi.Ctx.MouseX;
        }

        if (_state.ValueMouseDown && !_state.IsDragging)
        {
            if (!NiziUi.Ctx.MousePressed)
            {
                _state.ValueMouseDown = false;
                var delta = Math.Abs(NiziUi.Ctx.MouseX - _state.DragStartMouseX);
                if (delta < _dragThreshold)
                {
                    _state.IsEditing = true;
                    _state.EditText = value.ToString(_format);
                    _state.SelectAllOnNextRender = true;
                    NiziUi.Ctx.FocusedTextFieldId = textFieldId;
                    InputSystem.StartTextInput();
                }
            }
            else
            {
                var delta = Math.Abs(NiziUi.Ctx.MouseX - _state.DragStartMouseX);
                if (delta >= _dragThreshold)
                {
                    _state.IsDragging = true;
                    _state.ValueMouseDown = false;
                    NiziUi.Ctx.ActiveDragWidgetId = Id;
                }
            }
        }

        if (_state.IsDragging)
        {
            if (!NiziUi.Ctx.MousePressed)
            {
                _state.IsDragging = false;
                if (NiziUi.Ctx.ActiveDragWidgetId == Id)
                {
                    NiziUi.Ctx.ActiveDragWidgetId = 0;
                }
            }
            else
            {
                var delta = (NiziUi.Ctx.MouseX - _state.DragStartMouseX) * _sensitivity;
                var newVal = _state.DragStartValue + delta;
                if (Math.Abs(newVal - value) > float.Epsilon)
                {
                    value = newVal;
                    changed = true;
                }
            }
        }

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = _widthSizing.HasValue
            ? _widthSizing.Value.ToClayAxis()
            : _width > 0 ? ClaySizingAxis.Fixed(_width) : ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);

        NiziUi.Ctx.OpenElement(containerDecl);
        {
            var hasLabel = _labelWidth > 0;

            if (hasLabel)
            {
                var effectiveLabelBg = _labelColor;
                var effectiveLabelText = _labelTextColor;

                if (_labelAccent.HasValue)
                {
                    effectiveLabelBg = new UiColor(
                        (byte)Math.Max(0, _valueColor.R - 12),
                        (byte)Math.Max(0, _valueColor.G - 12),
                        (byte)Math.Max(0, _valueColor.B - 12),
                        _valueColor.A
                    );
                    effectiveLabelText = _labelAccent.Value;
                }

                var labelDecl = new ClayElementDeclaration { Id = labelId };
                labelDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_labelWidth);
                labelDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
                labelDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
                labelDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                labelDecl.BackgroundColor = effectiveLabelBg.ToClayColor();
                labelDecl.BorderRadius = new ClayBorderRadius { TopLeft = 4, BottomLeft = 4 };

                if (_labelAccent.HasValue)
                {
                    labelDecl.Border = new ClayBorderDesc
                    {
                        Width = new ClayBorderWidth { Left = 2 },
                        Color = _labelAccent.Value.ToClayColor()
                    };
                }

                NiziUi.Ctx.OpenElement(labelDecl);
                NiziUi.Ctx.Clay.Text(_label, new ClayTextDesc
                {
                    TextColor = effectiveLabelText.ToClayColor(),
                    FontSize = _fontSize,
                    TextAlignment = ClayTextAlignment.Center
                });
                NiziUi.Ctx.Clay.CloseElement();
            }

            var valueBg = _state.IsEditing ? _valueEditColor : _valueColor;

            var valueDecl = new ClayElementDeclaration { Id = valueId };
            valueDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            valueDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            valueDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            valueDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            valueDecl.Layout.ChildGap = 4;
            valueDecl.BackgroundColor = valueBg.ToClayColor();
            valueDecl.BorderRadius = hasLabel
                ? new ClayBorderRadius { TopRight = 4, BottomRight = 4 }
                : ClayBorderRadius.CreateUniform(4);

            if (!_state.IsEditing)
            {
                valueDecl.Layout.Padding = new ClayPadding { Left = 6, Right = 6, Top = 4, Bottom = 4 };
            }

            NiziUi.Ctx.OpenElement(valueDecl);

            if (_prefix != null)
            {
                NiziUi.Ctx.Clay.Text(_prefix, new ClayTextDesc
                {
                    TextColor = _prefixColor.ToClayColor(),
                    FontSize = _fontSize
                });
            }

            if (_state.IsEditing)
            {
                var editText = _state.EditText;
                var tf = NiziUi.TextField(textFieldIdStr, ref editText)
                    .BackgroundColor(UiColor.Transparent, UiColor.Transparent)
                    .TextColor(_valueTextColor)
                    .BorderColor(UiColor.Transparent, UiColor.Transparent)
                    .CursorColor(_valueTextColor)
                    .FontSize(_fontSize)
                    .Padding(4, 2)
                    .GrowWidth()
                    .CornerRadius(0);

                if (_state.SelectAllOnNextRender)
                {
                    var tfState = NiziUi.Ctx.GetOrCreateState<UiTextFieldState>(textFieldIdStr);
                    tfState.SelectAll();
                    _state.SelectAllOnNextRender = false;
                }

                tf.Show();
                _state.EditText = editText;
            }
            else
            {
                NiziUi.Ctx.Clay.Text(value.ToString(_format), new ClayTextDesc
                {
                    TextColor = _valueTextColor.ToClayColor(),
                    FontSize = _fontSize
                });
            }

            NiziUi.Ctx.Clay.CloseElement();
        }
        NiziUi.Ctx.Clay.CloseElement();

        return changed;
    }
}
