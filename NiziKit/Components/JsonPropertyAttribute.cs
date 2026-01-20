namespace NiziKit.Components;

/// <summary>
/// Marks a property for JSON serialization with a custom name.
/// Used by the source generator to map JSON properties to component properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class JsonPropertyAttribute(string name) : Attribute
{
    /// <summary>
    /// The name of the property in JSON.
    /// </summary>
    public string Name { get; } = name;
}
