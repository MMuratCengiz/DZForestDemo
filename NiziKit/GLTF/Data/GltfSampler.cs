using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfSampler
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("magFilter")]
    public int? MagFilter { get; set; }

    [JsonPropertyName("minFilter")]
    public int? MinFilter { get; set; }

    [JsonPropertyName("wrapS")]
    public int WrapS { get; set; } = GltfWrap.Repeat;

    [JsonPropertyName("wrapT")]
    public int WrapT { get; set; } = GltfWrap.Repeat;
}

public static class GltfFilter
{
    public const int Nearest = 9728;
    public const int Linear = 9729;
    public const int NearestMipmapNearest = 9984;
    public const int LinearMipmapNearest = 9985;
    public const int NearestMipmapLinear = 9986;
    public const int LinearMipmapLinear = 9987;
}

public static class GltfWrap
{
    public const int ClampToEdge = 33071;
    public const int MirroredRepeat = 33648;
    public const int Repeat = 10497;
}
