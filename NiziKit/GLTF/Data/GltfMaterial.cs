using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfMaterial
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pbrMetallicRoughness")]
    public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; set; }

    [JsonPropertyName("normalTexture")]
    public GltfNormalTextureInfo? NormalTexture { get; set; }

    [JsonPropertyName("occlusionTexture")]
    public GltfOcclusionTextureInfo? OcclusionTexture { get; set; }

    [JsonPropertyName("emissiveTexture")]
    public GltfTextureInfo? EmissiveTexture { get; set; }

    [JsonPropertyName("emissiveFactor")]
    public float[]? EmissiveFactor { get; set; }

    [JsonPropertyName("alphaMode")]
    public string AlphaMode { get; set; } = "OPAQUE";

    [JsonPropertyName("alphaCutoff")]
    public float AlphaCutoff { get; set; } = 0.5f;

    [JsonPropertyName("doubleSided")]
    public bool DoubleSided { get; set; }
}

public sealed class GltfPbrMetallicRoughness
{
    [JsonPropertyName("baseColorFactor")]
    public float[]? BaseColorFactor { get; set; }

    [JsonPropertyName("baseColorTexture")]
    public GltfTextureInfo? BaseColorTexture { get; set; }

    [JsonPropertyName("metallicFactor")]
    public float MetallicFactor { get; set; } = 1.0f;

    [JsonPropertyName("roughnessFactor")]
    public float RoughnessFactor { get; set; } = 1.0f;

    [JsonPropertyName("metallicRoughnessTexture")]
    public GltfTextureInfo? MetallicRoughnessTexture { get; set; }
}

public class GltfTextureInfo
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("texCoord")]
    public int TexCoord { get; set; }
}

public sealed class GltfNormalTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1.0f;
}

public sealed class GltfOcclusionTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("strength")]
    public float Strength { get; set; } = 1.0f;
}

public static class GltfAlphaMode
{
    public const string Opaque = "OPAQUE";
    public const string Mask = "MASK";
    public const string Blend = "BLEND";
}
