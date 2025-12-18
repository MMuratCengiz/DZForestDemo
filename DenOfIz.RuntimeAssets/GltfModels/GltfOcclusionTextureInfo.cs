using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfOcclusionTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("strength")]
    public float Strength { get; set; } = 1.0f;
}