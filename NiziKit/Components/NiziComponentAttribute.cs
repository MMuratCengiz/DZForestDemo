namespace NiziKit.Components;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NiziComponentAttribute : Attribute
{
    /// <summary>
    /// The type name used in JSON to identify this component.
    /// If not specified, uses the class name without "Component" suffix in lowercase.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Whether to generate a component factory for JSON deserialization.
    /// Default is true.
    /// </summary>
    public bool GenerateFactory { get; set; } = true;
}
