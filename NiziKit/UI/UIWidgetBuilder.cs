using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public struct WidgetStyle
{
    public UiColor BackgroundColor;
    public UiColor HoverColor;
    public UiColor PressedColor;
    public UiColor DisabledColor;
    public UiColor TextColor;
    public UiColor BorderColor;
    public UiColor FocusedBorderColor;
    public float BorderWidth;
    public float CornerRadius;
    public UiPadding Padding;
    public ushort FontSize;

    public static WidgetStyle Default => new()
    {
        BackgroundColor = UiColor.Rgb(45, 45, 48),
        HoverColor = UiColor.Rgb(55, 55, 60),
        PressedColor = UiColor.Rgb(35, 35, 38),
        DisabledColor = UiColor.Rgb(35, 35, 38),
        TextColor = UiColor.White,
        BorderColor = UiColor.Rgb(70, 70, 75),
        FocusedBorderColor = UiColor.Rgb(100, 149, 237),
        BorderWidth = 1,
        CornerRadius = 4,
        Padding = UiPadding.Symmetric(8, 6),
        FontSize = 14
    };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UiColor GetBackgroundColor(bool isHovered, bool isPressed, bool isDisabled)
    {
        if (isDisabled)
        {
            return DisabledColor;
        }

        if (isPressed)
        {
            return PressedColor;
        }

        if (isHovered)
        {
            return HoverColor;
        }

        return BackgroundColor;
    }
}
