using NiziKit.UI;

namespace NiziKit.Editor.Theme;

public interface IEditorTheme
{
    // Panel backgrounds
    UiColor PanelBackground { get; }
    UiColor PanelSecondaryBackground { get; }
    UiColor PanelTertiaryBackground { get; }
    UiColor PanelElevated { get; }
    UiColor SurfaceRaised { get; }
    UiColor SurfaceOverlay { get; }
    UiColor SurfaceInset { get; }

    // Accent
    UiColor Accent { get; }
    UiColor AccentLight { get; }
    UiColor AccentDark { get; }

    // Text
    UiColor TextPrimary { get; }
    UiColor TextSecondary { get; }
    UiColor TextMuted { get; }
    UiColor TextDisabled { get; }

    // Borders
    UiColor Border { get; }
    UiColor BorderLight { get; }

    // Semantic
    UiColor Success { get; }
    UiColor Warning { get; }
    UiColor Error { get; }
    UiColor Info { get; }

    // Interactive states
    UiColor Hover { get; }
    UiColor Active { get; }
    UiColor Selected { get; }
    UiColor SelectedBorder { get; }
    UiColor FocusRing { get; }

    // Axis colors
    UiColor AxisX { get; }
    UiColor AxisY { get; }
    UiColor AxisZ { get; }

    // Component styling
    UiColor ComponentAccent { get; }
    UiColor ComponentBorder { get; }
    UiColor SectionHeaderBg { get; }

    // Scroll
    UiColor ScrollThumb { get; }
    UiColor ScrollThumbHover { get; }
    UiColor ScrollTrack { get; }

    // Drag label
    UiColor DragLabelBg { get; }
    UiColor DragLabelHover { get; }

    // Dialog overlay
    UiColor DialogOverlay { get; }

    // Input controls
    UiColor InputBackground { get; }
    UiColor InputBackgroundFocused { get; }
    UiColor InputText { get; }

    // Font sizes
    ushort FontSizeCaption { get; }
    ushort FontSizeBody { get; }
    ushort FontSizeBodyLarge { get; }
    ushort FontSizeSubtitle { get; }
    ushort FontSizeTitle { get; }
    ushort FontSizeHeader { get; }

    // Icon sizes
    ushort IconSizeXS { get; }
    ushort IconSizeSmall { get; }
    ushort IconSizeBase { get; }
    ushort IconSizeMedium { get; }
    ushort IconSizeLarge { get; }
    ushort IconSizeXL { get; }

    // Spacing
    float SpacingXS { get; }
    float SpacingSM { get; }
    float SpacingMD { get; }
    float SpacingLG { get; }
    float SpacingXL { get; }

    // Corner radii
    float RadiusNone { get; }
    float RadiusSmall { get; }
    float RadiusMedium { get; }
    float RadiusLarge { get; }

    // Panel dimensions
    float PanelMinWidth { get; }
    float PanelMaxWidth { get; }
    float PanelPreferredWidth { get; }

    // Pre-built text styles
    UiTextStyle CaptionStyle { get; }
    UiTextStyle BodyStyle { get; }
    UiTextStyle BodyLargeStyle { get; }
    UiTextStyle SubtitleStyle { get; }
    UiTextStyle TitleStyle { get; }
    UiTextStyle HeaderStyle { get; }
    UiTextStyle MutedStyle { get; }
    UiTextStyle SecondaryStyle { get; }
    UiTextStyle AccentStyle { get; }
}
