namespace NiziKit.Components;

/// <summary>
/// Types of assets that can be referenced in JSON.
/// </summary>
public enum AssetRefType
{
    Mesh,
    Material,
    Texture,
    Shader,
    Skeleton,
    Animation
}

/// <summary>
/// Marks a property as an asset reference that should be resolved from JSON.
/// Used by the source generator to resolve asset references during component creation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class AssetRefAttribute(AssetRefType assetType, string jsonPropertyName) : Attribute
{
    /// <summary>
    /// The type of asset this property references.
    /// </summary>
    public AssetRefType AssetType { get; } = assetType;

    /// <summary>
    /// The name of the JSON property containing the asset reference.
    /// </summary>
    public string JsonPropertyName { get; } = jsonPropertyName;
}
