using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfAnimation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("channels")]
    public List<GltfAnimationChannel> Channels { get; set; } = [];

    [JsonPropertyName("samplers")]
    public List<GltfAnimationSampler> Samplers { get; set; } = [];
}

public sealed class GltfAnimationChannel
{
    [JsonPropertyName("sampler")]
    public int Sampler { get; set; }

    [JsonPropertyName("target")]
    public GltfAnimationTarget Target { get; set; } = new();
}

public sealed class GltfAnimationTarget
{
    [JsonPropertyName("node")]
    public int? Node { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
}

public sealed class GltfAnimationSampler
{
    [JsonPropertyName("input")]
    public int Input { get; set; }

    [JsonPropertyName("output")]
    public int Output { get; set; }

    [JsonPropertyName("interpolation")]
    public string Interpolation { get; set; } = "LINEAR";
}

public static class GltfAnimationPath
{
    public const string Translation = "translation";
    public const string Rotation = "rotation";
    public const string Scale = "scale";
    public const string Weights = "weights";
}

public static class GltfInterpolation
{
    public const string Linear = "LINEAR";
    public const string Step = "STEP";
    public const string CubicSpline = "CUBICSPLINE";
}
