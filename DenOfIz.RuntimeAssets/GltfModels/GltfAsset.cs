using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfAsset
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("generator")]
    public string? Generator { get; set; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("minVersion")]
    public string? MinVersion { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}