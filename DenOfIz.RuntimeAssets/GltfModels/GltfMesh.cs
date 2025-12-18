using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfMesh
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("primitives")]
    public List<GltfPrimitive> Primitives { get; set; } = [];

    [JsonPropertyName("weights")]
    public float[]? Weights { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}