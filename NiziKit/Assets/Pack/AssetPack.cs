using System.Collections.Concurrent;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;

namespace NiziKit.Assets.Pack;

public sealed class AssetPack : IDisposable
{
    private const string ManifestFileName = "pack.nizipack.json";

    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = "1.0.0";
    public string SourcePath { get; private set; } = string.Empty;

    private readonly ConcurrentDictionary<string, Texture2d> _textures = new();
    private readonly ConcurrentDictionary<string, Graphics.GpuShader> _shaders = new();
    private readonly ConcurrentDictionary<string, Model> _models = new();
    private readonly ConcurrentDictionary<string, string> _modelPaths = new();

    private IAssetPackProvider? _provider;
    private string _basePath = string.Empty;
    private bool _disposed;

    public IReadOnlyDictionary<string, Texture2d> Textures => _textures;
    public IReadOnlyDictionary<string, Graphics.GpuShader> Shaders => _shaders;
    public IReadOnlyDictionary<string, Model> Models => _models;

    internal IAssetPackProvider? Provider => _provider;

    public static AssetPack Load(string path)
    {
        var pack = new AssetPack();
        pack.LoadInternal(path);
        return pack;
    }

    public static async Task<AssetPack> LoadAsync(string path, CancellationToken ct = default)
    {
        var pack = new AssetPack();
        await pack.LoadInternalAsync(path, ct);
        return pack;
    }

    public Texture2d GetTexture(string key)
    {
        if (!_textures.TryGetValue(key, out var texture))
        {
            throw new KeyNotFoundException($"Texture '{key}' not found in asset pack '{Name}'");
        }
        return texture;
    }

    public Graphics.GpuShader GetShader(string key)
    {
        if (!_shaders.TryGetValue(key, out var shader))
        {
            throw new KeyNotFoundException($"Shader '{key}' not found in asset pack '{Name}'");
        }
        return shader;
    }

    public Model GetModel(string key)
    {
        if (!_models.TryGetValue(key, out var model))
        {
            throw new KeyNotFoundException($"Model '{key}' not found in asset pack '{Name}'");
        }
        return model;
    }

    public bool TryGetTexture(string key, out Texture2d? texture) => _textures.TryGetValue(key, out texture);
    public bool TryGetShader(string key, out Graphics.GpuShader? shader) => _shaders.TryGetValue(key, out shader);
    public bool TryGetModel(string key, out Model? model) => _models.TryGetValue(key, out model);

    public string? GetModelPath(string key) => _modelPaths.GetValueOrDefault(key);

    public IEnumerable<string> GetModelKeys() => _models.Keys;

    public Texture2d LoadTextureFromPack(string path)
    {
        if (_provider == null)
        {
            throw new InvalidOperationException("Pack provider not initialized");
        }

        var bytes = _provider.ReadBytes(path);
        var texture = new Texture2d();
        texture.LoadFromBytes(path, bytes);
        return texture;
    }

    public async Task<Texture2d> LoadTextureFromPackAsync(string path, CancellationToken ct = default)
    {
        if (_provider == null)
        {
            throw new InvalidOperationException("Pack provider not initialized");
        }

        var bytes = await _provider.ReadBytesAsync(path, ct);
        var texture = new Texture2d();
        texture.LoadFromBytes(path, bytes);
        return texture;
    }

    private void LoadInternal(string path)
    {
        SourcePath = path;
        var (provider, manifestPath) = CreateProvider(path);
        _provider = provider;
        _basePath = provider.BasePath;
        var json = provider.ReadText(manifestPath);
        var definition = AssetPackJson.FromJson(json);

        Name = definition.Name;
        Version = definition.Version;
        AssetPacks.Register(Name, this);

        LoadAssets(definition);
    }

    private async Task LoadInternalAsync(string path, CancellationToken ct)
    {
        SourcePath = path;
        var (provider, manifestPath) = CreateProvider(path);
        _provider = provider;
        _basePath = provider.BasePath;
        var json = await provider.ReadTextAsync(manifestPath, ct);
        var definition = AssetPackJson.FromJson(json);

        Name = definition.Name;
        Version = definition.Version;
        AssetPacks.Register(Name, this);

        await LoadAssetsAsync(definition, ct);
    }

    private static (IAssetPackProvider provider, string manifestPath) CreateProvider(string path)
    {
        var assetsRoot = Content.ResolvePath("");
        var fullPath = Path.IsPathRooted(path) ? path : Content.ResolvePath(path);

        if (fullPath.EndsWith(".nizipack", StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            return (CreatePackProvider(fullPath), ManifestFileName);
        }

        if (fullPath.EndsWith(".nizipack.json", StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            var relativeManifestPath = Path.GetRelativePath(assetsRoot, fullPath).Replace('\\', '/');
            return (new FolderAssetPackProvider(assetsRoot), relativeManifestPath);
        }

        var packFilePath = fullPath + ".nizipack";
        if (File.Exists(packFilePath))
        {
            return (CreatePackProvider(packFilePath), ManifestFileName);
        }

        var jsonPackFilePath = fullPath + ".nizipack.json";
        if (File.Exists(jsonPackFilePath))
        {
            var relativeManifestPath = Path.GetRelativePath(assetsRoot, jsonPackFilePath).Replace('\\', '/');
            return (new FolderAssetPackProvider(assetsRoot), relativeManifestPath);
        }

        throw new FileNotFoundException($"Asset pack not found: {path}");
    }

    private static IAssetPackProvider CreatePackProvider(string packPath)
    {
        return new BinaryAssetPackProvider(packPath);
    }

    private void LoadAssets(AssetPackJson definition)
    {
        Parallel.Invoke(
            () => LoadTextures(definition.Textures),
            () => LoadShaders(definition.Shaders),
            () => LoadModels(definition.Models)
        );
    }

    private async Task LoadAssetsAsync(AssetPackJson definition, CancellationToken ct)
    {
        await Task.WhenAll(
            LoadTexturesAsync(definition.Textures, ct),
            LoadShadersAsync(definition.Shaders, ct),
            LoadModelsAsync(definition.Models, ct)
        );
    }

    private void LoadTextures(Dictionary<string, string> textureDefs)
    {
        Parallel.ForEach(textureDefs, kvp =>
        {
            var bytes = _provider!.ReadBytes(kvp.Value);
            var texture = new Texture2d();
            texture.LoadFromBytes(kvp.Value, bytes);
            _textures[kvp.Key] = texture;
        });
    }

    private async Task LoadTexturesAsync(Dictionary<string, string> textureDefs, CancellationToken ct)
    {
        var tasks = textureDefs.Select(async kvp =>
        {
            var bytes = await _provider!.ReadBytesAsync(kvp.Value, ct);
            var texture = new Texture2d();
            texture.LoadFromBytes(kvp.Value, bytes);
            return (kvp.Key, texture);
        });

        foreach (var (key, texture) in await Task.WhenAll(tasks))
        {
            _textures[key] = texture;
        }
    }

    private void LoadShaders(Dictionary<string, string> shaderDefs)
    {
        Parallel.ForEach(shaderDefs, kvp =>
        {
            var shader = NiziKit.Assets.Assets.LoadShaderFromJson(kvp.Value);
            _shaders[kvp.Key] = shader;
        });
    }

    private async Task LoadShadersAsync(Dictionary<string, string> shaderDefs, CancellationToken ct)
    {
        var tasks = shaderDefs.Select(async kvp =>
        {
            var shader = await NiziKit.Assets.Assets.LoadShaderFromJsonAsync(kvp.Value, null, ct);
            return (kvp.Key, shader);
        });

        foreach (var (key, shader) in await Task.WhenAll(tasks))
        {
            _shaders[key] = shader;
        }
    }

    private void LoadModels(Dictionary<string, string> modelDefs)
    {
        Parallel.ForEach(modelDefs, kvp =>
        {
            var bytes = _provider!.ReadBytes(kvp.Value);
            var model = new Model();
            model.LoadFromBytes(bytes, kvp.Value);
            foreach (var mesh in model.Meshes)
            {
                Assets.Register(mesh, $"{Name}:{kvp.Key}:{mesh.Name}");
            }
            _models[kvp.Key] = model;
            _modelPaths[kvp.Key] = kvp.Value;
        });
    }

    private async Task LoadModelsAsync(Dictionary<string, string> modelDefs, CancellationToken ct)
    {
        var tasks = modelDefs.Select(async kvp =>
        {
            var bytes = await _provider!.ReadBytesAsync(kvp.Value, ct);
            var model = new Model();
            model.LoadFromBytes(bytes, kvp.Value);
            foreach (var mesh in model.Meshes)
            {
                Assets.Register(mesh, $"{Name}:{kvp.Key}:{mesh.Name}");
            }
            return (kvp.Key, kvp.Value, model);
        });

        foreach (var (key, path, model) in await Task.WhenAll(tasks))
        {
            _models[key] = model;
            _modelPaths[key] = path;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _textures.Clear();
        _shaders.Clear();
        _models.Clear();

        _provider?.Dispose();
        AssetPacks.Unregister(Name);
    }
}
