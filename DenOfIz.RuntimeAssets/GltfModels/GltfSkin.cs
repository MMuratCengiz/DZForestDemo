using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfSkin
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("inverseBindMatrices")]
    public int? InverseBindMatrices { get; set; }

    [JsonPropertyName("skeleton")]
    public int? Skeleton { get; set; }

    [JsonPropertyName("joints")]
    public List<int> Joints { get; set; } = [];

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}