using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfBuffer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }
}
