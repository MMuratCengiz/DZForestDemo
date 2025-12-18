using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfCameraPerspective
{
    [JsonPropertyName("aspectRatio")]
    public float? AspectRatio { get; set; }

    [JsonPropertyName("yfov")]
    public float Yfov { get; set; }

    [JsonPropertyName("zfar")]
    public float? Zfar { get; set; }

    [JsonPropertyName("znear")]
    public float Znear { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}