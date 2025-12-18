using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

public sealed class GltfCamera
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "perspective";

    [JsonPropertyName("perspective")]
    public GltfCameraPerspective? Perspective { get; set; }

    [JsonPropertyName("orthographic")]
    public GltfCameraOrthographic? Orthographic { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}