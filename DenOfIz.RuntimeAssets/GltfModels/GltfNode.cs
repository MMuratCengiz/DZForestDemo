using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfNode
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("children")]
    public List<int>? Children { get; set; }

    [JsonPropertyName("mesh")]
    public int? Mesh { get; set; }

    [JsonPropertyName("skin")]
    public int? Skin { get; set; }

    [JsonPropertyName("camera")]
    public int? Camera { get; set; }

    [JsonPropertyName("matrix")]
    public float[]? Matrix { get; set; }

    [JsonPropertyName("translation")]
    public float[]? Translation { get; set; }

    [JsonPropertyName("rotation")]
    public float[]? Rotation { get; set; }

    [JsonPropertyName("scale")]
    public float[]? Scale { get; set; }

    [JsonPropertyName("weights")]
    public float[]? Weights { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}