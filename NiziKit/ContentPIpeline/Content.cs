using System.Reflection;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics;

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

    public static ValueTask<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        return _provider!.ReadAllBytesAsync(NormalizePath(path), ct);
    }

    public static ValueTask<string> ReadTextAsync(string path, CancellationToken ct = default)
    {
        EnsureInitialized();
        return _provider!.ReadAllTextAsync(NormalizePath(path), ct);
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
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.ReadAllBytesAsync(NormalizePath(path)).GetAwaiter().GetResult();
        }
        throw new InvalidOperationException("Synchronous ReadBytes() is only available on desktop. Use ReadBytesAsync() instead.");
    }

    public static string ReadText(string path)
    {
        EnsureInitialized();
        if (_provider is FileContentProvider fileProvider)
        {
            return fileProvider.ReadAllTextAsync(NormalizePath(path)).GetAwaiter().GetResult();
        }
        throw new InvalidOperationException("Synchronous ReadText() is only available on desktop. Use ReadTextAsync() instead.");
    }

    public static bool Exists(string path)
    {
        EnsureInitialized();
        if (_provider is FileContentProvider)
        {
            return _provider.ExistsAsync(NormalizePath(path)).GetAwaiter().GetResult();
        }
        if (Manifest != null)
        {
            return Manifest.Contains(NormalizePath(path));
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

    public static GpuShader LoadShader(
        string vertexPath,
        string pixelPath,
        GraphicsPipelineDesc pipelineDesc,
        Dictionary<string, string?>? defines = null)
        => Assets.Assets.LoadShader(vertexPath, pixelPath, pipelineDesc, defines);

    public static Task<GpuShader> LoadShaderAsync(
        string vertexPath,
        string pixelPath,
        GraphicsPipelineDesc pipelineDesc,
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
        => Assets.Assets.LoadShaderAsync(vertexPath, pixelPath, pipelineDesc, defines, ct);

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

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');
}
