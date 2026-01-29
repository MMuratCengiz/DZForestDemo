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

    private readonly ConcurrentDictionary<string, Texture2d> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Graphics.GpuShader> _shaders = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Mesh> _meshes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Skeleton> _skeletons = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte[]> _animationData = new(StringComparer.OrdinalIgnoreCase);

    private IAssetPackProvider? _provider;
    private string _basePath = string.Empty;
    private bool _disposed;

    public IReadOnlyDictionary<string, Texture2d> Textures => _textures;
    public IReadOnlyDictionary<string, Graphics.GpuShader> Shaders => _shaders;
    public IReadOnlyDictionary<string, Mesh> Meshes => _meshes;
    public IReadOnlyDictionary<string, Skeleton> Skeletons => _skeletons;
    public IReadOnlyDictionary<string, byte[]> AnimationData => _animationData;

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

    public Texture2d GetTexture(string path)
    {
        if (!_textures.TryGetValue(path, out var texture))
        {
            throw new KeyNotFoundException($"Texture '{path}' not found in asset pack '{Name}'");
        }
        return texture;
    }

    public Graphics.GpuShader GetShader(string path)
    {
        if (!_shaders.TryGetValue(path, out var shader))
        {
            throw new KeyNotFoundException($"Shader '{path}' not found in asset pack '{Name}'");
        }
        return shader;
    }

    public Mesh GetMesh(string path)
    {
        if (!_meshes.TryGetValue(path, out var mesh))
        {
            throw new KeyNotFoundException($"Mesh '{path}' not found in asset pack '{Name}'");
        }
        return mesh;
    }

    public Skeleton GetSkeleton(string path)
    {
        if (!_skeletons.TryGetValue(path, out var skeleton))
        {
            throw new KeyNotFoundException($"Skeleton '{path}' not found in asset pack '{Name}'");
        }
        return skeleton;
    }

    public byte[] GetAnimationData(string path)
    {
        if (!_animationData.TryGetValue(path, out var data))
        {
            throw new KeyNotFoundException($"Animation '{path}' not found in asset pack '{Name}'");
        }
        return data;
    }

    public bool TryGetTexture(string path, out Texture2d? texture) => _textures.TryGetValue(path, out texture);
    public bool TryGetShader(string path, out Graphics.GpuShader? shader) => _shaders.TryGetValue(path, out shader);
    public bool TryGetMesh(string path, out Mesh? mesh) => _meshes.TryGetValue(path, out mesh);
    public bool TryGetSkeleton(string path, out Skeleton? skeleton) => _skeletons.TryGetValue(path, out skeleton);
    public bool TryGetAnimationData(string path, out byte[]? data) => _animationData.TryGetValue(path, out data);

    public IEnumerable<string> GetMeshPaths() => _meshes.Keys;
    public IEnumerable<string> GetSkeletonPaths() => _skeletons.Keys;
    public IEnumerable<string> GetAnimationPaths() => _animationData.Keys;
    public IEnumerable<string> GetTexturePaths() => _textures.Keys;
    public IEnumerable<string> GetShaderPaths() => _shaders.Keys;

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

        var allPaths = GetAllFilePaths(definition);
        AssetPacks.RegisterProvider(Name, _provider, allPaths);

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

        var allPaths = GetAllFilePaths(definition);
        AssetPacks.RegisterProvider(Name, _provider, allPaths);

        await LoadAssetsAsync(definition, ct);
    }

    private static IEnumerable<string> GetAllFilePaths(AssetPackJson definition)
    {
        return definition.Textures
            .Concat(definition.Meshes)
            .Concat(definition.Skeletons)
            .Concat(definition.Animations)
            .Concat(definition.Shaders);
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
            () => LoadMeshes(definition.Meshes),
            () => LoadSkeletons(definition.Skeletons),
            () => LoadAnimations(definition.Animations)
        );
    }

    private async Task LoadAssetsAsync(AssetPackJson definition, CancellationToken ct)
    {
        await Task.WhenAll(
            LoadTexturesAsync(definition.Textures, ct),
            LoadShadersAsync(definition.Shaders, ct),
            LoadMeshesAsync(definition.Meshes, ct),
            LoadSkeletonsAsync(definition.Skeletons, ct),
            LoadAnimationsAsync(definition.Animations, ct)
        );
    }

    private void LoadTextures(List<string> texturePaths)
    {
        Parallel.ForEach(texturePaths, path =>
        {
            var bytes = _provider!.ReadBytes(path);
            var texture = new Texture2d();
            texture.LoadFromBytes(path, bytes);
            _textures[path] = texture;
        });
    }

    private async Task LoadTexturesAsync(List<string> texturePaths, CancellationToken ct)
    {
        var tasks = texturePaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            var texture = new Texture2d();
            texture.LoadFromBytes(path, bytes);
            return (path, texture);
        });

        foreach (var (path, texture) in await Task.WhenAll(tasks))
        {
            _textures[path] = texture;
        }
    }

    private void LoadShaders(List<string> shaderPaths)
    {
        Parallel.ForEach(shaderPaths, path =>
        {
            var shader = NiziKit.Assets.Assets.LoadShaderFromJson(path);
            _shaders[path] = shader;
        });
    }

    private async Task LoadShadersAsync(List<string> shaderPaths, CancellationToken ct)
    {
        var tasks = shaderPaths.Select(async path =>
        {
            var shader = await NiziKit.Assets.Assets.LoadShaderFromJsonAsync(path, null, ct);
            return (path, shader);
        });

        foreach (var (path, shader) in await Task.WhenAll(tasks))
        {
            _shaders[path] = shader;
        }
    }

    private void LoadMeshes(List<string> meshPaths)
    {
        Parallel.ForEach(meshPaths, path =>
        {
            var bytes = _provider!.ReadBytes(path);
            var mesh = Mesh.LoadFromNiziMesh(bytes);
            Assets.Register(mesh, path);
            _meshes[path] = mesh;
        });
    }

    private async Task LoadMeshesAsync(List<string> meshPaths, CancellationToken ct)
    {
        var tasks = meshPaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            var mesh = Mesh.LoadFromNiziMesh(bytes);
            Assets.Register(mesh, path);
            return (path, mesh);
        });

        foreach (var (path, mesh) in await Task.WhenAll(tasks))
        {
            _meshes[path] = mesh;
        }
    }

    private void LoadSkeletons(List<string> skeletonPaths)
    {
        Parallel.ForEach(skeletonPaths, path =>
        {
            var bytes = _provider!.ReadBytes(path);
            var skeleton = Skeleton.Load(bytes);
            _skeletons[path] = skeleton;
        });
    }

    private async Task LoadSkeletonsAsync(List<string> skeletonPaths, CancellationToken ct)
    {
        var tasks = skeletonPaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            var skeleton = Skeleton.Load(bytes);
            return (path, skeleton);
        });

        foreach (var (path, skeleton) in await Task.WhenAll(tasks))
        {
            _skeletons[path] = skeleton;
        }
    }

    private void LoadAnimations(List<string> animationPaths)
    {
        Parallel.ForEach(animationPaths, path =>
        {
            var bytes = _provider!.ReadBytes(path);
            _animationData[path] = bytes;
        });
    }

    private async Task LoadAnimationsAsync(List<string> animationPaths, CancellationToken ct)
    {
        var tasks = animationPaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            return (path, bytes);
        });

        foreach (var (path, bytes) in await Task.WhenAll(tasks))
        {
            _animationData[path] = bytes;
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
        _meshes.Clear();

        foreach (var skeleton in _skeletons.Values)
        {
            skeleton.Dispose();
        }
        _skeletons.Clear();
        _animationData.Clear();

        _provider?.Dispose();
        AssetPacks.UnregisterProvider(Name);
        AssetPacks.Unregister(Name);
    }
}
