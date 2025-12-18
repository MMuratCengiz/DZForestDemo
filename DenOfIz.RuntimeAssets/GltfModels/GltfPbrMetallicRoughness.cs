using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfPbrMetallicRoughness
{
    [JsonPropertyName("baseColorFactor")]
    public float[]? BaseColorFactor { get; set; }

    [JsonPropertyName("baseColorTexture")]
    public GltfTextureInfo? BaseColorTexture { get; set; }

    [JsonPropertyName("metallicFactor")]
    public float MetallicFactor { get; set; } = 1.0f;

    [JsonPropertyName("roughnessFactor")]
    public float RoughnessFactor { get; set; } = 1.0f;

    [JsonPropertyName("metallicRoughnessTexture")]
    public GltfTextureInfo? MetallicRoughnessTexture { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}