using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfSparse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("indices")]
    public GltfSparseIndices Indices { get; set; } = new();

    [JsonPropertyName("values")]
    public GltfSparseValues Values { get; set; } = new();

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}