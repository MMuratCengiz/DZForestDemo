using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfScene
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("nodes")]
    public List<int>? Nodes { get; set; }
}
