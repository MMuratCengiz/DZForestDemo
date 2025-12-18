using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfBuffer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}