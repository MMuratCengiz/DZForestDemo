using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfBufferView
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("buffer")]
    public int Buffer { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    [JsonPropertyName("byteStride")]
    public int? ByteStride { get; set; }

    [JsonPropertyName("target")]
    public int? Target { get; set; }
}

public static class GltfBufferTarget
{
    public const int ArrayBuffer = 34962;
    public const int ElementArrayBuffer = 34963;
}
