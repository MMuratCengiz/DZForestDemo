namespace DZForestDemo.AvaUi.Models;

public enum CombatEntryType
{
    Attack,
    Defense,
    Spell,
    Heal,
    Damage,
    Death,
    Info
}

public record TextSegment(
    string Text,
    bool IsHoverable = false,
    string? TooltipTitle = null,
    string? TooltipContent = null,
    string? TooltipIcon = null,
    string? ColorResourceKey = null);

public record CombatLogEntry(
    IReadOnlyList<TextSegment> Segments,
    CombatEntryType Type = CombatEntryType.Info)
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
