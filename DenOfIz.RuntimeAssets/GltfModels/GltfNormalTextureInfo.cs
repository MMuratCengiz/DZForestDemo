using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfNormalTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1.0f;
}