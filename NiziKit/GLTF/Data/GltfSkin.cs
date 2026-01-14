using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

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
}
