using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfBufferView
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("buffer")]
    public int Buffer { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    [JsonPropertyName("byteStride")]
    public int ByteStride { get; set; }

    [JsonPropertyName("target")]
    public int? Target { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}