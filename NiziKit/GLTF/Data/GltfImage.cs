using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfImage
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }
}
