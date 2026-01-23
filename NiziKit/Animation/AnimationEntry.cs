using NiziKit.Components;

namespace NiziKit.Animation;

public class AnimationEntry
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("source")]
    public string? SourceRef { get; set; }

    public bool IsExternal => !string.IsNullOrEmpty(SourceRef);

    public AnimationEntry()
    {
    }

    public AnimationEntry(string name, string? sourceRef = null)
    {
        Name = name;
        SourceRef = sourceRef;
    }

    public static AnimationEntry FromSkeleton(string animationName)
    {
        return new AnimationEntry(animationName);
    }

    public static AnimationEntry External(string name, string sourceRef)
    {
        return new AnimationEntry(name, sourceRef);
    }

    public override string ToString()
    {
        return IsExternal ? $"{Name} (from {SourceRef})" : Name;
    }
}
