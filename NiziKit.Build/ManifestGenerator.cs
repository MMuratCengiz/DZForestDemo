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
            var relativePath = Path.GetRelativePath(baseDir, packFile).Replace('\\', '/');
            manifest.Packs.Add(relativePath);
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
    public List<string> Packs { get; set; } = [];
}

