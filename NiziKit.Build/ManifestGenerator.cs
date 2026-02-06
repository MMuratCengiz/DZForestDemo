using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Build;

public class ManifestGenerator
{
    private static readonly Dictionary<string, string> ExtensionToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".dztex", "texture" },
        { ".nizimesh", "mesh" },
        { ".ozzskel", "skeleton" },
        { ".ozzanim", "animation" }
    };

    public static void Generate(string assetsDir, string outputDir)
    {
        var baseDir = Path.GetFullPath(assetsDir);
        const string packName = "default";

        // Scan all asset files and classify by extension
        var textures = new List<string>();
        var meshes = new List<string>();
        var skeletons = new List<string>();
        var animations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!ExtensionToType.TryGetValue(ext, out var assetType))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(baseDir, file).Replace('\\', '/');

            switch (assetType)
            {
                case "texture":
                    textures.Add(relativePath);
                    break;
                case "mesh":
                    meshes.Add(relativePath);
                    break;
                case "skeleton":
                    skeletons.Add(relativePath);
                    break;
                case "animation":
                    animations.Add(relativePath);
                    break;
            }
        }

        // Generate the default pack definition
        var packData = new PackData
        {
            Name = packName,
            Version = "1.0.0",
            Textures = textures,
            Meshes = meshes,
            Skeletons = skeletons,
            Animations = animations
        };

        // Ensure Packs directory exists
        var packsDir = Path.Combine(baseDir, "Packs");
        Directory.CreateDirectory(packsDir);

        // Write default.nizipack.json
        var packJsonPath = Path.Combine(packsDir, "default.nizipack.json");
        var packJson = JsonSerializer.Serialize(packData, JsonOptions);
        File.WriteAllText(packJsonPath, packJson);
        Console.WriteLine($"Generated pack definition at: {packJsonPath}");

        // Build the manifest
        var manifest = new GeneratedManifest
        {
            Version = "2.0.0",
            Packs = [],
            AssetIndex = new Dictionary<string, GeneratedAssetMapping>()
        };

        manifest.Packs.Add(new GeneratedPackEntry
        {
            Name = packName,
            Path = "Packs/default.nizipack.json",
            DeploymentName = $"{packName}.nizipack"
        });

        // Build reverse mapping: asset path -> pack info
        foreach (var path in textures)
        {
            manifest.AssetIndex[path] = new GeneratedAssetMapping { Pack = packName, Type = "texture" };
        }
        foreach (var path in meshes)
        {
            manifest.AssetIndex[path] = new GeneratedAssetMapping { Pack = packName, Type = "mesh" };
        }
        foreach (var path in skeletons)
        {
            manifest.AssetIndex[path] = new GeneratedAssetMapping { Pack = packName, Type = "skeleton" };
        }
        foreach (var path in animations)
        {
            manifest.AssetIndex[path] = new GeneratedAssetMapping { Pack = packName, Type = "animation" };
        }

        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Generated manifest at: {outputPath}");
        Console.WriteLine($"  Packs: {manifest.Packs.Count}");
        Console.WriteLine($"  Assets indexed: {manifest.AssetIndex.Count}");
        Console.WriteLine($"    Textures: {textures.Count}");
        Console.WriteLine($"    Meshes: {meshes.Count}");
        Console.WriteLine($"    Skeletons: {skeletons.Count}");
        Console.WriteLine($"    Animations: {animations.Count}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

internal class GeneratedManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0.0";

    [JsonPropertyName("packs")]
    public List<GeneratedPackEntry> Packs { get; set; } = [];

    [JsonPropertyName("assetIndex")]
    public Dictionary<string, GeneratedAssetMapping> AssetIndex { get; set; } = new();
}

internal class GeneratedPackEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("deploymentName")]
    public string? DeploymentName { get; set; }
}

internal class GeneratedAssetMapping
{
    [JsonPropertyName("pack")]
    public string Pack { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

internal class PackData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

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

}
