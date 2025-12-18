using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfPrimitive
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, int> Attributes { get; set; } = [];

    [JsonPropertyName("indices")]
    public int? Indices { get; set; }

    [JsonPropertyName("material")]
    public int? Material { get; set; }

    [JsonPropertyName("mode")]
    public GltfPrimitiveMode Mode { get; set; } = GltfPrimitiveMode.Triangles;

    [JsonPropertyName("targets")]
    public List<Dictionary<string, int>>? Targets { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}