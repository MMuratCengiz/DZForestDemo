using NiziKit.UI;

namespace DZForestDemo.UI;

public static class GameTheme
{
    // Background / Surface
    public static readonly UiColor Background = UiColor.FromHex(0x1A1410);
    public static readonly UiColor Surface = UiColor.FromHex(0x252018);
    public static readonly UiColor Border = UiColor.FromHex(0x4A3C28);
    public static readonly UiColor InputBg = UiColor.FromHex(0x1E1812);

    // Text
    public static readonly UiColor TextPrimary = UiColor.FromHex(0xE8DCC8);
    public static readonly UiColor TextSecondary = UiColor.FromHex(0xB8A888);
    public static readonly UiColor TextGold = UiColor.FromHex(0xC8A84E);
    public static readonly UiColor TextMuted = UiColor.FromHex(0x807060);

    // Chat type colors
    public static readonly UiColor ChatNormal = UiColor.FromHex(0xE8DCC8);
    public static readonly UiColor ChatSystem = UiColor.FromHex(0x6898C8);
    public static readonly UiColor ChatWhisper = UiColor.FromHex(0xB888D8);
    public static readonly UiColor ChatParty = UiColor.FromHex(0x68C888);

    // Combat type colors
    public static readonly UiColor CombatAttack = UiColor.FromHex(0xD8A048);
    public static readonly UiColor CombatDefense = UiColor.FromHex(0x6898C8);
    public static readonly UiColor CombatSpell = UiColor.FromHex(0xB888D8);
    public static readonly UiColor CombatHeal = UiColor.FromHex(0x48C878);
    public static readonly UiColor CombatDamage = UiColor.FromHex(0xD85848);
    public static readonly UiColor CombatDeath = UiColor.FromHex(0xA83828);

    // Tooltip
    public static readonly UiColor TooltipBg = UiColor.FromHex(0x2A2218);
    public static readonly UiColor TooltipBorder = UiColor.FromHex(0x5A4C38);

    // Row alternation
    public static readonly UiColor RowEven = UiColor.Transparent;
    public static readonly UiColor RowOdd = UiColor.Rgba(232, 220, 200, 24);

    // Tab
    public static readonly UiColor TabHover = UiColor.Rgba(232, 220, 200, 48);
    public static readonly UiColor TabSelected = UiColor.FromHex(0x252018);
    public static readonly UiColor TabSeparator = UiColor.FromHex(0x4A3C28);

    // Font sizes
    public const ushort FontBody = 14;
    public const ushort FontTimestamp = 12;
    public const ushort FontPrefix = 13;
    public const ushort FontTooltipTitle = 14;
    public const ushort FontTooltipBody = 12;

    public static void RegisterStyles()
    {
        NiziUi.RegisterClass("scrollPanel", new UiStyleClass
        {
            Background = Surface,
            Border = new UiBorder(1, 1, 1, 1, Border),
            CornerRadius = 4,
            Width = UiSizing.Grow(),
            Height = UiSizing.Grow(),
            ScrollVertical = true
        });

        NiziUi.RegisterClass("messageRow", new UiStyleClass
        {
            Width = UiSizing.Grow(),
            Height = UiSizing.Fit(),
            Padding = UiPadding.Symmetric(6, 4)
        });

        NiziUi.RegisterClass("entryRow", new UiStyleClass
        {
            Width = UiSizing.Grow(),
            Height = UiSizing.Fit(),
            Padding = UiPadding.Symmetric(6, 4),
            Gap = 1,
            AlignY = UiAlignY.Center
        });

        NiziUi.RegisterClass("tooltip", new UiStyleClass
        {
            Background = TooltipBg,
            Border = new UiBorder(1, 1, 1, 1, TooltipBorder),
            CornerRadius = 6,
            Padding = UiPadding.All(12),
            Gap = 6,
            Direction = UiDirection.Vertical
        });

        NiziUi.RegisterTextClass("body", new UiTextStyleClass
        {
            Color = TextPrimary,
            FontSize = FontBody,
            WrapMode = UiTextWrap.Words
        });

        NiziUi.RegisterTextClass("timestamp", new UiTextStyleClass
        {
            Color = TextMuted,
            FontSize = FontTimestamp
        });

        NiziUi.RegisterTextClass("tooltipTitle", new UiTextStyleClass
        {
            Color = TextGold,
            FontSize = FontTooltipTitle
        });

        NiziUi.RegisterTextClass("tooltipBody", new UiTextStyleClass
        {
            Color = TextSecondary,
            FontSize = FontTooltipBody
        });
    }

    public static UiColor ResolveColorKey(string? colorKey)
    {
        if (colorKey == null)
        {
            return TextPrimary;
        }

        return colorKey switch
        {
            "CombatTextGold" => TextGold,
            "CombatDamage" => CombatDamage,
            "CombatSpell" => CombatSpell,
            "CombatHeal" => CombatHeal,
            "CombatDeath" => CombatDeath,
            "CombatDefense" => CombatDefense,
            "CombatAttack" => CombatAttack,
            _ => TextPrimary
        };
    }
}
