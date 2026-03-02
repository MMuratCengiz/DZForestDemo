using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace DZForestDemo.AvaUi.Components;

public class HoverText : TextBlock
{
    public static readonly StyledProperty<string?> TooltipTitleProperty =
        AvaloniaProperty.Register<HoverText, string?>(nameof(TooltipTitle));

    public static readonly StyledProperty<string?> TooltipContentProperty =
        AvaloniaProperty.Register<HoverText, string?>(nameof(TooltipContent));

    public static readonly StyledProperty<string?> TooltipIconProperty =
        AvaloniaProperty.Register<HoverText, string?>(nameof(TooltipIcon));

    private IBrush? _originalForeground;
    private static readonly SolidColorBrush FallbackGold = new(Color.Parse("#C8A84E"));
    private static readonly SolidColorBrush UnderlineBrush = new(Color.Parse("#80B8A888"));

    public string? TooltipTitle
    {
        get => GetValue(TooltipTitleProperty);
        set => SetValue(TooltipTitleProperty, value);
    }

    public string? TooltipContent
    {
        get => GetValue(TooltipContentProperty);
        set => SetValue(TooltipContentProperty, value);
    }

    public string? TooltipIcon
    {
        get => GetValue(TooltipIconProperty);
        set => SetValue(TooltipIconProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Cursor = new Cursor(StandardCursorType.Hand);
        ApplyUnderline();
    }

    private void ApplyUnderline()
    {
        TextDecorations = [new TextDecoration { Location = TextDecorationLocation.Underline, Stroke = UnderlineBrush }];
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _originalForeground = Foreground;
        var goldBrush = TryFindBrush("GameTextGold") ?? FallbackGold;
        Foreground = goldBrush;

        TextDecorations = [new TextDecoration { Location = TextDecorationLocation.Underline, Stroke = goldBrush }];
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_originalForeground != null)
        {
            Foreground = _originalForeground;
        }

        ApplyUnderline();
    }

    private IBrush? TryFindBrush(string key)
    {
        if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
        {
            return brush;
        }

        return null;
    }
}
