using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Build;

public class ManifestGenerator
{
    public static void Generate(string assetsDir, string outputDir)
    {
        var manifest = new GeneratedManifest
        {
            Version = "1.0.0",
            Packs = []
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
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("packs")]
    public List<GeneratedPackEntry> Packs { get; set; } = [];
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

internal class PackData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}
