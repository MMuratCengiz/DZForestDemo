using NiziKit.UI;

namespace DZForestDemo.UI;

public class DemoGameUi
{
    private readonly ChatBox _chatBox = new();
    private readonly CombatLogBox _combatLogBox = new();
    private readonly ChatService _chatService = new();
    private readonly CombatLogService _combatLogService = new();
    private string _currentChatText = "";
    private int _activeTab; // 0 = Chat, 1 = Combat Log

    private static readonly string[] TabLabels = ["Chat", "Combat Log"];

    public DemoGameUi()
    {
        GameTheme.RegisterStyles();
        SeedDemoData();
    }

    public void Render()
    {
        // Outer container: anchor to bottom-left
        using (NiziUi.Panel("ChatPanel")
                   .GrowHeight()
                   .GrowWidth(500, 750)
                   .Padding(15)
                   .Background(UiColor.Transparent)
                   .AlignChildren(UiAlignX.Left, UiAlignY.Bottom)
                   .Open())
        {
            // Main chat window
            using (NiziUi.Column("ChatWindow")
                       .GrowWidth()
                       .GrowHeight(500, 600)
                       .Background(GameTheme.Background)
                       .Border(1, GameTheme.Border)
                       .CornerRadius(6)
                       .Open())
            {
                NiziUi.TabControl("ChatTabs")
                    .TabBarBackground(GameTheme.Surface)
                    .SelectedColor(GameTheme.Background, GameTheme.TextGold)
                    .DefaultColor(GameTheme.Surface, GameTheme.TextSecondary)
                    .HoverColor(GameTheme.TabHover)
                    .Indicator(GameTheme.TextGold, 2)
                    .Separator(GameTheme.TabSeparator, 1)
                    .FontSize(GameTheme.FontBody)
                    .Show(TabLabels, ref _activeTab);

                // Content area
                using (NiziUi.Row("ContentArea").GrowWidth().GrowHeight().Padding(4).Open())
                {
                    if (_activeTab == 0)
                    {
                        _chatBox.Render();
                    }
                    else
                    {
                        _combatLogBox.Render();
                    }
                }

                // Input row (only for chat tab)
                if (_activeTab == 0)
                {
                    using (NiziUi.Row("InputArea").GrowWidth().Padding(4).Gap(4).Open())
                    {
                        var input = NiziUi
                            .TextField("ChatInput", ref _currentChatText)
                            .GrowWidth()
                            .BackgroundColor(GameTheme.InputBg, GameTheme.Surface)
                            .TextColor(GameTheme.TextPrimary)
                            .BorderColor(GameTheme.Border, GameTheme.TextGold)
                            .PlaceholderColor(GameTheme.TextMuted)
                            .Placeholder("Type a message...")
                            .CornerRadius(4);

                        input.Show();
                        if (input.SubmittedThisFrame && !string.IsNullOrWhiteSpace(_currentChatText))
                        {
                            _chatService.AddMessage("You", _currentChatText);
                            _chatBox.AddMessage(_chatService.Messages[^1]);
                            _currentChatText = "";
                        }
                    }
                }
            }
        }
    }

    private void SeedDemoData()
    {
        // Chat messages
        _chatService.AddSystemMessage("Welcome to the Whispering Woods. Tread carefully.");
        _chatService.AddMessage("Aldric", "I see movement ahead in the thicket. Ready your weapons.");
        _chatService.AddMessage("Seraphina", "My wards are in place. We are protected from ambush.", ChatMessageType.Party);
        _chatService.AddMessage("Theron", "The tracks here are fresh\u2014dire wolves, at least three.");
        _chatService.AddMessage("Mysterious Voice", "Turn back, mortals. This forest belongs to the old powers...", ChatMessageType.Whisper);
        _chatService.AddMessage("Aldric", "Stand firm. We press on.");
        _chatService.AddSystemMessage("A pack of Dire Wolves emerges from the shadows!");

        foreach (var msg in _chatService.Messages)
        {
            _chatBox.AddMessage(msg);
        }

        // Combat log entries
        _combatLogService.AddAttack(
            "Aldric", "Dire Wolf Alpha", "Flamebrand Longsword", 24,
            "Level 8 Human Fighter\nSTR 18 | DEX 14 | CON 16\nHP: 84/84",
            "Dire Wolf Alpha (CR 3)\nHP: 52/76\nResistance: Cold",
            "Flamebrand Longsword (+1)\nDamage: 1d8+5 Fire\nRequires attunement");

        _combatLogService.AddEntry(new CombatLogEntry(new List<TextSegment>
        {
            new("Dire Wolf Alpha", true, "Dire Wolf Alpha", "CR 3 Beast\nHP: 52/76\nPack Tactics: Advantage when ally is adjacent", "\ud83d\udc3a", "CombatTextGold"),
            new(" bites at "),
            new("Aldric", true, "Aldric", "Level 8 Human Fighter\nAC: 20 (Plate + Shield)", "\ud83d\udee1", "CombatTextGold"),
            new(" but "),
            new("misses", false, null, null, null, "CombatDefense"),
            new("! (AC 20)")
        }, CombatEntryType.Defense));

        _combatLogService.AddSpell(
            "Seraphina", "Scorching Ray", "Dire Wolf Beta", 18,
            "Level 6 Half-Elf Sorcerer\nCHA 20 | CON 14\nHP: 38/38\nSorcery Points: 4/6",
            "Scorching Ray (2nd Level Evocation)\nRange: 120 ft\nThree rays, 2d6 fire each\nRanged spell attack per ray",
            "Dire Wolf Beta (CR 1)\nHP: 19/37\nVulnerability: Fire");

        _combatLogService.AddHeal(
            "Brother Caelum", "Prayer of Healing", "Theron", 14,
            "Level 5 Dwarf Cleric (Life Domain)\nWIS 18 | CON 16\nHP: 45/45\nChannel Divinity: 1/1",
            "Prayer of Healing (2nd Level Evocation)\nHeals 2d8 + WIS modifier\nCasting time: 10 minutes\nLife Domain: +4 bonus healing",
            "Level 7 Wood Elf Ranger\nHP: 42/56\nFavored Enemy: Beasts");

        _combatLogService.AddAttack(
            "Theron", "Dire Wolf Beta", "Oathbow", 31,
            "Level 7 Wood Elf Ranger\nDEX 20 | WIS 16\nHP: 42/56\nHunter's Mark active",
            "Dire Wolf Beta (CR 1)\nHP: 19/37\nVulnerability: Fire",
            "Oathbow (Legendary)\nDamage: 1d8+5 + 3d6 (sworn enemy)\n\"Swift defeat to my enemies\"");

        _combatLogService.AddDeath("Dire Wolf Beta",
            "Dire Wolf Beta (CR 1)\nFelled by Theron's arrow\nXP: 200 each");

        _combatLogService.AddInfo("Initiative order: Aldric (19), Seraphina (17), Dire Wolf Alpha (15), Theron (12), Brother Caelum (8)");

        foreach (var entry in _combatLogService.Entries)
        {
            _combatLogBox.AddEntry(entry);
        }
    }
}
