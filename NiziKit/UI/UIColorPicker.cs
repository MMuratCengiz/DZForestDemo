using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.UI;

public sealed class UiColorPickerState
{
    public bool IsOpen;
    public float Hue;
    public float Saturation;
    public float Value = 1f;
    public bool HsvInitialized;

    public Texture? WheelTexture;
    public byte[]? PixelBuffer;
    public float LastRenderedHue = -1f;
    public float LastRenderedSat = -1f;
    public float LastRenderedVal = -1f;
    public int ActualTexSize;

    public bool IsDraggingHue;
    public bool IsDraggingSv;
    public UiSliderState AlphaSlider = new();
}

public ref struct UiColorPicker
{
    private readonly UiContext _context;
    private readonly UiColorPickerState _state;

    private bool _hasAlpha;
    private ushort _fontSize;
    private float _swatchHeight;
    private float _cornerRadius;
    private UiColor _borderColor;
    private UiColor _panelBackground;
    private UiColor _labelColor;
    private UiColor _valueTextColor;
    private UiSizing _width;

    private const int TexSize = 180;
    private const float TexCenter = TexSize / 2f;
    private const float RingOuter = 85f;
    private const float RingInner = 62f;
    private const float SvHalf = 43f;
    private const float SvSize = SvHalf * 2f;

    internal UiColorPicker(UiContext ctx, string id, UiColorPickerState state)
    {
        _context = ctx;
        _state = state;
        Id = ctx.StringCache.GetId(id);

        _hasAlpha = false;
        _fontSize = 12;
        _swatchHeight = 18;
        _cornerRadius = 2;
        _borderColor = UiColor.Rgb(55, 55, 60);
        _panelBackground = UiColor.Rgb(35, 35, 40);
        _labelColor = UiColor.Rgb(180, 180, 190);
        _valueTextColor = UiColor.Rgb(160, 160, 170);
        _width = UiSizing.Grow();
    }

    public uint Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker HasAlpha(bool alpha) { _hasAlpha = alpha; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker FontSize(ushort size) { _fontSize = size; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker SwatchHeight(float h) { _swatchHeight = h; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker CornerRadius(float r) { _cornerRadius = r; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker BorderColor(UiColor color) { _borderColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker PanelBackground(UiColor color) { _panelBackground = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker LabelColor(UiColor color) { _labelColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker ValueTextColor(UiColor color) { _valueTextColor = color; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker Width(UiSizing sizing) { _width = sizing; return this; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColorPicker GrowWidth() { _width = UiSizing.Grow(); return this; }

    public bool Show(ref float r, ref float g, ref float b)
    {
        var a = 1f;
        return ShowInternal(ref r, ref g, ref b, ref a, false);
    }

    public bool Show(ref float r, ref float g, ref float b, ref float a)
    {
        return ShowInternal(ref r, ref g, ref b, ref a, _hasAlpha);
    }

    private bool ShowInternal(ref float r, ref float g, ref float b, ref float a, bool showAlpha)
    {
        var changed = false;

        SyncHsvFromRgb(r, g, b);

        HsvToRgb(_state.Hue, _state.Saturation, _state.Value, out var dr, out var dg, out var db);
        var swatchColor = UiColor.Rgba(
            (byte)(Math.Clamp(dr, 0f, 1f) * 255),
            (byte)(Math.Clamp(dg, 0f, 1f) * 255),
            (byte)(Math.Clamp(db, 0f, 1f) * 255),
            (byte)(Math.Clamp(a, 0f, 1f) * 255));

        var swatchId = _context.StringCache.GetId("CPSwatch", Id);
        var swatchInteraction = _context.GetInteraction(swatchId);

        var containerDecl = new ClayElementDeclaration { Id = Id };
        containerDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        containerDecl.Layout.Sizing.Width = _width.ToClayAxis();
        containerDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        containerDecl.Layout.ChildGap = 4;
        containerDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        _context.OpenElement(containerDecl);
        {
            var swatchBorderColor = swatchInteraction.IsHovered
                ? UiColor.Rgb(120, 120, 130) : _borderColor;

            var swatchDecl = new ClayElementDeclaration { Id = swatchId };
            swatchDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            swatchDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(_swatchHeight);
            swatchDecl.BackgroundColor = swatchColor.ToClayColor();
            swatchDecl.BorderRadius = ClayBorderRadius.CreateUniform(_cornerRadius);
            swatchDecl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform(1),
                Color = swatchBorderColor.ToClayColor()
            };

            _context.OpenElement(swatchDecl);
            _context.Clay.CloseElement();

            if (_state.IsOpen)
            {
                if (RenderPopup(swatchId, ref a, showAlpha))
                {
                    changed = true;
                }
            }
        }
        _context.Clay.CloseElement();

        var justEndedDrag = false;
        if (!_context.MousePressed && (_state.IsDraggingHue || _state.IsDraggingSv))
        {
            _state.IsDraggingHue = false;
            _state.IsDraggingSv = false;
            justEndedDrag = true;
        }

        if (_state.IsOpen && _context.MouseJustReleased && !IsAnyDragging() && !justEndedDrag)
        {
            var popupId = _context.StringCache.GetId("CPPopup", Id);
            if (!_context.Clay.PointerOver(popupId))
            {
                _state.IsOpen = false;
            }
        }

        if (!_state.IsOpen && swatchInteraction.WasClicked)
        {
            _state.IsOpen = true;
        }

        if (changed)
        {
            HsvToRgb(_state.Hue, _state.Saturation, _state.Value, out r, out g, out b);
        }

        return changed;
    }

    private void SyncHsvFromRgb(float r, float g, float b)
    {
        if (_state.IsDraggingHue || _state.IsDraggingSv)
        {
            return;
        }

        if (!_state.HsvInitialized)
        {
            RgbToHsv(r, g, b, out _state.Hue, out _state.Saturation, out _state.Value);
            _state.HsvInitialized = true;
            return;
        }

        HsvToRgb(_state.Hue, _state.Saturation, _state.Value, out var er, out var eg, out var eb);
        if (MathF.Abs(er - r) > 0.002f || MathF.Abs(eg - g) > 0.002f || MathF.Abs(eb - b) > 0.002f)
        {
            RgbToHsv(r, g, b, out _state.Hue, out _state.Saturation, out _state.Value);
        }
    }

    private bool IsAnyDragging()
    {
        return _state.IsDraggingHue || _state.IsDraggingSv || _state.AlphaSlider.IsDragging;
    }

    private bool RenderPopup(uint anchorId, ref float a, bool showAlpha)
    {
        var changed = false;

        EnsureWheelTexture();
        var actualSize = _state.ActualTexSize;

        var popupId = _context.StringCache.GetId("CPPopup", Id);
        var popupDecl = new ClayElementDeclaration { Id = popupId };
        popupDecl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        popupDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(TexSize + 20);
        popupDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        popupDecl.Layout.Padding = new ClayPadding { Left = 10, Right = 10, Top = 10, Bottom = 10 };
        popupDecl.Layout.ChildGap = 8;
        popupDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        popupDecl.BackgroundColor = _panelBackground.ToClayColor();
        popupDecl.BorderRadius = ClayBorderRadius.CreateUniform(6);
        popupDecl.Border = new ClayBorderDesc
        {
            Width = ClayBorderWidth.CreateUniform(1),
            Color = _borderColor.ToClayColor()
        };
        popupDecl.Floating = new ClayFloatingDesc
        {
            AttachTo = ClayFloatingAttachTo.ElementWithId,
            ParentId = anchorId,
            ParentAttachPoint = ClayFloatingAttachPoint.LeftBottom,
            ElementAttachPoint = ClayFloatingAttachPoint.LeftTop,
            ZIndex = 1000
        };

        _context.OpenElement(popupDecl);
        _context.BeginPopupScope(popupId);
        {
            var wheelId = _context.StringCache.GetId("CPWheel", Id);
            var wheelDecl = new ClayElementDeclaration { Id = wheelId };
            wheelDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(TexSize);
            wheelDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(TexSize);
            _context.OpenElement(wheelDecl);
            {
                if (_state.WheelTexture != null)
                {
                    _context.Clay.Texture(_state.WheelTexture, actualSize, actualSize);
                }
            }
            _context.Clay.CloseElement();

            if (HandleWheelInteraction(wheelId))
            {
                changed = true;
            }

            if (showAlpha)
            {
                if (RenderAlphaSlider(ref a))
                {
                    changed = true;
                }
            }

            HsvToRgb(_state.Hue, _state.Saturation, _state.Value, out var hexR, out var hexG, out var hexB);
            RenderHexDisplay(hexR, hexG, hexB, a, showAlpha);
        }
        _context.EndPopupScope();
        _context.Clay.CloseElement();

        return changed;
    }

    private bool HandleWheelInteraction(uint wheelId)
    {
        var changed = false;
        var wheelInteraction = _context.GetInteraction(wheelId);

        if (wheelInteraction.IsPressed && !_state.IsDraggingHue && !_state.IsDraggingSv
            && _context.ActiveDragWidgetId == 0)
        {
            var bbox = _context.Clay.GetElementBoundingBox(wheelId);
            var scale = bbox.Width > 0 ? TexSize / bbox.Width : 1f;
            var centerX = bbox.X + bbox.Width / 2f;
            var centerY = bbox.Y + bbox.Height / 2f;
            var relX = (_context.MouseX - centerX) * scale;
            var relY = (_context.MouseY - centerY) * scale;
            var distSq = relX * relX + relY * relY;

            if (distSq >= RingInner * RingInner && distSq <= RingOuter * RingOuter)
            {
                _state.IsDraggingHue = true;
                _context.ActiveDragWidgetId = Id;
            }
            else if (MathF.Abs(relX) <= SvHalf && MathF.Abs(relY) <= SvHalf)
            {
                _state.IsDraggingSv = true;
                _context.ActiveDragWidgetId = Id;
            }
        }

        if (_state.IsDraggingHue)
        {
            var bbox = _context.Clay.GetElementBoundingBox(wheelId);
            var centerX = bbox.X + bbox.Width / 2f;
            var centerY = bbox.Y + bbox.Height / 2f;
            var angle = MathF.Atan2(_context.MouseY - centerY, _context.MouseX - centerX);
            var hue = (angle / (2f * MathF.PI) + 0.5f) * 360f;
            if (hue >= 360f)
            {
                hue -= 360f;
            }
            if (hue < 0f)
            {
                hue += 360f;
            }
            _state.Hue = hue;
            changed = true;
        }

        if (_state.IsDraggingSv)
        {
            var bbox = _context.Clay.GetElementBoundingBox(wheelId);
            var scale = bbox.Width > 0 ? TexSize / bbox.Width : 1f;
            var centerX = bbox.X + bbox.Width / 2f;
            var centerY = bbox.Y + bbox.Height / 2f;
            var svScreenHalf = SvHalf / scale;
            var svScreenSize = SvSize / scale;

            _state.Saturation = Math.Clamp(
                (_context.MouseX - (centerX - svScreenHalf)) / svScreenSize, 0f, 1f);
            _state.Value = 1f - Math.Clamp(
                (_context.MouseY - (centerY - svScreenHalf)) / svScreenSize, 0f, 1f);
            changed = true;
        }

        return changed;
    }

    private bool RenderAlphaSlider(ref float a)
    {
        var rowId = _context.StringCache.GetId("CPAlphaRow", Id);
        var rowDecl = new ClayElementDeclaration { Id = rowId };
        rowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        rowDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(TexSize);
        rowDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        rowDecl.Layout.ChildGap = 6;
        rowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        var changed = false;

        _context.OpenElement(rowDecl);
        {
            var labelId = _context.StringCache.GetId("CPAlphaLbl", Id);
            var labelDecl = new ClayElementDeclaration { Id = labelId };
            labelDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(14);
            labelDecl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            _context.OpenElement(labelDecl);
            _context.Clay.Text("A", new ClayTextDesc
            {
                TextColor = UiColor.Rgb(180, 180, 180).ToClayColor(),
                FontSize = _fontSize
            });
            _context.Clay.CloseElement();

            var sliderId = "CPAlphaSlider_" + Id;
            var slider = new UiSlider(_context, sliderId, _state.AlphaSlider);
            changed = slider
                .Range(0f, 1f)
                .TrackColor(UiColor.Rgb(50, 50, 55))
                .FillColor(UiColor.Rgb(180, 180, 180))
                .ThumbColor(UiColor.White, UiColor.Rgb(200, 200, 200))
                .TrackHeight(4)
                .ThumbRadius(5)
                .ShowValue(false)
                .FontSize(_fontSize)
                .GrowWidth()
                .Show(ref a);

            var valId = _context.StringCache.GetId("CPAlphaVal", Id);
            var valDecl = new ClayElementDeclaration { Id = valId };
            valDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(30);
            valDecl.Layout.ChildAlignment.X = ClayAlignmentX.Right;
            _context.OpenElement(valDecl);
            _context.Clay.Text(a.ToString("F2"), new ClayTextDesc
            {
                TextColor = _valueTextColor.ToClayColor(),
                FontSize = _fontSize
            });
            _context.Clay.CloseElement();
        }
        _context.Clay.CloseElement();

        return changed;
    }

    private void RenderHexDisplay(float r, float g, float b, float a, bool showAlpha)
    {
        var ri = (byte)(Math.Clamp(r, 0f, 1f) * 255);
        var gi = (byte)(Math.Clamp(g, 0f, 1f) * 255);
        var bi = (byte)(Math.Clamp(b, 0f, 1f) * 255);
        var ai = (byte)(Math.Clamp(a, 0f, 1f) * 255);

        var hex = showAlpha
            ? $"#{ri:X2}{gi:X2}{bi:X2}{ai:X2}"
            : $"#{ri:X2}{gi:X2}{bi:X2}";

        var hexRowId = _context.StringCache.GetId("CPHexRow", Id);
        var hexRowDecl = new ClayElementDeclaration { Id = hexRowId };
        hexRowDecl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        hexRowDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        hexRowDecl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        hexRowDecl.Layout.ChildGap = 6;
        hexRowDecl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

        _context.OpenElement(hexRowDecl);
        {
            _context.Clay.Text("Hex", new ClayTextDesc
            {
                TextColor = _labelColor.ToClayColor(),
                FontSize = _fontSize
            });

            var spacerId = _context.StringCache.GetId("CPHexSpc", Id);
            var spacerDecl = new ClayElementDeclaration { Id = spacerId };
            spacerDecl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            _context.OpenElement(spacerDecl);
            _context.Clay.CloseElement();

            var previewColor = UiColor.Rgba(ri, gi, bi, ai);
            var prevId = _context.StringCache.GetId("CPHexPrev", Id);
            var prevDecl = new ClayElementDeclaration { Id = prevId };
            prevDecl.Layout.Sizing.Width = ClaySizingAxis.Fixed(16);
            prevDecl.Layout.Sizing.Height = ClaySizingAxis.Fixed(16);
            prevDecl.BackgroundColor = previewColor.ToClayColor();
            prevDecl.BorderRadius = ClayBorderRadius.CreateUniform(2);
            prevDecl.Border = new ClayBorderDesc
            {
                Width = ClayBorderWidth.CreateUniform(1),
                Color = _borderColor.ToClayColor()
            };
            _context.OpenElement(prevDecl);
            _context.Clay.CloseElement();

            _context.Clay.Text(hex, new ClayTextDesc
            {
                TextColor = _valueTextColor.ToClayColor(),
                FontSize = _fontSize
            });
        }
        _context.Clay.CloseElement();
    }

    private void EnsureWheelTexture()
    {
        var dpiScale = _context.Clay.GetDpiScale();
        var actualSize = Math.Max(1, (int)(TexSize * dpiScale));

        if (_state.ActualTexSize != actualSize)
        {
            _state.WheelTexture = null;
            _state.PixelBuffer = null;
            _state.ActualTexSize = actualSize;
            _state.LastRenderedHue = -1f;
        }

        var needsUpdate = _state.WheelTexture == null
            || MathF.Abs(_state.Hue - _state.LastRenderedHue) > 0.01f
            || MathF.Abs(_state.Saturation - _state.LastRenderedSat) > 0.001f
            || MathF.Abs(_state.Value - _state.LastRenderedVal) > 0.001f;

        if (!needsUpdate)
        {
            return;
        }

        _state.PixelBuffer ??= new byte[actualSize * actualSize * 4];
        GenerateWheelPixels(_state.PixelBuffer, actualSize, _state.Hue, _state.Saturation, _state.Value,
            _panelBackground.R, _panelBackground.G, _panelBackground.B);

        if (_state.WheelTexture == null)
        {
            var device = GraphicsContext.Device;
            _state.WheelTexture = device.CreateTexture(new TextureDesc
            {
                Width = (uint)actualSize,
                Height = (uint)actualSize,
                Depth = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8Unorm,
                Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
                DebugName = StringView.Create("ColorWheel")
            });
            GraphicsContext.ResourceTracking.TrackTexture(_state.WheelTexture, QueueType.Graphics);
        }

        UploadPixels(_state.WheelTexture, _state.PixelBuffer, actualSize);

        _state.LastRenderedHue = _state.Hue;
        _state.LastRenderedSat = _state.Saturation;
        _state.LastRenderedVal = _state.Value;
    }

    private static void UploadPixels(Texture texture, byte[] pixels, int texSize)
    {
        var device = GraphicsContext.Device;
        using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = true
        });
        batchCopy.Begin();
        batchCopy.CopyDataToTexture(new CopyDataToTextureDesc
        {
            Data = ByteArrayView.Create(pixels),
            DstTexture = texture,
            AutoAlign = false,
            Width = (uint)texSize,
            Height = (uint)texSize,
            MipLevel = 0,
            ArrayLayer = 0
        });
        batchCopy.Submit(null);
    }

    private static void GenerateWheelPixels(byte[] pixels, int pixelSize, float hue, float sat, float val,
        byte bgR, byte bgG, byte bgB)
    {
        var s = pixelSize / (float)TexSize;
        var center = pixelSize / 2f;
        var ringOuter = RingOuter * s;
        var ringInner = RingInner * s;
        var svHalf = SvHalf * s;
        var svSize = SvSize * s;

        var svL = (int)(center - svHalf);
        var svT = (int)(center - svHalf);
        var svR = (int)(center + svHalf);
        var svB = (int)(center + svHalf);

        for (var y = 0; y < pixelSize; y++)
        {
            var dy = y - center;
            for (var x = 0; x < pixelSize; x++)
            {
                var dx = x - center;
                var distSq = dx * dx + dy * dy;
                var offset = (y * pixelSize + x) * 4;

                if (distSq >= (ringInner - 1f) * (ringInner - 1f)
                    && distSq <= (ringOuter + 1f) * (ringOuter + 1f))
                {
                    var dist = MathF.Sqrt(distSq);
                    if (dist >= ringInner - 1f && dist <= ringOuter + 1f)
                    {
                        var angle = MathF.Atan2(dy, dx);
                        var h = (angle / (2f * MathF.PI) + 0.5f) * 360f;
                        if (h >= 360f)
                        {
                            h -= 360f;
                        }
                        HsvToRgb(h, 1f, 1f, out var cr, out var cg, out var cb);

                        var alpha = 1f;
                        if (dist > ringOuter - 1f)
                        {
                            alpha = Math.Clamp(ringOuter - dist + 1f, 0f, 1f);
                        }
                        else if (dist < ringInner + 1f)
                        {
                            alpha = Math.Clamp(dist - ringInner + 1f, 0f, 1f);
                        }

                        pixels[offset] = (byte)(cr * 255f * alpha + bgR * (1f - alpha));
                        pixels[offset + 1] = (byte)(cg * 255f * alpha + bgG * (1f - alpha));
                        pixels[offset + 2] = (byte)(cb * 255f * alpha + bgB * (1f - alpha));
                        pixels[offset + 3] = 255;
                    }
                    else
                    {
                        pixels[offset] = bgR;
                        pixels[offset + 1] = bgG;
                        pixels[offset + 2] = bgB;
                        pixels[offset + 3] = 255;
                    }
                }
                else if (x >= svL && x < svR && y >= svT && y < svB)
                {
                    var sx = (x - svL) / (svSize - 1f);
                    var sy = 1f - (y - svT) / (svSize - 1f);
                    HsvToRgb(hue, sx, sy, out var cr, out var cg, out var cb);

                    pixels[offset] = (byte)(cr * 255f);
                    pixels[offset + 1] = (byte)(cg * 255f);
                    pixels[offset + 2] = (byte)(cb * 255f);
                    pixels[offset + 3] = 255;
                }
                else
                {
                    pixels[offset] = bgR;
                    pixels[offset + 1] = bgG;
                    pixels[offset + 2] = bgB;
                    pixels[offset + 3] = 255;
                }
            }
        }

        var hueAngle = (hue / 360f - 0.5f) * 2f * MathF.PI;
        var ringMid = (ringOuter + ringInner) / 2f;
        var hueIndX = center + ringMid * MathF.Cos(hueAngle);
        var hueIndY = center + ringMid * MathF.Sin(hueAngle);
        DrawIndicator(pixels, pixelSize, hueIndX, hueIndY, 5f * s);

        var svIndX = center - svHalf + sat * (svSize - 1f);
        var svIndY = center - svHalf + (1f - val) * (svSize - 1f);
        DrawIndicator(pixels, pixelSize, svIndX, svIndY, 4f * s);
    }

    private static void DrawIndicator(byte[] pixels, int pixelSize, float cx, float cy, float radius)
    {
        var ir = (int)MathF.Ceiling(radius + 2);
        var icx = (int)cx;
        var icy = (int)cy;

        for (var dy = -ir; dy <= ir; dy++)
        {
            for (var dx = -ir; dx <= ir; dx++)
            {
                var px = icx + dx;
                var py = icy + dy;
                if (px < 0 || px >= pixelSize || py < 0 || py >= pixelSize)
                {
                    continue;
                }

                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var offset = (py * pixelSize + px) * 4;

                if (dist <= radius - 1.5f)
                {
                    pixels[offset] = 255;
                    pixels[offset + 1] = 255;
                    pixels[offset + 2] = 255;
                    pixels[offset + 3] = 255;
                }
                else if (dist <= radius)
                {
                    pixels[offset] = 30;
                    pixels[offset + 1] = 30;
                    pixels[offset + 2] = 30;
                    pixels[offset + 3] = 255;
                }
            }
        }
    }

    private static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
    {
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;

        v = max;
        s = max > 0.001f ? delta / max : 0f;

        if (delta < 0.001f)
        {
            h = 0f;
        }
        else if (max == r)
        {
            h = 60f * ((g - b) / delta % 6f);
        }
        else if (max == g)
        {
            h = 60f * ((b - r) / delta + 2f);
        }
        else
        {
            h = 60f * ((r - g) / delta + 4f);
        }

        if (h < 0f)
        {
            h += 360f;
        }
    }

    internal static void HsvToRgb(float h, float s, float v, out float r, out float g, out float b)
    {
        if (s < 0.001f)
        {
            r = g = b = v;
            return;
        }

        h %= 360f;
        if (h < 0f)
        {
            h += 360f;
        }

        var c = v * s;
        var x = c * (1f - MathF.Abs(h / 60f % 2f - 1f));
        var m = v - c;

        float r1, g1, b1;
        if (h < 60f) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120f) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180f) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240f) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300f) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        r = r1 + m;
        g = g1 + m;
        b = b1 + m;
    }
}

public static partial class Ui
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColorPicker ColorPicker(UiContext ctx, string id)
    {
        var elementId = ctx.StringCache.GetId(id);
        var state = ctx.GetOrCreateState<UiColorPickerState>(elementId);
        return new UiColorPicker(ctx, id, state);
    }
}

public static partial class UiElementScopeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColorPicker ColorPicker(this ref UiElementScope scope, UiContext ctx, string id)
    {
        return Ui.ColorPicker(ctx, id);
    }
}
