using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfMesh
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("primitives")]
    public List<GltfPrimitive> Primitives { get; set; } = [];

    [JsonPropertyName("weights")]
    public float[]? Weights { get; set; }
}

public sealed class GltfPrimitive
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, int> Attributes { get; set; } = [];

    [JsonPropertyName("indices")]
    public int? Indices { get; set; }

    [JsonPropertyName("material")]
    public int? Material { get; set; }

    [JsonPropertyName("mode")]
    public int Mode { get; set; } = 4;

    [JsonPropertyName("targets")]
    public List<Dictionary<string, int>>? Targets { get; set; }
}

public static class GltfPrimitiveMode
{
    public const int Points = 0;
    public const int Lines = 1;
    public const int LineLoop = 2;
    public const int LineStrip = 3;
    public const int Triangles = 4;
    public const int TriangleStrip = 5;
    public const int TriangleFan = 6;
}
