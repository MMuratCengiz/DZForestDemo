using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfRoot
{
    [JsonPropertyName("asset")]
    public GltfAsset Asset { get; set; } = new();

    [JsonPropertyName("scene")]
    public int? Scene { get; set; }

    [JsonPropertyName("scenes")]
    public List<GltfScene>? Scenes { get; set; }

    [JsonPropertyName("nodes")]
    public List<GltfNode>? Nodes { get; set; }

    [JsonPropertyName("meshes")]
    public List<GltfMesh>? Meshes { get; set; }

    [JsonPropertyName("accessors")]
    public List<GltfAccessor>? Accessors { get; set; }

    [JsonPropertyName("bufferViews")]
    public List<GltfBufferView>? BufferViews { get; set; }

    [JsonPropertyName("buffers")]
    public List<GltfBuffer>? Buffers { get; set; }

    [JsonPropertyName("materials")]
    public List<GltfMaterial>? Materials { get; set; }

    [JsonPropertyName("textures")]
    public List<GltfTexture>? Textures { get; set; }

    [JsonPropertyName("images")]
    public List<GltfImage>? Images { get; set; }

    [JsonPropertyName("samplers")]
    public List<GltfSampler>? Samplers { get; set; }

    [JsonPropertyName("skins")]
    public List<GltfSkin>? Skins { get; set; }

    [JsonPropertyName("animations")]
    public List<GltfAnimation>? Animations { get; set; }

    [JsonPropertyName("extensionsUsed")]
    public List<string>? ExtensionsUsed { get; set; }

    [JsonPropertyName("extensionsRequired")]
    public List<string>? ExtensionsRequired { get; set; }
}

public sealed class GltfAsset
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("generator")]
    public string? Generator { get; set; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("minVersion")]
    public string? MinVersion { get; set; }
}
