using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfAnimation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("channels")]
    public List<GltfAnimationChannel> Channels { get; set; } = [];

    [JsonPropertyName("samplers")]
    public List<GltfAnimationSampler> Samplers { get; set; } = [];

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}