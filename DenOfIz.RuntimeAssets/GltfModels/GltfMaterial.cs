using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfMaterial
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pbrMetallicRoughness")]
    public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; set; }

    [JsonPropertyName("normalTexture")]
    public GltfNormalTextureInfo? NormalTexture { get; set; }

    [JsonPropertyName("occlusionTexture")]
    public GltfOcclusionTextureInfo? OcclusionTexture { get; set; }

    [JsonPropertyName("emissiveTexture")]
    public GltfTextureInfo? EmissiveTexture { get; set; }

    [JsonPropertyName("emissiveFactor")]
    public float[]? EmissiveFactor { get; set; }

    [JsonPropertyName("alphaMode")]
    public string? AlphaMode { get; set; }

    [JsonPropertyName("alphaCutoff")]
    public float? AlphaCutoff { get; set; }

    [JsonPropertyName("doubleSided")]
    public bool DoubleSided { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}