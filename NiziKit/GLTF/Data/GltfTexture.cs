using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfTexture
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sampler")]
    public int? Sampler { get; set; }

    [JsonPropertyName("source")]
    public int? Source { get; set; }
}
