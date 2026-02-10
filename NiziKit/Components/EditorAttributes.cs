namespace NiziKit.Components;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SerializeFieldAttribute : Attribute
{
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DontSerializeAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ReadOnlyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RangeAttribute(float min, float max) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TooltipAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HeaderAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SpaceAttribute(float height = 8) : Attribute
{
    public float Height { get; } = height;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ColorAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AnimationSelectorAttribute(string skeletonPropertyName) : Attribute
{
    public string SkeletonPropertyName { get; } = skeletonPropertyName;
}
