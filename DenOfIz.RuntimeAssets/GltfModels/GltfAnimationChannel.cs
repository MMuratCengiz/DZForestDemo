using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfAnimationChannel
{
    [JsonPropertyName("sampler")]
    public int Sampler { get; set; }

    [JsonPropertyName("target")]
    public GltfAnimationTarget Target { get; set; } = new();

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}