using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfAnimationSampler
{
    [JsonPropertyName("input")]
    public int Input { get; set; }

    [JsonPropertyName("output")]
    public int Output { get; set; }

    [JsonPropertyName("interpolation")]
    public string Interpolation { get; set; } = "LINEAR";

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}