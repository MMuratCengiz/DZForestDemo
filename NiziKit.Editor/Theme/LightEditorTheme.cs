using NiziKit.UI;

namespace NiziKit.Editor.Theme;

public sealed class LightEditorTheme : IEditorTheme
{
    // Panel backgrounds (inverted from dark)
    public UiColor PanelBackground => UiColor.FromHex(0xF0F0F4);
    public UiColor PanelSecondaryBackground => UiColor.FromHex(0xE8E8EC);
    public UiColor PanelTertiaryBackground => UiColor.FromHex(0xFFFFFF);
    public UiColor PanelElevated => UiColor.FromHex(0xFAFAFC);
    public UiColor SurfaceRaised => UiColor.FromHex(0xFFFFFF);
    public UiColor SurfaceOverlay => UiColor.FromHex(0xF4F4F8);
    public UiColor SurfaceInset => UiColor.FromHex(0xECECF0);

    // Accent (same hue, adjusted for light bg)
    public UiColor Accent => UiColor.FromHex(0x357050);
    public UiColor AccentLight => UiColor.FromHex(0x408060);
    public UiColor AccentDark => UiColor.FromHex(0x2A5A40);

    // Text (inverted)
    public UiColor TextPrimary => UiColor.FromHex(0x1A1A24);
    public UiColor TextSecondary => UiColor.FromHex(0x505060);
    public UiColor TextMuted => UiColor.FromHex(0x808090);
    public UiColor TextDisabled => UiColor.FromHex(0xA0A0B0);

    // Borders
    public UiColor Border => UiColor.FromHex(0xD0D0D8);
    public UiColor BorderLight => UiColor.FromHex(0xC0C0C8);

    // Semantic
    public UiColor Success => UiColor.FromHex(0x408040);
    public UiColor Warning => UiColor.FromHex(0xA08030);
    public UiColor Error => UiColor.FromHex(0xC05050);
    public UiColor Info => UiColor.FromHex(0x408070);

    // Interactive states
    public UiColor Hover => UiColor.FromHex(0xE4E4EC);
    public UiColor Active => UiColor.FromHex(0xD8D8E0);
    public UiColor Selected => UiColor.FromHex(0xD0E8DA);
    public UiColor SelectedBorder => UiColor.FromHex(0x357050);
    public UiColor FocusRing => UiColor.Rgba(0x35, 0x70, 0x50, 0x80);

    // Axis colors (same)
    public UiColor AxisX => UiColor.FromHex(0xD04040);
    public UiColor AxisY => UiColor.FromHex(0x40A060);
    public UiColor AxisZ => UiColor.FromHex(0x4070D0);

    // Component styling
    public UiColor ComponentAccent => UiColor.FromHex(0xE8E8F0);
    public UiColor ComponentBorder => UiColor.FromHex(0xCCCCD4);
    public UiColor SectionHeaderBg => UiColor.FromHex(0xE4E4EC);

    // Scroll
    public UiColor ScrollThumb => UiColor.FromHex(0xC0C0CC);
    public UiColor ScrollThumbHover => UiColor.FromHex(0xA0A0B0);
    public UiColor ScrollTrack => UiColor.FromHex(0xF0F0F4);

    // Drag label
    public UiColor DragLabelBg => UiColor.FromHex(0xD8E8E0);
    public UiColor DragLabelHover => UiColor.FromHex(0xC8E0D0);

    // Dialog overlay
    public UiColor DialogOverlay => UiColor.Rgba(0x20, 0x20, 0x30, 0xC0);

    // Input controls
    public UiColor InputBackground => UiColor.FromHex(0xE0E0E4);
    public UiColor InputBackgroundFocused => UiColor.FromHex(0xD8D8DC);
    public UiColor InputText => TextPrimary;

    // Font sizes
    public ushort FontSizeCaption => 12;
    public ushort FontSizeBody => 14;
    public ushort FontSizeBodyLarge => 15;
    public ushort FontSizeSubtitle => 17;
    public ushort FontSizeTitle => 22;
    public ushort FontSizeHeader => 26;

    // Icon sizes
    public ushort IconSizeXS => 10;
    public ushort IconSizeSmall => 13;
    public ushort IconSizeBase => 16;
    public ushort IconSizeMedium => 18;
    public ushort IconSizeLarge => 22;
    public ushort IconSizeXL => 28;

    // Spacing
    public float SpacingXS => 4;
    public float SpacingSM => 8;
    public float SpacingMD => 12;
    public float SpacingLG => 16;
    public float SpacingXL => 24;

    // Corner radii
    public float RadiusNone => 0;
    public float RadiusSmall => 2;
    public float RadiusMedium => 4;
    public float RadiusLarge => 6;

    // Panel dimensions
    public float PanelMinWidth => 280;
    public float PanelMaxWidth => 420;
    public float PanelPreferredWidth => 320;

    // Pre-built text styles
    public UiTextStyle CaptionStyle => new() { Color = TextMuted, FontSize = FontSizeCaption };
    public UiTextStyle BodyStyle => new() { Color = TextPrimary, FontSize = FontSizeBody };
    public UiTextStyle BodyLargeStyle => new() { Color = TextPrimary, FontSize = FontSizeBodyLarge };
    public UiTextStyle SubtitleStyle => new() { Color = TextPrimary, FontSize = FontSizeSubtitle };
    public UiTextStyle TitleStyle => new() { Color = TextPrimary, FontSize = FontSizeTitle };
    public UiTextStyle HeaderStyle => new() { Color = TextPrimary, FontSize = FontSizeHeader };
    public UiTextStyle MutedStyle => new() { Color = TextMuted, FontSize = FontSizeBody };
    public UiTextStyle SecondaryStyle => new() { Color = TextSecondary, FontSize = FontSizeBody };
    public UiTextStyle AccentStyle => new() { Color = Accent, FontSize = FontSizeBody };
}
