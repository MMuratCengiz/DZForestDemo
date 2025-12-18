using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfSampler
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("magFilter")]
    public int? MagFilter { get; set; }

    [JsonPropertyName("minFilter")]
    public int? MinFilter { get; set; }

    [JsonPropertyName("wrapS")]
    public int WrapS { get; set; } = 10497; // REPEAT

    [JsonPropertyName("wrapT")]
    public int WrapT { get; set; } = 10497; // REPEAT

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}