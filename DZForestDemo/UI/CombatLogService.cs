namespace DZForestDemo.UI;

public class CombatLogService
{
    private const int MaxEntries = 300;

    public List<CombatLogEntry> Entries { get; } = [];

    public void AddEntry(CombatLogEntry entry)
    {
        Entries.Add(entry);
        TrimExcess();
    }

    public void AddAttack(string attacker, string target, string weapon, int damage,
        string? attackerTooltip = null, string? targetTooltip = null, string? weaponTooltip = null)
    {
        var segments = new List<TextSegment>
        {
            new(attacker, true, attacker, attackerTooltip ?? "Level ?? Adventurer", "\u2694", "CombatTextGold"),
            new(" strikes "),
            new(target, true, target, targetTooltip ?? "A hostile creature", "\ud83d\udc7e", "CombatTextGold"),
            new(" with "),
            new(weapon, true, weapon, weaponTooltip ?? "A sturdy weapon", "\ud83d\udde1", "CombatTextGold"),
            new(" for "),
            new($"{damage}", false, null, null, null, "CombatDamage"),
            new(" damage.")
        };

        AddEntry(new CombatLogEntry(segments, CombatEntryType.Attack));
    }

    public void AddSpell(string caster, string spell, string target, int damage,
        string? casterTooltip = null, string? spellTooltip = null, string? targetTooltip = null)
    {
        var segments = new List<TextSegment>
        {
            new(caster, true, caster, casterTooltip ?? "A skilled spellcaster", "\ud83e\uddd9", "CombatTextGold"),
            new(" casts "),
            new(spell, true, spell, spellTooltip ?? "A magical spell", "\u2728", "CombatSpell"),
            new(" on "),
            new(target, true, target, targetTooltip ?? "A hostile creature", "\ud83d\udc7e", "CombatTextGold"),
            new(" for "),
            new($"{damage}", false, null, null, null, "CombatSpell"),
            new(" damage.")
        };

        AddEntry(new CombatLogEntry(segments, CombatEntryType.Spell));
    }

    public void AddHeal(string healer, string spell, string target, int amount,
        string? healerTooltip = null, string? spellTooltip = null, string? targetTooltip = null)
    {
        var segments = new List<TextSegment>
        {
            new(healer, true, healer, healerTooltip ?? "A devoted healer", "\u2695", "CombatTextGold"),
            new(" heals "),
            new(target, true, target, targetTooltip ?? "An ally", "\ud83d\udee1", "CombatTextGold"),
            new(" with "),
            new(spell, true, spell, spellTooltip ?? "A healing spell", "\ud83d\udc9a", "CombatHeal"),
            new(" for "),
            new($"+{amount}", false, null, null, null, "CombatHeal"),
            new(" HP.")
        };

        AddEntry(new CombatLogEntry(segments, CombatEntryType.Heal));
    }

    public void AddDeath(string target, string? tooltip = null)
    {
        var segments = new List<TextSegment>
        {
            new(target, true, target, tooltip ?? "Has fallen in battle", "\ud83d\udc80", "CombatDeath"),
            new(" has been slain!", false, null, null, null, "CombatDeath")
        };

        AddEntry(new CombatLogEntry(segments, CombatEntryType.Death));
    }

    public void AddInfo(string text)
    {
        var segments = new List<TextSegment> { new(text) };
        AddEntry(new CombatLogEntry(segments, CombatEntryType.Info));
    }

    private void TrimExcess()
    {
        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(0);
        }
    }
}
