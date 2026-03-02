using Avalonia.Input;
using DenOfIz;
using DZForestDemo.AvaUi.Models;
using DZForestDemo.AvaUi.Services;
using DZForestDemo.AvaUi.Views;
using NiziKit.Graphics.Resources;
using NiziKit.Skia;

namespace DZForestDemo.AvaUi;

public class DemoUi
{
    private readonly NiziAvalonia _ui;
    private readonly MainLayout _mainLayout = new();
    private readonly ChatService _chatService = new();
    private readonly CombatLogService _combatLogService = new();

    public DemoUi()
    {
        _ui = new NiziAvalonia
        {
            Content = _mainLayout
        };

        var chatWindow = _mainLayout.ChatWindow;
        chatWindow.BindChatService(_chatService);
        chatWindow.BindCombatLogService(_combatLogService);

        SeedDemoData();
    }

    private void SeedDemoData()
    {
        _chatService.AddSystemMessage("Welcome to the Whispering Woods. Tread carefully.");
        _chatService.AddMessage("Aldric", "I see movement ahead in the thicket. Ready your weapons.");
        _chatService.AddMessage("Seraphina", "My wards are in place. We are protected from ambush.", ChatMessageType.Party);
        _chatService.AddMessage("Theron", "The tracks here are fresh\u2014dire wolves, at least three.", ChatMessageType.Normal);
        _chatService.AddMessage("Mysterious Voice", "Turn back, mortals. This forest belongs to the old powers...", ChatMessageType.Whisper);
        _chatService.AddMessage("Aldric", "Stand firm. We press on.");
        _chatService.AddSystemMessage("A pack of Dire Wolves emerges from the shadows!");

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
    }

    public Texture? Texture => _ui.Texture;

    public void OnEvent(ref Event ev)
    {
        _ui.OnEvent(ref ev);
    }

    public void Update(float deltaTime)
    {
        _ui.Update(deltaTime);
    }
}
