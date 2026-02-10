using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Assets.Serde;

public sealed class AssetPackJson
{
    public static readonly Dictionary<string, string> ExtensionToAssetType = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".dztex", "texture" },
        { ".nizimesh", "mesh" },
        { ".ozzskel", "skeleton" },
        { ".ozzanim", "animation" }
    };

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("textures")]
    public List<string> Textures { get; set; } = [];

    [JsonPropertyName("meshes")]
    public List<string> Meshes { get; set; } = [];

    [JsonPropertyName("skeletons")]
    public List<string> Skeletons { get; set; } = [];

    [JsonPropertyName("animations")]
    public List<string> Animations { get; set; } = [];

    public static AssetPackJson FromFilePaths(string name, IEnumerable<string> paths)
    {
        var pack = new AssetPackJson { Name = name };
        foreach (var path in paths)
        {
            var ext = Path.GetExtension(path);
            if (!ExtensionToAssetType.TryGetValue(ext, out var assetType))
            {
                continue;
            }

            switch (assetType)
            {
                case "texture": pack.Textures.Add(path); break;
                case "mesh": pack.Meshes.Add(path); break;
                case "skeleton": pack.Skeletons.Add(path); break;
                case "animation": pack.Animations.Add(path); break;
            }
        }
        return pack;
    }

    public static AssetPackJson FromJson(string json)
        => JsonSerializer.Deserialize<AssetPackJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public static async Task<AssetPackJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<AssetPackJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);
}
