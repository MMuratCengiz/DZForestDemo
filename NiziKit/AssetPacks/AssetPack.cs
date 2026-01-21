using NiziKit.Assets;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;

namespace NiziKit.AssetPacks;

public sealed class AssetPack : IDisposable
{
    private const string ManifestFileName = "pack.nizipack.json";

    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = "1.0.0";
    public string SourcePath { get; private set; } = string.Empty;

    private readonly Dictionary<string, Texture2d> _textures = new();
    private readonly Dictionary<string, Graphics.GpuShader> _shaders = new();
    private readonly Dictionary<string, Material> _materials = new();
    private readonly Dictionary<string, Model> _models = new();

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

        LoadTextures(definition.Textures);
        LoadShaders(definition.Shaders);
        LoadMaterials(definition.Materials);
        LoadModels(definition.Models);
    }

    private async Task ApplyDefinitionAsync(AssetPackJson definition, CancellationToken ct)
    {
        Name = definition.Name;
        Version = definition.Version;

        await Task.WhenAll(
            LoadTexturesAsync(definition.Textures, ct),
            LoadShadersAsync(definition.Shaders, ct),
            LoadMaterialsAsync(definition.Materials, ct),
            LoadModelsAsync(definition.Models, ct)
        );
    }

    private void LoadTextures(Dictionary<string, string> textureDefs)
    {
        foreach (var (key, path) in textureDefs)
        {
            var texture = Assets.Assets.LoadTexture(path);
            _textures[key] = texture;
        }
    }

    private async Task LoadTexturesAsync(Dictionary<string, string> textureDefs, CancellationToken ct)
    {
        var tasks = textureDefs.Select(async kvp =>
        {
            var texture = await Assets.Assets.LoadTextureAsync(kvp.Value, ct);
            return (kvp.Key, texture);
        });

        foreach (var (key, texture) in await Task.WhenAll(tasks))
        {
            _textures[key] = texture;
        }
    }

    private void LoadShaders(Dictionary<string, string> shaderDefs)
    {
        foreach (var (key, path) in shaderDefs)
        {
            var shader = Assets.Assets.LoadShaderFromJson(path);
            _shaders[key] = shader;
        }
    }

    private async Task LoadShadersAsync(Dictionary<string, string> shaderDefs, CancellationToken ct)
    {
        var tasks = shaderDefs.Select(async kvp =>
        {
            var shader = await Assets.Assets.LoadShaderFromJsonAsync(kvp.Value, ct);
            return (kvp.Key, shader);
        });

        foreach (var (key, shader) in await Task.WhenAll(tasks))
        {
            _shaders[key] = shader;
        }
    }

    private void LoadMaterials(Dictionary<string, string> materialDefs)
    {
        foreach (var (key, path) in materialDefs)
        {
            var material = Assets.Assets.LoadMaterial(path);
            _materials[key] = material;
        }
    }

    private async Task LoadMaterialsAsync(Dictionary<string, string> materialDefs, CancellationToken ct)
    {
        var tasks = materialDefs.Select(async kvp =>
        {
            var material = await Assets.Assets.LoadMaterialAsync(kvp.Value, ct);
            return (kvp.Key, material);
        });

        foreach (var (key, material) in await Task.WhenAll(tasks))
        {
            _materials[key] = material;
        }
    }

    private void LoadModels(Dictionary<string, string> modelDefs)
    {
        foreach (var (key, path) in modelDefs)
        {
            var model = Assets.Assets.LoadModel(path);
            _models[key] = model;
        }
    }

    private async Task LoadModelsAsync(Dictionary<string, string> modelDefs, CancellationToken ct)
    {
        var tasks = modelDefs.Select(async kvp =>
        {
            var model = await Assets.Assets.LoadModelAsync(kvp.Value, ct);
            return (kvp.Key, model);
        });

        foreach (var (key, model) in await Task.WhenAll(tasks))
        {
            _models[key] = model;
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
