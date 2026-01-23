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
    private readonly ConcurrentDictionary<string, Material> _materials = new();
    private readonly ConcurrentDictionary<string, Model> _models = new();
    private readonly ConcurrentDictionary<string, string> _modelPaths = new();

    private IAssetPackProvider? _provider;
    private string _basePath = string.Empty;
    private bool _disposed;

    public IReadOnlyDictionary<string, Texture2d> Textures => _textures;
    public IReadOnlyDictionary<string, Graphics.GpuShader> Shaders => _shaders;
    public IReadOnlyDictionary<string, Material> Materials => _materials;
    public IReadOnlyDictionary<string, Model> Models => _models;

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

    public Material GetMaterial(string key)
    {
        if (!_materials.TryGetValue(key, out var material))
        {
            throw new KeyNotFoundException($"Material '{key}' not found in asset pack '{Name}'");
        }
        return material;
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
    public bool TryGetMaterial(string key, out Material? material) => _materials.TryGetValue(key, out material);
    public bool TryGetModel(string key, out Model? model) => _models.TryGetValue(key, out model);

    public string? GetModelPath(string key) => _modelPaths.GetValueOrDefault(key);

    public IEnumerable<string> GetModelKeys() => _models.Keys;

    private void LoadInternal(string path)
    {
        SourcePath = path;
        (_provider, _basePath) = CreateProvider(path);
        var json = _provider.ReadText(ManifestFileName);
        var definition = AssetPackJson.FromJson(json);
        ApplyDefinition(definition);
        AssetPacks.Register(Name, this);
    }

    private async Task LoadInternalAsync(string path, CancellationToken ct)
    {
        SourcePath = path;
        (_provider, _basePath) = CreateProvider(path);
        var json = await _provider.ReadTextAsync(ManifestFileName, ct);
        var definition = AssetPackJson.FromJson(json);
        await ApplyDefinitionAsync(definition, ct);
        AssetPacks.Register(Name, this);
    }

    private static (IAssetPackProvider provider, string basePath) CreateProvider(string path)
    {
        var fullPath = Path.IsPathRooted(path)
            ? path
            : Content.ResolvePath(path);

        if (Directory.Exists(fullPath))
        {
            return (new FolderAssetPackProvider(fullPath), fullPath);
        }

        if (File.Exists(fullPath) && Path.GetExtension(fullPath).Equals(".nizipack", StringComparison.OrdinalIgnoreCase))
        {
            return (new ZipAssetPackProvider(fullPath), Path.GetDirectoryName(fullPath) ?? string.Empty);
        }

        var packFilePath = fullPath + ".nizipack";
        if (File.Exists(packFilePath))
        {
            return (new ZipAssetPackProvider(packFilePath), Path.GetDirectoryName(packFilePath) ?? string.Empty);
        }

        var manifestPath = Path.Combine(fullPath, ManifestFileName);
        if (File.Exists(manifestPath))
        {
            return (new FolderAssetPackProvider(fullPath), fullPath);
        }

        throw new FileNotFoundException($"Asset pack not found: {path}");
    }

    private void ApplyDefinition(AssetPackJson definition)
    {
        Name = definition.Name;
        Version = definition.Version;

        Parallel.Invoke(
            () => LoadTextures(definition.Textures),
            () => LoadShaders(definition.Shaders)
        );

        Parallel.Invoke(
            () => LoadMaterials(definition.Materials),
            () => LoadModels(definition.Models)
        );
    }

    private async Task ApplyDefinitionAsync(AssetPackJson definition, CancellationToken ct)
    {
        Name = definition.Name;
        Version = definition.Version;

        await Task.WhenAll(
            LoadTexturesAsync(definition.Textures, ct),
            LoadShadersAsync(definition.Shaders, ct)
        );

        await Task.WhenAll(
            LoadMaterialsAsync(definition.Materials, ct),
            LoadModelsAsync(definition.Models, ct)
        );
    }

    private void LoadTextures(Dictionary<string, string> textureDefs)
    {
        Parallel.ForEach(textureDefs, kvp =>
        {
            var texture = NiziKit.Assets.Assets.LoadTexture(kvp.Value);
            _textures[kvp.Key] = texture;
        });
    }

    private async Task LoadTexturesAsync(Dictionary<string, string> textureDefs, CancellationToken ct)
    {
        var tasks = textureDefs.Select(async kvp =>
        {
            var texture = await NiziKit.Assets.Assets.LoadTextureAsync(kvp.Value, ct);
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

    private void LoadMaterials(Dictionary<string, string> materialDefs)
    {
        Parallel.ForEach(materialDefs, kvp =>
        {
            var material = NiziKit.Assets.Assets.LoadMaterial(kvp.Value);
            _materials[kvp.Key] = material;
        });
    }

    private async Task LoadMaterialsAsync(Dictionary<string, string> materialDefs, CancellationToken ct)
    {
        var tasks = materialDefs.Select(async kvp =>
        {
            var material = await NiziKit.Assets.Assets.LoadMaterialAsync(kvp.Value, ct);
            return (kvp.Key, material);
        });

        foreach (var (key, material) in await Task.WhenAll(tasks))
        {
            _materials[key] = material;
        }
    }

    private void LoadModels(Dictionary<string, string> modelDefs)
    {
        Parallel.ForEach(modelDefs, kvp =>
        {
            var model = NiziKit.Assets.Assets.LoadModel(kvp.Value);
            _models[kvp.Key] = model;
            _modelPaths[kvp.Key] = kvp.Value;
        });
    }

    private async Task LoadModelsAsync(Dictionary<string, string> modelDefs, CancellationToken ct)
    {
        var tasks = modelDefs.Select(async kvp =>
        {
            var model = await NiziKit.Assets.Assets.LoadModelAsync(kvp.Value, ct);
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
        _materials.Clear();
        _models.Clear();

        _provider?.Dispose();
        AssetPacks.Unregister(Name);
    }
}
