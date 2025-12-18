using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfCameraOrthographic
{
    [JsonPropertyName("xmag")]
    public float Xmag { get; set; }

    [JsonPropertyName("ymag")]
    public float Ymag { get; set; }

    [JsonPropertyName("zfar")]
    public float Zfar { get; set; }

    [JsonPropertyName("znear")]
    public float Znear { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}