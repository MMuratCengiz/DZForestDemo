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
}

public ref struct UiDraggableValue
{
    private readonly UiContext _context;
    private readonly UiDraggableValueState _state;

    private string _label;
    private UiColor _labelColor;
    private UiColor _labelTextColor;
    private float _sensitivity;
    private string _format;
    private ushort _fontSize;
    private float _width;
    private float _labelWidth;

    internal UiDraggableValue(UiContext ctx, string id, UiDraggableValueState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _label = "V";
        _labelColor = UiColor.Rgb(60, 130, 200);
        _labelTextColor = UiColor.White;
        _sensitivity = 0.5f;
        _format = "F2";
        _fontSize = 13;
        _width = 0;
        _labelWidth = 20;
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
    public UiDraggableValue Width(float w) { _width = w; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiDraggableValue LabelWidth(float w) { _labelWidth = w; return this; }

    public bool Show(ref float value)
    {
        var changed = false;
        var labelId = _context.StringCache.GetId("DVLabel", Id);
        var valueId = _context.StringCache.GetId("DVValue", Id);
        var labelInteraction = _context.GetInteraction(labelId);
        var valueInteraction = _context.GetInteraction(valueId);

        if (labelInteraction.IsPressed && !_state.IsDragging && !_state.IsEditing && _context.ActiveDragWidgetId == 0)
        {
            _state.IsDragging = true;
            _state.DragStartValue = value;
            _state.DragStartMouseX = _context.MouseX;
            _context.ActiveDragWidgetId = Id;
        }

        if (_state.IsDragging)
        {
            if (!_context.MousePressed)
            {
                _state.IsDragging = false;
            }
            else
            {
                var delta = (_context.MouseX - _state.DragStartMouseX) * _sensitivity;
                var newVal = _state.DragStartValue + delta;
                if (Math.Abs(newVal - value) > float.Epsilon)
                {
                    value = newVal;
                    changed = true;
                }
            }
        }

        if (valueInteraction.WasClicked && !_state.IsDragging)
        {
            _state.IsEditing = true;
            _state.EditText = value.ToString(_format);
            InputSystem.StartTextInput();
        }

        if (_state.IsEditing)
        {
            foreach (var ev in _context.FrameEvents)
            {
                if (ev.Type == EventType.KeyDown)
                {
                    if (ev.Key.KeyCode == KeyCode.Return || ev.Key.KeyCode == KeyCode.KeypadEnter)
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
                        InputSystem.StopTextInput();
                    }
                    else if (ev.Key.KeyCode == KeyCode.Escape)
                    {
                        _state.IsEditing = false;
                        InputSystem.StopTextInput();
                    }
                    else if (ev.Key.KeyCode == KeyCode.Backspace)
                    {
                        if (_state.EditText.Length > 0)
                        {
                            _state.EditText = _state.EditText[..^1];
                        }
                    }
                }
                else if (ev.Type == EventType.TextInput)
                {
                    _state.EditText += ev.Text.Text;
                }
            }

            if (_context.MouseJustReleased && !valueInteraction.IsHovered && !labelInteraction.IsHovered)
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
                InputSystem.StopTextInput();
            }
        }

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = _width > 0 ? ClaySizingAxis.Fixed(_width) : ClaySizingAxis.Grow(0, float.MaxValue);
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);

        _context.Clay.OpenElement(containerDecl);
        {
            var labelDecl = new ClayElementDeclaration { Id = labelId };
            labelDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(_labelWidth);
            labelDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            labelDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            labelDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            labelDecl.BackgroundColor = _labelColor.ToClayColor();
            labelDecl.BorderRadius = new ClayBorderRadius { TopLeft = 4, BottomLeft = 4 };

            _context.Clay.OpenElement(labelDecl);
            _context.Clay.Text(StringView.Intern(_label), new ClayTextDesc
            {
                TextColor = _labelTextColor.ToClayColor(),
                FontSize = _fontSize,
                TextAlignment = ClayTextAlignment.Center
            });
            _context.Clay.CloseElement();

            var displayText = _state.IsEditing ? _state.EditText + "|" : value.ToString(_format);
            var valueBg = _state.IsEditing ? UiColor.Rgb(30, 30, 35) : UiColor.Rgb(40, 40, 45);

            var valueDecl = new ClayElementDeclaration { Id = valueId };
            valueDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            valueDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            valueDecl.Layout.Padding = new ClayPadding { Left = 6, Right = 6, Top = 4, Bottom = 4 };
            valueDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            valueDecl.BackgroundColor = valueBg.ToClayColor();
            valueDecl.BorderRadius = new ClayBorderRadius { TopRight = 4, BottomRight = 4 };

            _context.Clay.OpenElement(valueDecl);
            _context.Clay.Text(StringView.Intern(displayText), new ClayTextDesc
            {
                TextColor = UiColor.Rgb(200, 200, 200).ToClayColor(),
                FontSize = _fontSize
            });
            _context.Clay.CloseElement();
        }
        _context.Clay.CloseElement();

        return changed;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDraggableValue DraggableValue(UiContext ctx, string id)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiDraggableValueState>(elementId);
        return new UiDraggableValue(ctx, id, state);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDraggableValue DraggableValue(this ref UiElementScope scope, UiContext ctx, string id)
    {
        return Ui.DraggableValue(ctx, id);
    }
}
