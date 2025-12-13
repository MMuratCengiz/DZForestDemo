using System.Runtime.CompilerServices;
using DenOfIz;

namespace UIFramework;

/// <summary>
/// Represents a UI color with RGBA components.
/// Zero-allocation struct with implicit conversions to/from ClayColor.
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiColor(byte r, byte g, byte b, byte a = 255)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor Rgb(byte r, byte g, byte b) => new(r, g, b, 255);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor Rgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor FromHex(uint hex)
    {
        return new UiColor(
            (byte)((hex >> 16) & 0xFF),
            (byte)((hex >> 8) & 0xFF),
            (byte)(hex & 0xFF),
            255
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor FromHexA(uint hex)
    {
        return new UiColor(
            (byte)((hex >> 24) & 0xFF),
            (byte)((hex >> 16) & 0xFF),
            (byte)((hex >> 8) & 0xFF),
            (byte)(hex & 0xFF)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UiColor WithAlpha(byte alpha) => new(R, G, B, alpha);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayColor ToClayColor() => ClayColor.Create(R, G, B, A);

    // Common colors
    public static UiColor Transparent => new(0, 0, 0, 0);
    public static UiColor White => new(255, 255, 255, 255);
    public static UiColor Black => new(0, 0, 0, 255);
    public static UiColor Red => new(255, 0, 0, 255);
    public static UiColor Green => new(0, 255, 0, 255);
    public static UiColor Blue => new(0, 0, 255, 255);
    public static UiColor Gray => new(128, 128, 128, 255);
    public static UiColor LightGray => new(200, 200, 200, 255);
    public static UiColor DarkGray => new(64, 64, 64, 255);
}

/// <summary>
/// Represents padding for UI elements.
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiPadding(float left, float right, float top, float bottom)
{
    public readonly float Left = left;
    public readonly float Right = right;
    public readonly float Top = top;
    public readonly float Bottom = bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding All(float value) => new(value, value, value, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Symmetric(float horizontal, float vertical) =>
        new(horizontal, horizontal, vertical, vertical);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Horizontal(float value) => new(value, value, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Vertical(float value) => new(0, 0, value, value);

    public static UiPadding Zero => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayPadding ToClayPadding() => new()
    {
        Left = (ushort)Left,
        Right = (ushort)Right,
        Top = (ushort)Top,
        Bottom = (ushort)Bottom
    };
}

/// <summary>
/// Represents border radius for UI elements.
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiCornerRadius(float topLeft, float topRight, float bottomLeft, float bottomRight)
{
    public readonly float TopLeft = topLeft;
    public readonly float TopRight = topRight;
    public readonly float BottomLeft = bottomLeft;
    public readonly float BottomRight = bottomRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius All(float value) => new(value, value, value, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius Top(float value) => new(value, value, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius Bottom(float value) => new(0, 0, value, value);

    public static UiCornerRadius None => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayBorderRadius ToClayBorderRadius() => new()
    {
        TopLeft = TopLeft,
        TopRight = TopRight,
        BottomLeft = BottomLeft,
        BottomRight = BottomRight
    };
}

/// <summary>
/// Represents a border for UI elements.
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiBorder(float left, float right, float top, float bottom, UiColor color)
{
    public readonly float Left = left;
    public readonly float Right = right;
    public readonly float Top = top;
    public readonly float Bottom = bottom;
    public readonly UiColor Color = color;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder All(float width, UiColor color) => new(width, width, width, width, color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder Horizontal(float width, UiColor color) => new(width, width, 0, 0, color);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder Vertical(float width, UiColor color) => new(0, 0, width, width, color);

    public static UiBorder None => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayBorderDesc ToClayBorder() => new()
    {
        Width = new ClayBorderWidth
        {
            Left = (uint)Left,
            Right = (uint)Right,
            Top = (uint)Top,
            Bottom = (uint)Bottom
        },
        Color = Color.ToClayColor()
    };
}

/// <summary>
/// Specifies how a dimension should be sized.
/// </summary>
public readonly struct UiSizing
{
    internal readonly ClaySizingType Type;
    internal readonly float MinSize;
    internal readonly float MaxSize;

    private UiSizing(ClaySizingType type, float min, float max)
    {
        Type = type;
        MinSize = min;
        MaxSize = max;
    }

    /// <summary>
    /// Fixed size in pixels.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Fixed(float size) => new(ClaySizingType.Fixed, size, size);

    /// <summary>
    /// Fit to content with optional min/max constraints.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Fit(float min = 0, float max = float.MaxValue) =>
        new(ClaySizingType.Fit, min, max);

    /// <summary>
    /// Grow to fill available space with optional min/max constraints.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Grow(float min = 0, float max = float.MaxValue) =>
        new(ClaySizingType.Grow, min, max);

    /// <summary>
    /// Percentage of parent's size (0-100).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Percent(float percent) =>
        new(ClaySizingType.Percent, percent, percent);

    /// <summary>
    /// Default sizing (fit to content).
    /// </summary>
    public static UiSizing Auto => Fit();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClaySizingAxis ToClayAxis()
    {
        return Type switch
        {
            ClaySizingType.Fixed => ClaySizingAxis.Fixed(MinSize),
            ClaySizingType.Fit => ClaySizingAxis.Fit(MinSize, MaxSize),
            ClaySizingType.Grow => ClaySizingAxis.Grow(MinSize, MaxSize),
            ClaySizingType.Percent => ClaySizingAxis.Percent(MinSize),
            _ => ClaySizingAxis.Fit(0, float.MaxValue)
        };
    }
}

/// <summary>
/// Layout direction for child elements.
/// </summary>
public enum UiDirection
{
    /// <summary>
    /// Children are arranged horizontally (left to right).
    /// </summary>
    Horizontal,

    /// <summary>
    /// Children are arranged vertically (top to bottom).
    /// </summary>
    Vertical
}

/// <summary>
/// Horizontal alignment for children or content.
/// </summary>
public enum UiAlignX
{
    Left,
    Center,
    Right
}

/// <summary>
/// Vertical alignment for children or content.
/// </summary>
public enum UiAlignY
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// Text alignment options.
/// </summary>
public enum UiTextAlign
{
    Left,
    Center,
    Right
}

/// <summary>
/// Interaction state for UI elements.
/// </summary>
public readonly struct UiInteraction
{
    public readonly bool IsHovered;
    public readonly bool IsPressed;
    public readonly bool WasClicked;

    internal UiInteraction(bool hovered, bool pressed, bool clicked)
    {
        IsHovered = hovered;
        IsPressed = pressed;
        WasClicked = clicked;
    }

    public static UiInteraction None => default;
}
