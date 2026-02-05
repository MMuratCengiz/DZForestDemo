using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Build;

public class ManifestGenerator
{
    public static void Generate(string assetsDir, string outputDir)
    {
        var manifest = new GeneratedManifest
        {
            Version = "2.0.0",
            Packs = [],
            AssetIndex = new Dictionary<string, GeneratedAssetMapping>()
        };

        var baseDir = Path.GetFullPath(assetsDir);
        foreach (var packFile in Directory.EnumerateFiles(baseDir, "*.nizipack.json", SearchOption.AllDirectories))
        {
            try
            {
                var packJson = File.ReadAllText(packFile);
                var packData = JsonSerializer.Deserialize<PackData>(packJson, JsonOptions);
                if (packData == null)
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(baseDir, packFile).Replace('\\', '/');
                var packName = packData.Name;

                manifest.Packs.Add(new GeneratedPackEntry
                {
                    Name = packName,
                    Path = relativePath,
                    DeploymentName = $"{packName}.nizipack"
                });

                // Build reverse mapping: asset path -> pack info
                foreach (var texturePath in packData.Textures)
                {
                    manifest.AssetIndex[texturePath] = new GeneratedAssetMapping { Pack = packName, Type = "texture" };
                }
                foreach (var meshPath in packData.Meshes)
                {
                    manifest.AssetIndex[meshPath] = new GeneratedAssetMapping { Pack = packName, Type = "mesh" };
                }
                foreach (var skeletonPath in packData.Skeletons)
                {
                    manifest.AssetIndex[skeletonPath] = new GeneratedAssetMapping { Pack = packName, Type = "skeleton" };
                }
                foreach (var animationPath in packData.Animations)
                {
                    manifest.AssetIndex[animationPath] = new GeneratedAssetMapping { Pack = packName, Type = "animation" };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to read pack {packFile}: {ex.Message}");
            }
        }

        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Generated manifest at: {outputPath}");
        Console.WriteLine($"  Packs: {manifest.Packs.Count}");
        Console.WriteLine($"  Assets indexed: {manifest.AssetIndex.Count}");
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
