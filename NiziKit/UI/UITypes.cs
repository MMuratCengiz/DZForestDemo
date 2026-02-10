using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiColor(byte r, byte g, byte b, byte a = 255)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor Rgb(byte r, byte g, byte b)
    {
        return new UiColor(r, g, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor Rgba(byte r, byte g, byte b, byte a)
    {
        return new UiColor(r, g, b, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiColor FromHex(uint hex)
    {
        return new UiColor(
            (byte)((hex >> 16) & 0xFF),
            (byte)((hex >> 8) & 0xFF),
            (byte)(hex & 0xFF)
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
    public UiColor WithAlpha(byte alpha)
    {
        return new UiColor(R, G, B, alpha);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayColor ToClayColor()
    {
        return ClayColor.Create(R, G, B, A);
    }

    public static UiColor Transparent => new(0, 0, 0, 0);
    public static UiColor White => new(255, 255, 255);
    public static UiColor Black => new(0, 0, 0);
    public static UiColor Red => new(255, 0, 0);
    public static UiColor Green => new(0, 255, 0);
    public static UiColor Blue => new(0, 0, 255);
    public static UiColor Gray => new(128, 128, 128);
    public static UiColor LightGray => new(200, 200, 200);
    public static UiColor DarkGray => new(64, 64, 64);
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiPadding(float left, float right, float top, float bottom)
{
    public readonly float Left = left;
    public readonly float Right = right;
    public readonly float Top = top;
    public readonly float Bottom = bottom;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding All(float value)
    {
        return new UiPadding(value, value, value, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Symmetric(float horizontal, float vertical)
    {
        return new UiPadding(horizontal, horizontal, vertical, vertical);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Horizontal(float value)
    {
        return new UiPadding(value, value, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiPadding Vertical(float value)
    {
        return new UiPadding(0, 0, value, value);
    }

    public static UiPadding Zero => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayPadding ToClayPadding()
    {
        return new ClayPadding
        {
            Left = (ushort)Left,
            Right = (ushort)Right,
            Top = (ushort)Top,
            Bottom = (ushort)Bottom
        };
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct UiCornerRadius(float topLeft, float topRight, float bottomLeft, float bottomRight)
{
    public readonly float TopLeft = topLeft;
    public readonly float TopRight = topRight;
    public readonly float BottomLeft = bottomLeft;
    public readonly float BottomRight = bottomRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius All(float value)
    {
        return new UiCornerRadius(value, value, value, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius Top(float value)
    {
        return new UiCornerRadius(value, value, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCornerRadius Bottom(float value)
    {
        return new UiCornerRadius(0, 0, value, value);
    }

    public static UiCornerRadius None => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayBorderRadius ToClayBorderRadius()
    {
        return new ClayBorderRadius
        {
            TopLeft = TopLeft,
            TopRight = TopRight,
            BottomLeft = BottomLeft,
            BottomRight = BottomRight
        };
    }
}

public record struct UiBorder(float Left, float Right, float Top, float Bottom, UiColor Color)
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder All(float width, UiColor color)
    {
        return new UiBorder(width, width, width, width, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder Horizontal(float width, UiColor color)
    {
        return new UiBorder(width, width, 0, 0, color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiBorder Vertical(float width, UiColor color)
    {
        return new UiBorder(0, 0, width, width, color);
    }

    public static UiBorder None => default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ClayBorderDesc ToClayBorder()
    {
        return new ClayBorderDesc
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
}

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Fixed(float size)
    {
        return new UiSizing(ClaySizingType.Fixed, size, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Fit(float min = 0, float max = float.MaxValue)
    {
        return new UiSizing(ClaySizingType.Fit, min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Grow(float min = 0, float max = float.MaxValue)
    {
        return new UiSizing(ClaySizingType.Grow, min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiSizing Percent(float percent)
    {
        return new UiSizing(ClaySizingType.Percent, percent, percent);
    }

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

public enum UiDirection
{
    Horizontal,
    Vertical
}

public enum UiAlignX
{
    Left,
    Center,
    Right
}

public enum UiAlignY
{
    Top,
    Center,
    Bottom
}

public enum UiTextAlign
{
    Left,
    Center,
    Right
}

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
