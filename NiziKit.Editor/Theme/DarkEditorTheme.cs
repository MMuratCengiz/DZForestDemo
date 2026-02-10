using NiziKit.UI;

namespace NiziKit.Editor.Theme;

public sealed class DarkEditorTheme : IEditorTheme
{
    // Panel backgrounds
    public UiColor PanelBackground => UiColor.FromHex(0x0A0A0C);
    public UiColor PanelSecondaryBackground => UiColor.FromHex(0x050506);
    public UiColor PanelTertiaryBackground => UiColor.FromHex(0x000000);
    public UiColor PanelElevated => UiColor.FromHex(0x151518);
    public UiColor SurfaceRaised => UiColor.FromHex(0x101014);
    public UiColor SurfaceOverlay => UiColor.FromHex(0x181820);
    public UiColor SurfaceInset => UiColor.FromHex(0x0C0C0F);

    // Accent
    public UiColor Accent => UiColor.FromHex(0x408060);
    public UiColor AccentLight => UiColor.FromHex(0x509070);
    public UiColor AccentDark => UiColor.FromHex(0x306050);

    // Text
    public UiColor TextPrimary => UiColor.FromHex(0xE0E0E8);
    public UiColor TextSecondary => UiColor.FromHex(0xA0A0B0);
    public UiColor TextMuted => UiColor.FromHex(0x606070);
    public UiColor TextDisabled => UiColor.FromHex(0x404050);

    // Borders
    public UiColor Border => UiColor.FromHex(0x252530);
    public UiColor BorderLight => UiColor.FromHex(0x353540);

    // Semantic
    public UiColor Success => UiColor.FromHex(0x60A060);
    public UiColor Warning => UiColor.FromHex(0xC0A050);
    public UiColor Error => UiColor.FromHex(0xC06060);
    public UiColor Info => UiColor.FromHex(0x509080);

    // Interactive states
    public UiColor Hover => UiColor.FromHex(0x1A1A20);
    public UiColor Active => UiColor.FromHex(0x252530);
    public UiColor Selected => UiColor.FromHex(0x254535);
    public UiColor SelectedBorder => UiColor.FromHex(0x408060);
    public UiColor FocusRing => UiColor.Rgba(0x40, 0x80, 0x60, 0x80);

    // Axis colors
    public UiColor AxisX => UiColor.FromHex(0xE06060);
    public UiColor AxisY => UiColor.FromHex(0x60C080);
    public UiColor AxisZ => UiColor.FromHex(0x6090E0);

    // Component styling
    public UiColor ComponentAccent => UiColor.FromHex(0x161620);
    public UiColor ComponentBorder => UiColor.FromHex(0x2A2A35);
    public UiColor SectionHeaderBg => UiColor.FromHex(0x121216);

    // Scroll
    public UiColor ScrollThumb => UiColor.FromHex(0x303040);
    public UiColor ScrollThumbHover => UiColor.FromHex(0x404055);
    public UiColor ScrollTrack => UiColor.FromHex(0x0A0A0C);

    // Drag label
    public UiColor DragLabelBg => UiColor.FromHex(0x1A2825);
    public UiColor DragLabelHover => UiColor.FromHex(0x203530);

    // Dialog overlay
    public UiColor DialogOverlay => UiColor.Rgba(0x18, 0x18, 0x25, 0xE0);

    // Input controls
    public UiColor InputBackground => UiColor.FromHex(0x28282D);
    public UiColor InputBackgroundFocused => UiColor.FromHex(0x1E1E23);
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
