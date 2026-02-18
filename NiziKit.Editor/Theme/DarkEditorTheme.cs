using NiziKit.UI;

namespace NiziKit.Editor.Theme;

public sealed class DarkEditorTheme : IEditorTheme
{
    public UiColor PanelBackground => UiColor.FromHex(0x18181E);
    public UiColor PanelSecondaryBackground => UiColor.FromHex(0x131317);
    public UiColor PanelTertiaryBackground => UiColor.FromHex(0x0E0E12);
    public UiColor PanelElevated => UiColor.FromHex(0x252530);
    public UiColor SurfaceRaised => UiColor.FromHex(0x1E1E26);
    public UiColor SurfaceOverlay => UiColor.FromHex(0x282834);
    public UiColor SurfaceInset => UiColor.FromHex(0x121218);

    public UiColor Accent => UiColor.FromHex(0x408060);
    public UiColor AccentLight => UiColor.FromHex(0x509070);
    public UiColor AccentDark => UiColor.FromHex(0x306050);

    public UiColor TextPrimary => UiColor.FromHex(0xE4E4EC);
    public UiColor TextSecondary => UiColor.FromHex(0xA0A0B0);
    public UiColor TextMuted => UiColor.FromHex(0x686878);
    public UiColor TextDisabled => UiColor.FromHex(0x484858);

    public UiColor Border => UiColor.FromHex(0x32323E);
    public UiColor BorderLight => UiColor.FromHex(0x3E3E4C);

    public UiColor Success => UiColor.FromHex(0x60A060);
    public UiColor Warning => UiColor.FromHex(0xC0A050);
    public UiColor Error => UiColor.FromHex(0xC06060);
    public UiColor Info => UiColor.FromHex(0x509080);

    public UiColor Hover => UiColor.FromHex(0x282832);
    public UiColor Active => UiColor.FromHex(0x303040);
    public UiColor Selected => UiColor.FromHex(0x2C4C3A);
    public UiColor SelectedBorder => UiColor.FromHex(0x408060);
    public UiColor FocusRing => UiColor.Rgba(0x40, 0x80, 0x60, 0x80);

    public UiColor AxisX => UiColor.FromHex(0xE06060);
    public UiColor AxisY => UiColor.FromHex(0x60C080);
    public UiColor AxisZ => UiColor.FromHex(0x6090E0);

    public UiColor ComponentAccent => UiColor.FromHex(0x1E1E28);
    public UiColor ComponentBorder => UiColor.FromHex(0x36363F);
    public UiColor SectionHeaderBg => UiColor.FromHex(0x222230);

    public UiColor ScrollThumb => UiColor.FromHex(0x3C3C4A);
    public UiColor ScrollThumbHover => UiColor.FromHex(0x4C4C5A);
    public UiColor ScrollTrack => UiColor.FromHex(0x18181E);

    public UiColor DragLabelBg => UiColor.FromHex(0x1E2E2A);
    public UiColor DragLabelHover => UiColor.FromHex(0x263C34);

    public UiColor DialogOverlay => UiColor.Rgba(0x18, 0x18, 0x25, 0xE0);

    public UiColor InputBackground => UiColor.FromHex(0x20202A);
    public UiColor InputBackgroundFocused => UiColor.FromHex(0x1C1C24);
    public UiColor InputText => TextPrimary;

    public ushort FontSizeCaption => 12;
    public ushort FontSizeBody => 14;
    public ushort FontSizeBodyLarge => 15;
    public ushort FontSizeSubtitle => 17;
    public ushort FontSizeTitle => 22;
    public ushort FontSizeHeader => 26;

    public ushort IconSizeXS => 10;
    public ushort IconSizeSmall => 13;
    public ushort IconSizeBase => 16;
    public ushort IconSizeMedium => 18;
    public ushort IconSizeLarge => 22;
    public ushort IconSizeXL => 28;

    public float SpacingXS => 4;
    public float SpacingSM => 8;
    public float SpacingMD => 12;
    public float SpacingLG => 16;
    public float SpacingXL => 24;

    public float RadiusNone => 0;
    public float RadiusSmall => 2;
    public float RadiusMedium => 4;
    public float RadiusLarge => 6;

    public float PanelMinWidth => 280;
    public float PanelMaxWidth => 420;
    public float PanelPreferredWidth => 380;

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
