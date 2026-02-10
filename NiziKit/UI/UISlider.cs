using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public sealed class UiSliderState
{
    public bool IsDragging { get; set; }
}

public ref struct UiSlider
{
    private readonly UiContext _context;
    private readonly UiSliderState _state;

    private float _min;
    private float _max;
    private float _step;
    private UiColor _trackColor;
    private UiColor _fillColor;
    private UiColor _thumbColor;
    private UiColor _thumbHoverColor;
    private float _trackHeight;
    private float _thumbRadius;
    private bool _showValue;
    private string _format;
    private UiColor _valueColor;
    private ushort _fontSize;
    private UiSizing _width;

    internal UiSlider(UiContext ctx, string id, UiSliderState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _min = 0;
        _max = 1;
        _step = 0;
        _trackColor = UiColor.Rgb(50, 50, 55);
        _fillColor = UiColor.Rgb(60, 130, 200);
        _thumbColor = UiColor.White;
        _thumbHoverColor = UiColor.Rgb(220, 220, 220);
        _trackHeight = 6;
        _thumbRadius = 8;
        _showValue = false;
        _format = "F2";
        _valueColor = UiColor.Gray;
        _fontSize = 12;
        _width = UiSizing.Grow();
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider Range(float min, float max) { _min = min; _max = max; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider Step(float step) { _step = step; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider TrackColor(UiColor color) { _trackColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider FillColor(UiColor color) { _fillColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider ThumbColor(UiColor color) { _thumbColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider ThumbColor(UiColor normal, UiColor hover) { _thumbColor = normal; _thumbHoverColor = hover; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider TrackHeight(float h) { _trackHeight = h; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider ThumbRadius(float r) { _thumbRadius = r; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider ShowValue(bool show = true, string format = "F2") { _showValue = show; _format = format; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider ValueColor(UiColor color) { _valueColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider Width(float w) { _width = UiSizing.Fixed(w); return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiSlider GrowWidth() { _width = UiSizing.Grow(); return this; }

    public bool Show(ref float value)
    {
        var changed = false;
        var range = _max - _min;
        if (range <= 0)
        {
            range = 1;
        }

        var normalized = Math.Clamp((value - _min) / range, 0f, 1f);

        var trackId = _context.StringCache.GetId("SlTrack", Id);
        var thumbId = _context.StringCache.GetId("SlThumb", Id);
        var trackInteraction = _context.GetInteraction(trackId);
        var thumbInteraction = _context.GetInteraction(thumbId);

        if (thumbInteraction.IsPressed && !_state.IsDragging && _context.ActiveDragWidgetId == 0)
        {
            _state.IsDragging = true;
            _context.ActiveDragWidgetId = Id;
        }

        if (_state.IsDragging && !_context.MousePressed)
        {
            _state.IsDragging = false;
        }

        if (trackInteraction.WasClicked && !_state.IsDragging)
        {
            var box = _context.Clay.GetElementBoundingBox(trackId);
            if (box.Width > 0)
            {
                var relX = (_context.MouseX - box.X) / box.Width;
                var newVal = _min + Math.Clamp(relX, 0f, 1f) * range;
                if (_step > 0)
                {
                    newVal = MathF.Round(newVal / _step) * _step;
                }

                newVal = Math.Clamp(newVal, _min, _max);
                if (Math.Abs(newVal - value) > float.Epsilon)
                {
                    value = newVal;
                    changed = true;
                }
            }
        }

        if (_state.IsDragging)
        {
            var box = _context.Clay.GetElementBoundingBox(trackId);
            if (box.Width > 0)
            {
                var relX = (_context.MouseX - box.X) / box.Width;
                var newVal = _min + Math.Clamp(relX, 0f, 1f) * range;
                if (_step > 0)
                {
                    newVal = MathF.Round(newVal / _step) * _step;
                }

                newVal = Math.Clamp(newVal, _min, _max);
                if (Math.Abs(newVal - value) > float.Epsilon)
                {
                    value = newVal;
                    changed = true;
                }
            }
            normalized = Math.Clamp((value - _min) / range, 0f, 1f);
        }

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = _width.ToClayAxis();
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        containerDecl.Layout.ChildGap = 8;

        _context.OpenElement(containerDecl);
        {
            var trackDecl = new ClayElementDeclaration { Id = trackId };
            trackDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            trackDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_trackHeight);
            trackDecl.BackgroundColor = _trackColor.ToClayColor();
            trackDecl.BorderRadius = ClayBorderRadius.CreateUniform(_trackHeight / 2);

            _context.OpenElement(trackDecl);
            {
                var fillId = _context.StringCache.GetId("SlFill", Id);
                var fillDecl = new ClayElementDeclaration { Id = fillId };
                fillDecl.Layout.Sizing.Width = ClaySizingAxis.Percent(normalized);
                fillDecl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
                fillDecl.BackgroundColor = _fillColor.ToClayColor();
                fillDecl.BorderRadius = ClayBorderRadius.CreateUniform(_trackHeight / 2);
                _context.OpenElement(fillDecl);
                _context.Clay.CloseElement();

                var thumbSize = _thumbRadius * 2;
                var isThumbHovered = thumbInteraction.IsHovered || _state.IsDragging;
                var thumbDecl = new ClayElementDeclaration { Id = thumbId };
                thumbDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(thumbSize);
                thumbDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(thumbSize);
                thumbDecl.BackgroundColor = (isThumbHovered ? _thumbHoverColor : _thumbColor).ToClayColor();
                thumbDecl.BorderRadius = ClayBorderRadius.CreateUniform(_thumbRadius);
                thumbDecl.Floating = new ClayFloatingDesc
                {
                    AttachTo = ClayFloatingAttachTo.ElementWithId,
                    ParentId = fillId,
                    ParentAttachPoint = ClayFloatingAttachPoint.RightCenter,
                    ElementAttachPoint = ClayFloatingAttachPoint.CenterCenter,
                    ZIndex = 100
                };
                _context.OpenElement(thumbDecl);
                _context.Clay.CloseElement();
            }
            _context.Clay.CloseElement();

            if (_showValue)
            {
                _context.Clay.Text(StringView.Intern(value.ToString(_format)), new ClayTextDesc
                {
                    TextColor = _valueColor.ToClayColor(),
                    FontSize = _fontSize
                });
            }
        }
        _context.Clay.CloseElement();

        return changed;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSlider Slider(UiContext ctx, string id)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiSliderState>(elementId);
        return new UiSlider(ctx, id, state);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSlider Slider(this ref UiElementScope scope, UiContext ctx, string id)
    {
        return Ui.Slider(ctx, id);
    }
}
