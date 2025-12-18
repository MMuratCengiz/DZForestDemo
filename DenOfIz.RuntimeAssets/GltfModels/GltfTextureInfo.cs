using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public class GltfTextureInfo
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("texCoord")]
    public int TexCoord { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}