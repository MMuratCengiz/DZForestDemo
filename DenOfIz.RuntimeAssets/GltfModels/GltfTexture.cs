using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfTexture
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sampler")]
    public int? Sampler { get; set; }

    [JsonPropertyName("source")]
    public int? Source { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}