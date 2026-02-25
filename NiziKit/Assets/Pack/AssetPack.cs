using System.Collections.Concurrent;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;

namespace NiziKit.Assets.Pack;

public sealed class AssetPack : IDisposable
{

    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = "1.0.0";
    public string SourcePath { get; private set; } = string.Empty;

    private readonly ConcurrentDictionary<string, Texture2d> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Mesh> _meshes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Skeleton> _skeletons = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte[]> _animationData = new(StringComparer.OrdinalIgnoreCase);

    private IAssetPackProvider? _provider;
    private AssetPackJson _definition = new();
    private string _basePath = string.Empty;
    private bool _disposed;

    public IReadOnlyDictionary<string, Texture2d> Textures => _textures;
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
    public bool TryGetMesh(string path, out Mesh? mesh) => _meshes.TryGetValue(path, out mesh);
    public bool TryGetSkeleton(string path, out Skeleton? skeleton) => _skeletons.TryGetValue(path, out skeleton);
    public bool TryGetAnimationData(string path, out byte[]? data) => _animationData.TryGetValue(path, out data);

    public IEnumerable<string> GetMeshPaths() => _definition.Meshes;
    public IEnumerable<string> GetSkeletonPaths() => _definition.Skeletons;
    public IEnumerable<string> GetAnimationPaths() => _definition.Animations;
    public IEnumerable<string> GetTexturePaths() => _definition.Textures;

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

    /// <summary>
    /// Load a pack from a binary .nizipack file path.
    /// Assets are not loaded eagerly — they are loaded on-demand when requested.
    /// </summary>
    private void LoadInternal(string path)
    {
        SourcePath = path;
        var fullPath = Path.IsPathRooted(path) ? path : Content.ResolvePath(path);

        _provider = new BinaryAssetPackProvider(fullPath);
        _basePath = _provider.BasePath;

        var packName = Path.GetFileNameWithoutExtension(fullPath);
        _definition = AssetPackJson.FromFilePaths(packName, _provider.GetFilePaths());

        Name = _definition.Name;
        Version = _definition.Version;
        AssetPacks.Register(Name, this);

        var allPaths = GetAllFilePaths(_definition);
        AssetPacks.RegisterProvider(Name, _provider, allPaths);
    }

    private Task LoadInternalAsync(string path, CancellationToken ct)
    {
        LoadInternal(path);
        return Task.CompletedTask;
    }

    public static AssetPack LoadFromDirectory(string assetsRoot, string packName = "default")
    {
        var pack = new AssetPack();
        pack.LoadFromDirectoryInternal(assetsRoot, packName);
        return pack;
    }

    public static async Task<AssetPack> LoadFromDirectoryAsync(string assetsRoot, string packName = "default", CancellationToken ct = default)
    {
        var pack = new AssetPack();
        await pack.LoadFromDirectoryInternalAsync(assetsRoot, packName, ct);
        return pack;
    }

    private void LoadFromDirectoryInternal(string assetsRoot, string packName)
    {
        SourcePath = packName;
        _provider = new FolderAssetPackProvider(assetsRoot);
        _basePath = assetsRoot;

        var assetFiles = ScanAssetFiles(assetsRoot);
        _definition = AssetPackJson.FromFilePaths(packName, assetFiles);

        Name = _definition.Name;
        Version = _definition.Version;
        AssetPacks.Register(Name, this);

        var allPaths = GetAllFilePaths(_definition);
        AssetPacks.RegisterProvider(Name, _provider, allPaths);
    }

    private Task LoadFromDirectoryInternalAsync(string assetsRoot, string packName, CancellationToken ct)
    {
        LoadFromDirectoryInternal(assetsRoot, packName);
        return Task.CompletedTask;
    }

    private static List<string> ScanAssetFiles(string assetsRoot)
    {
        var files = new List<string>();
        foreach (var file in Directory.EnumerateFiles(assetsRoot, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (AssetPackJson.ExtensionToAssetType.ContainsKey(ext))
            {
                files.Add(Path.GetRelativePath(assetsRoot, file).Replace('\\', '/'));
            }
        }
        return files;
    }

    private static IEnumerable<string> GetAllFilePaths(AssetPackJson definition)
    {
        return definition.Textures
            .Concat(definition.Meshes)
            .Concat(definition.Skeletons)
            .Concat(definition.Animations);
    }

    public Texture2d? GetOrLoadTexture(string path)
    {
        if (_textures.TryGetValue(path, out var texture))
        {
            return texture;
        }

        if (_provider == null)
        {
            return null;
        }

        var bytes = _provider.ReadBytes(path);
        texture = new Texture2d();
        texture.LoadFromBytes(path, bytes);
        return _textures.GetOrAdd(path, texture);
    }

    public Mesh? GetOrLoadMesh(string path)
    {
        if (_meshes.TryGetValue(path, out var mesh))
        {
            return mesh;
        }

        if (_provider == null)
        {
            return null;
        }

        var bytes = _provider.ReadBytes(path);
        mesh = Mesh.Load(bytes);
        mesh.AssetPath = path;
        NiziAssets.Register(mesh, path);
        return _meshes.GetOrAdd(path, mesh);
    }

    public Skeleton? GetOrLoadSkeleton(string path)
    {
        if (_skeletons.TryGetValue(path, out var skeleton))
        {
            return skeleton;
        }

        if (_provider == null)
        {
            return null;
        }

        var bytes = _provider.ReadBytes(path);
        skeleton = Skeleton.Load(bytes);
        skeleton.AssetPath = path;
        return _skeletons.GetOrAdd(path, skeleton);
    }

    public byte[]? GetOrLoadAnimationData(string path)
    {
        if (_animationData.TryGetValue(path, out var data))
        {
            return data;
        }

        if (_provider == null)
        {
            return null;
        }

        data = _provider.ReadBytes(path);
        return _animationData.GetOrAdd(path, data);
    }

    private void LoadAssets(AssetPackJson definition)
    {
        Parallel.Invoke(
            () => LoadTextures(definition.Textures),
            () => LoadMeshes(definition.Meshes),
            () => LoadSkeletons(definition.Skeletons),
            () => LoadAnimations(definition.Animations)
        );
    }

    private async Task LoadAssetsAsync(AssetPackJson definition, CancellationToken ct)
    {
        await Task.WhenAll(
            LoadTexturesAsync(definition.Textures, ct),
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

    private void LoadMeshes(List<string> meshPaths)
    {
        Parallel.ForEach(meshPaths, path =>
        {
            var bytes = _provider!.ReadBytes(path);
            var mesh = Mesh.Load(bytes);
            mesh.AssetPath = path;
            NiziAssets.Register(mesh, path);
            _meshes[path] = mesh;
        });
    }

    private async Task LoadMeshesAsync(List<string> meshPaths, CancellationToken ct)
    {
        var tasks = meshPaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            var mesh = Mesh.Load(bytes);
            mesh.AssetPath = path;
            NiziAssets.Register(mesh, path);
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
            skeleton.AssetPath = path;
            _skeletons[path] = skeleton;
        });
    }

    private async Task LoadSkeletonsAsync(List<string> skeletonPaths, CancellationToken ct)
    {
        var tasks = skeletonPaths.Select(async path =>
        {
            var bytes = await _provider!.ReadBytesAsync(path, ct);
            var skeleton = Skeleton.Load(bytes);
            skeleton.AssetPath = path;
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
