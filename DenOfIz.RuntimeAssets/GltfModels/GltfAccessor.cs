using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfAccessor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("componentType")]
    public GltfComponentType ComponentType { get; set; }

    [JsonPropertyName("normalized")]
    public bool Normalized { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SCALAR";

    [JsonPropertyName("max")]
    public float[]? Max { get; set; }

    [JsonPropertyName("min")]
    public float[]? Min { get; set; }

    [JsonPropertyName("sparse")]
    public GltfSparse? Sparse { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}