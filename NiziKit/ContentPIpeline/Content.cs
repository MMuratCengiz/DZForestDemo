using System.Reflection;
using DenOfIz;
using NiziKit.Assets.Pack;

namespace NiziKit.ContentPipeline;

public static class Content
{
    private static readonly Lock Lock = new();
    private static IContentProvider? _provider;
    private static string _basePath = "Assets";

    public static bool IsInitialized => _provider != null;

    public static void Initialize(HttpClient httpClient, string basePath = "Assets/")
    {
        lock (Lock)
        {
            _basePath = basePath.TrimEnd('/');
            _provider = new HttpContentProvider(httpClient, basePath);
        }
    }

    public static void Initialize(string assetsPath)
    {
        lock (Lock)
        {
            var fullPath = Path.GetFullPath(assetsPath);
            _basePath = fullPath;
            _provider = new FileContentProvider(fullPath);

            var manifestPath = Path.Combine(fullPath, "manifest.json");
            if (File.Exists(manifestPath))
            {
                Manifest = AssetManifest.FromJson(File.ReadAllText(manifestPath));
            }
        }
    }

    public static ValueTask<Stream> OpenAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        return _provider!.OpenAsync(NormalizePath(path), ct);
    }

    public static ValueTask<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        return _provider!.ExistsAsync(NormalizePath(path), ct);
    }

    public static async ValueTask<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        var normalized = NormalizePath(path);
        if (TryReadBytesFromPacks(normalized, out var bytes))
        {
            return bytes!;
        }
        return await _provider!.ReadAllBytesAsync(normalized, ct);
    }

    public static async ValueTask<string> ReadTextAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        var normalized = NormalizePath(path);
        if (TryReadTextFromPacks(normalized, out var text))
        {
            return text!;
        }
        return await _provider!.ReadAllTextAsync(normalized, ct);
    }

    public static Stream Open(string path)
    {
        EnsureInitialized();
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.OpenAsync(NormalizePath(path)).GetAwaiter().GetResult();
        }
        throw new InvalidOperationException("Synchronous Open() is only available on desktop. Use OpenAsync() instead.");
    }

    public static byte[] ReadBytes(string path)
    {
        EnsureInitialized();
        var normalized = NormalizePath(path);
        if (TryReadBytesFromPacks(normalized, out var bytes))
        {
            return bytes!;
        }
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.ReadAllBytesAsync(normalized).GetAwaiter().GetResult();
        }
        throw new InvalidOperationException("Synchronous ReadBytes() is only available on desktop. Use ReadBytesAsync() instead.");
    }

    public static string ReadText(string path)
    {
        EnsureInitialized();
        var normalized = NormalizePath(path);
        if (TryReadTextFromPacks(normalized, out var text))
        {
            return text!;
        }
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.ReadAllTextAsync(normalized).GetAwaiter().GetResult();
        }
        throw new InvalidOperationException("Synchronous ReadText() is only available on desktop. Use ReadTextAsync() instead.");
    }

    public static bool Exists(string path)
    {
        EnsureInitialized();
        var normalized = NormalizePath(path);
        if (ExistsInPacks(normalized))
        {
            return true;
        }
        if (_provider is FileContentProvider)
        {
            return _provider.ExistsAsync(normalized).GetAwaiter().GetResult();
        }
        if (Manifest != null)
        {
            return Manifest.Contains(normalized);
        }
        throw new InvalidOperationException("Synchronous Exists() requires filesystem or loaded manifest.");
    }

    public static string ResolvePath(string relativePath)
    {
        EnsureInitialized();
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.GetFullPath(NormalizePath(relativePath));
        }
        return $"{_basePath}/{NormalizePath(relativePath)}";
    }

    public static async Task LoadManifestAsync(CancellationToken ct = default)
    {
        EnsureInitialized();
        var json = await _provider!.ReadAllTextAsync("manifest.json", ct);
        Manifest = AssetManifest.FromJson(json);
    }

    public static AssetManifest? Manifest { get; private set; }

    public static ShaderProgram LoadShaderProgram(
        string vertexPath,
        string pixelPath,
        Dictionary<string, string?>? defines = null)
        => Assets.Assets.LoadShaderProgram(vertexPath, pixelPath, defines);

    public static Task<ShaderProgram> LoadShaderProgramAsync(
        string vertexPath,
        string pixelPath,
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
        => Assets.Assets.LoadShaderProgramAsync(vertexPath, pixelPath, defines, ct);

    public static ShaderProgram LoadComputeProgram(
        string computePath,
        Dictionary<string, string?>? defines = null)
        => Assets.Assets.LoadComputeProgram(computePath, defines);

    public static Task<ShaderProgram> LoadComputeProgramAsync(
        string computePath,
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
        => Assets.Assets.LoadComputeProgramAsync(computePath, defines, ct);


    private static void EnsureInitialized()
    {
        if (_provider != null)
        {
            return;
        }

        lock (Lock)
        {
            if (_provider != null)
            {
                return;
            }

            if (OperatingSystem.IsBrowser())
            {
                throw new InvalidOperationException("On WASM, call Content.Initialize(httpClient) at startup.");
            }

            var assetsPath = FindAssetsDirectory();
            _provider = new FileContentProvider(assetsPath);
            _basePath = assetsPath;

            var manifestPath = Path.Combine(assetsPath, "manifest.json");
            if (File.Exists(manifestPath))
            {
                Manifest = AssetManifest.FromJson(File.ReadAllText(manifestPath));
            }

            AssetPacks.DiscoverAndIndexPacks();
        }
    }

    private static string FindAssetsDirectory()
    {
        var exePath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(exePath))
        {
            var exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDir))
            {
                var assetsDir = Path.Combine(exeDir, "Assets");
                if (Directory.Exists(assetsDir))
                {
                    return assetsDir;
                }
            }
        }

        var currentAssets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
        if (Directory.Exists(currentAssets))
        {
            return currentAssets;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "Assets");
    }

    private static string NormalizePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static bool TryReadTextFromPacks(string path, out string? text)
    {
        if (TryReadTextFromEmbeddedResources(path, out text))
        {
            return true;
        }
        if (AssetPacks.TryGetProviderForPath(path, out var provider) && provider != null)
        {
            text = provider.ReadText(path);
            return true;
        }
        text = null;
        return false;
    }

    private static bool TryReadBytesFromPacks(string path, out byte[]? bytes)
    {
        if (TryReadBytesFromEmbeddedResources(path, out bytes))
        {
            return true;
        }
        if (AssetPacks.TryGetProviderForPath(path, out var provider) && provider != null)
        {
            bytes = provider.ReadBytes(path);
            return true;
        }
        bytes = null;
        return false;
    }

    private static bool ExistsInPacks(string path)
    {
        if (ExistsInEmbeddedResources(path))
        {
            return true;
        }
        return AssetPacks.FileExistsInPacks(path);
    }

    private static bool TryReadTextFromEmbeddedResources(string path, out string? text)
    {
        var resourceName = PathToResourceName(path);
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                text = reader.ReadToEnd();
                return true;
            }
        }
        text = null;
        return false;
    }

    private static bool TryReadBytesFromEmbeddedResources(string path, out byte[]? bytes)
    {
        var resourceName = PathToResourceName(path);
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                bytes = new byte[stream.Length];
                stream.ReadExactly(bytes);
                return true;
            }
        }
        bytes = null;
        return false;
    }

    private static bool ExistsInEmbeddedResources(string path)
    {
        var resourceName = PathToResourceName(path);
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            var names = assembly.GetManifestResourceNames();
            return names.Contains(resourceName);
        }
        return false;
    }

    private static string PathToResourceName(string path)
    {
        var normalized = path.Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);
        var directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/').Replace('/', '.');
        if (string.IsNullOrEmpty(directory))
        {
            return fileName;
        }
        return $"{directory}.{fileName}";
    }
}
