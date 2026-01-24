using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NiziKit.Assets.Serde;

namespace NiziKit.ContentPipeline;

public sealed class ManifestOptions
{
    public static readonly string[] DefaultExcludes =
    [
        ".git",
        ".git/**",
        ".svn",
        ".svn/**",
        ".hg",
        ".hg/**",
        "node_modules/**",
        "*.meta",
        ".DS_Store",
        "Thumbs.db",
        "*.tmp",
        "*.bak",
        "*~"
    ];

    public List<string> Includes { get; set; } = ["**/*"];
    public List<string> Excludes { get; set; } = [.. DefaultExcludes];
    public List<string> ForceIncludes { get; set; } = [];

    public static ManifestOptions LoadFromDirectory(string directory)
    {
        var configPath = Path.Combine(directory, ".niziassets");
        if (!File.Exists(configPath))
        {
            return new ManifestOptions();
        }

        var options = new ManifestOptions();
        foreach (var line in File.ReadAllLines(configPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith('!'))
            {
                options.ForceIncludes.Add(trimmed[1..]);
            }
            else
            {
                options.Excludes.Add(trimmed);
            }
        }

        return options;
    }

    public bool ShouldInclude(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', '/');

        foreach (var forceInclude in ForceIncludes)
        {
            if (MatchesGlob(normalizedPath, forceInclude))
            {
                return true;
            }
        }

        foreach (var exclude in Excludes)
        {
            if (MatchesGlob(normalizedPath, exclude))
            {
                return false;
            }
        }

        foreach (var include in Includes)
        {
            if (MatchesGlob(normalizedPath, include))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesGlob(string path, string pattern)
    {
        if (pattern is "**/*" or "**")
        {
            return true;
        }

        var segments = path.Split('/');

        if (!pattern.Contains('/'))
        {
            foreach (var segment in segments)
            {
                if (MatchesSimpleGlob(segment, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        if (pattern.EndsWith("/**/*") || pattern.EndsWith("/**"))
        {
            var prefix = pattern.EndsWith("/**/*") ? pattern[..^5] : pattern[..^3];
            return path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                   path.Equals(prefix, StringComparison.OrdinalIgnoreCase);
        }

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", ".") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    private static bool MatchesSimpleGlob(string text, string pattern)
    {
        if (pattern == "*")
        {
            return true;
        }

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";

        return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
    }
}

public sealed class AssetManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("assets")]
    public List<AssetEntry> Assets { get; set; } = [];

    [JsonPropertyName("packs")]
    public List<string>? Packs { get; set; }

    private Dictionary<string, AssetEntry>? _lookup;

    public bool TryGetAsset(string path, out AssetEntry? entry)
    {
        _lookup ??= Assets.ToDictionary(a => a.Path, StringComparer.OrdinalIgnoreCase);
        return _lookup.TryGetValue(NormalizePath(path), out entry);
    }

    public bool Contains(string path) => TryGetAsset(path, out _);

    public IEnumerable<string> EnumerateDirectory(string directory, string pattern = "*")
    {
        var normalizedDir = NormalizePath(directory).TrimEnd('/');
        if (string.IsNullOrEmpty(normalizedDir))
        {
            normalizedDir = "";
        }
        else
        {
            normalizedDir += "/";
        }

        foreach (var asset in Assets)
        {
            if (!asset.Path.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = asset.Path[normalizedDir.Length..];
            if (relativePath.Contains('/'))
            {
                continue;
            }

            if (pattern == "*" || MatchesPattern(relativePath, pattern))
            {
                yield return asset.Path;
            }
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);

    public static AssetManifest FromJson(string json) =>
        JsonSerializer.Deserialize<AssetManifest>(json, NiziJsonSerializationOptions.Default) ?? new AssetManifest();

    public static async Task<AssetManifest> LoadAsync(IContentProvider provider, string path = "manifest.json", CancellationToken ct = default)
    {
        var json = await provider.ReadAllTextAsync(path, ct);
        return FromJson(json);
    }

    public static AssetManifest GenerateFromDirectory(string directory, ManifestOptions? options = null)
    {
        options ??= ManifestOptions.LoadFromDirectory(directory);
        var manifest = new AssetManifest();
        var baseDir = Path.GetFullPath(directory);

        foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(baseDir, file).Replace('\\', '/');

            if (!options.ShouldInclude(relativePath))
            {
                continue;
            }

            var fileInfo = new FileInfo(file);
            manifest.Assets.Add(new AssetEntry
            {
                Path = relativePath,
                Size = fileInfo.Length,
                Hash = ComputeHash(file)
            });
        }

        return manifest;
    }

    public void SaveToDirectory(string directory)
    {
        var path = Path.Combine(directory, "manifest.json");
        File.WriteAllText(path, ToJson());
    }

    private static string ComputeHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static bool MatchesPattern(string name, string pattern)
    {
        if (pattern == "*")
        {
            return true;
        }

        if (pattern.StartsWith("*."))
        {
            var ext = pattern[1..];
            return name.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
        }

        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AssetEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";
}
