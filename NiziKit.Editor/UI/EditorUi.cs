using NiziKit.Editor.Theme;
using NiziKit.UI;

namespace NiziKit.Editor.UI;

public static class EditorUi
{
    private static IEditorTheme T => EditorTheme.Current;

    public static void SectionHeader(UiFrame ui, UiContext ctx, string id, string icon, string title)
    {
        using var header = ui.Panel(id)
            .Horizontal()
            .Background(T.SectionHeaderBg)
            .Padding(10, 4)
            .Gap(6)
            .AlignChildrenY(UiAlignY.Center)
            .GrowWidth()
            .FitHeight()
            .Open();

        ui.Icon(icon, T.Accent, T.IconSizeSmall);
        ui.Text(title, new UiTextStyle { Color = T.TextPrimary, FontSize = T.FontSizeBody });
    }

    public static bool AccentButton(UiContext ctx, string id, string text)
    {
        return Ui.Button(ctx, id, text)
            .Color(T.Accent, T.AccentLight, T.AccentDark)
            .TextColor(UiColor.White)
            .FontSize(T.FontSizeBody)
            .CornerRadius(T.RadiusMedium)
            .Padding(12, 5)
            .Show();
    }

    public static bool GhostButton(UiContext ctx, string id, string text)
    {
        return Ui.Button(ctx, id, text)
            .Color(UiColor.Transparent, T.Hover, T.Active)
            .TextColor(T.TextPrimary)
            .FontSize(T.FontSizeBody)
            .CornerRadius(T.RadiusMedium)
            .Padding(12, 5)
            .Border(0, UiColor.Transparent)
            .Show();
    }

    public static bool DangerButton(UiContext ctx, string id, string text)
    {
        return Ui.Button(ctx, id, text)
            .Color(T.Error, T.Error.WithAlpha(220), T.Error.WithAlpha(180))
            .TextColor(UiColor.Rgb(0x11, 0x11, 0x1B))
            .FontSize(T.FontSizeBody)
            .CornerRadius(T.RadiusMedium)
            .Padding(12, 5)
            .Border(0, UiColor.Transparent)
            .Show();
    }

    public static bool IconButton(UiContext ctx, string id, string icon)
    {
        var btn = Ui.Button(ctx, id, "")
            .Color(UiColor.Transparent, T.Hover, T.Active)
            .CornerRadius(T.RadiusMedium)
            .Padding(4, 4)
            .Border(0, UiColor.Transparent);

        using var scope = btn.Open();
        scope.Icon(icon, T.TextSecondary, T.IconSizeSmall);
        return btn.WasClicked();
    }

    public static bool ToolbarToggle(UiContext ctx, string id, string icon, string label, bool active)
    {
        var bg = active ? T.Accent : UiColor.Transparent;
        var hoverBg = active ? T.AccentLight : T.Hover;
        var textColor = active ? UiColor.White : T.TextSecondary;

        var btn = Ui.Button(ctx, id, "")
            .Color(bg, hoverBg, T.Active)
            .CornerRadius(T.RadiusMedium)
            .Padding(6, 4)
            .Gap(4)
            .Horizontal()
            .Border(0, UiColor.Transparent);

        using var scope = btn.Open();
        scope.Icon(icon, textColor, T.IconSizeXS);
        scope.Text(label, new UiTextStyle { Color = textColor, FontSize = T.FontSizeCaption });
        return btn.WasClicked();
    }

    public static void ThemedDivider(UiContext ctx)
    {
        Ui.Divider(ctx, T.Border);
    }

    public static UiElementScope DialogOverlay(UiFrame ui, string id)
    {
        return ui.Panel(id)
            .Background(T.DialogOverlay)
            .FloatingRoot(900)
            .Grow()
            .AlignChildren(UiAlignX.Center, UiAlignY.Center)
            .Open();
    }

    public static UiElementScope DialogContainer(UiFrame ui, UiContext ctx, string id, string title, float width, float height)
    {
        var container = ui.Panel(id)
            .Background(T.PanelBackground)
            .Border(1, T.Border)
            .CornerRadius(T.RadiusLarge)
            .Width(width)
            .Height(height)
            .Vertical()
            .Open();

        using (ui.Panel(id + "_header")
            .Background(T.Accent)
            .Padding(20, 10)
            .GrowWidth()
            .FitHeight()
            .Horizontal()
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            ui.Text(title, new UiTextStyle { Color = UiColor.White, FontSize = T.FontSizeSubtitle });
        }

        using (ui.Panel(id + "_div").GrowWidth().Height(1).Background(T.Border).Open()) { }
        return container;
    }

    public static void MenuButton(UiContext ctx, string id, string label, out bool clicked, out UiContextMenuState menuState)
    {
        var btn = Ui.Button(ctx, id, label)
            .Color(UiColor.Transparent, T.Hover, T.Active)
            .TextColor(T.TextPrimary)
            .FontSize(T.FontSizeBody)
            .Padding(8, 4)
            .CornerRadius(T.RadiusSmall)
            .Border(0, UiColor.Transparent);

        clicked = btn.Show();
        menuState = Ui.GetContextMenuState(ctx, id + "_menu");
    }

    public static void Badge(UiFrame ui, string text)
    {
        using (ui.Panel("badge_" + text)
            .Background(T.PanelElevated)
            .CornerRadius(T.RadiusSmall)
            .Padding(6, 3)
            .FitWidth()
            .FitHeight()
            .Open())
        {
            ui.Text(text, new UiTextStyle { Color = T.TextSecondary, FontSize = T.FontSizeCaption });
        }
    }
}
