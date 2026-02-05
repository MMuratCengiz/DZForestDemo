using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NiziKit.ContentPipeline;
using NiziKit.Core;
using NiziKit.Graphics;

namespace NiziKit.Assets.Pack;

public static class AssetPacks
{
    private static ILogger? _logger;
    private static ILogger Logger => _logger ??= Log.Get(typeof(AssetPacks));
    private static readonly ConcurrentDictionary<string, AssetPack> _packs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, (string packName, IAssetPackProvider provider)> _fileIndex = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, IAssetPackProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, AssetPack pack)
    {
        _packs[name] = pack;
    }

    public static void Unregister(string name)
    {
        _packs.TryRemove(name, out _);
    }

    public static AssetPack Get(string name)
    {
        if (!_packs.TryGetValue(name, out var pack))
        {
            throw new KeyNotFoundException($"Asset pack '{name}' not found");
        }
        return pack;
    }

    public static bool TryGet(string name, out AssetPack? pack)
        => _packs.TryGetValue(name, out pack);

    public static bool TryGetPack(string name, out AssetPack? pack)
        => _packs.TryGetValue(name, out pack);

    public static bool IsLoaded(string name) => _packs.ContainsKey(name);

    public static Texture2d GetTexture(string packName, string path)
        => Get(packName).GetTexture(path);

    public static Mesh GetMesh(string packName, string path)
        => Get(packName).GetMesh(path);

    public static Skeleton GetSkeleton(string packName, string path)
        => Get(packName).GetSkeleton(path);

    public static byte[] GetAnimationData(string packName, string path)
        => Get(packName).GetAnimationData(path);

    public static string? GetPackForPath(string path)
    {
        return Content.Manifest?.GetPackForPath(path);
    }

    public static Mesh? GetMeshByPath(string path)
    {
        var packName = GetPackForPath(path);
        if (packName == null)
        {
            return null;
        }

        EnsurePackLoaded(packName);
        if (TryGet(packName, out var pack) && pack != null)
        {
            return pack.Meshes.GetValueOrDefault(path);
        }
        return null;
    }

    public static Skeleton? GetSkeletonByPath(string path)
    {
        var packName = GetPackForPath(path);
        if (packName == null)
        {
            return null;
        }

        EnsurePackLoaded(packName);
        if (TryGet(packName, out var pack) && pack != null)
        {
            return pack.Skeletons.GetValueOrDefault(path);
        }
        return null;
    }

    public static byte[]? GetAnimationDataByPath(string path)
    {
        var packName = GetPackForPath(path);
        if (packName == null)
        {
            return null;
        }

        EnsurePackLoaded(packName);
        if (TryGet(packName, out var pack) && pack != null)
        {
            return pack.AnimationData.GetValueOrDefault(path);
        }
        return null;
    }

    public static Texture2d? GetTextureByPath(string path)
    {
        var packName = GetPackForPath(path);
        if (packName == null)
        {
            return null;
        }

        EnsurePackLoaded(packName);
        return TryGet(packName, out var pack) ? pack?.Textures.GetValueOrDefault(path) : null;
    }

    public static void EnsurePackLoaded(string packName)
    {
        if (IsLoaded(packName))
        {
            return;
        }

        var entry = GetPackEntry(packName);
        if (entry != null)
        {
            AssetPack.Load(entry.Path);
        }
    }

    public static async Task EnsurePackLoadedAsync(string packName, CancellationToken ct = default)
    {
        if (IsLoaded(packName))
        {
            return;
        }

        var entry = GetPackEntry(packName);
        if (entry != null)
        {
            await AssetPack.LoadAsync(entry.Path, ct);
        }
    }

    public static void LoadFromManifest()
    {
        if (Content.Manifest?.Packs != null && Content.Manifest.Packs.Count > 0)
        {
            var packsToLoad = Content.Manifest.Packs
                .Where(p => !IsLoaded(p.Name))
                .ToList();

            Parallel.ForEach(packsToLoad, p => AssetPack.Load(p.Path));
            return;
        }

        DiscoverAndLoadPacks();
    }

    public static async Task LoadFromManifestAsync(CancellationToken ct = default)
    {
        if (Content.Manifest?.Packs != null && Content.Manifest.Packs.Count > 0)
        {
            var packsToLoad = Content.Manifest.Packs
                .Where(p => !IsLoaded(p.Name))
                .ToList();

            var tasks = packsToLoad.Select(p => AssetPack.LoadAsync(p.Path, ct));
            await Task.WhenAll(tasks);
            return;
        }

        await DiscoverAndLoadPacksAsync(ct);
    }

    private static void DiscoverAndLoadPacks()
    {
        DiscoverAndIndexPacks();
    }

    private static async Task DiscoverAndLoadPacksAsync(CancellationToken ct)
    {
        await Task.Run(DiscoverAndIndexPacks, ct);
    }

    public static void DiscoverAndIndexPacks()
    {
        var assetsPath = Content.ResolvePath("");
        if (!Directory.Exists(assetsPath))
        {
            return;
        }

        var packFiles = Directory.GetFiles(assetsPath, "*.nizipack", SearchOption.TopDirectoryOnly);
        foreach (var packFile in packFiles)
        {
            var packName = Path.GetFileNameWithoutExtension(packFile);
            if (!_providers.ContainsKey(packName))
            {
                IndexPack(packFile, packName);
            }
        }
    }

    private static void IndexPack(string packFile, string packName)
    {
        var provider = CreatePackProvider(packFile);
        if (provider != null)
        {
            RegisterProvider(packName, provider);
        }
    }

    private static IAssetPackProvider? CreatePackProvider(string packPath)
    {
        try
        {
            return new BinaryAssetPackProvider(packPath);
        }
        catch
        {
            return null;
        }
    }

    public static PackEntry? GetPackEntry(string name)
    {
        return Content.Manifest?.Packs?.FirstOrDefault(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static void Clear()
    {
        foreach (var pack in _packs.Values)
        {
            pack.Dispose();
        }
        _packs.Clear();
    }

    public static void Reload(string name)
    {
        if (_packs.TryGetValue(name, out var existingPack))
        {
            var sourcePath = existingPack.SourcePath;
            existingPack.Dispose();
            AssetPack.Load(sourcePath);
        }
    }

    public static async Task ReloadAsync(string name, CancellationToken ct = default)
    {
        if (_packs.TryGetValue(name, out var existingPack))
        {
            var sourcePath = existingPack.SourcePath;
            existingPack.Dispose();
            await AssetPack.LoadAsync(sourcePath, ct);
        }
    }

    public static IEnumerable<string> GetLoadedPackNames() => _packs.Keys;

    public static IEnumerable<AssetPack> GetLoadedPacks() => _packs.Values;

    internal static void RegisterProvider(string packName, IAssetPackProvider provider)
    {
        RegisterProvider(packName, provider, provider.GetFilePaths());
    }

    internal static void RegisterProvider(string packName, IAssetPackProvider provider, IEnumerable<string> filePaths)
    {
        _providers[packName] = provider;
        foreach (var path in filePaths)
        {
            var normalized = path.Replace('\\', '/').TrimStart('/');
            _fileIndex.TryAdd(normalized, (packName, provider));
        }
    }

    internal static void UnregisterProvider(string packName)
    {
        _providers.TryRemove(packName, out _);
        var keysToRemove = _fileIndex
            .Where(kvp => kvp.Value.packName.Equals(packName, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in keysToRemove)
        {
            _fileIndex.TryRemove(key, out _);
        }
    }

    internal static bool TryGetProviderForPath(string path, out IAssetPackProvider? provider)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        if (_fileIndex.TryGetValue(normalized, out var entry))
        {
            provider = entry.provider;
            return true;
        }
        provider = null;
        return false;
    }

    internal static bool FileExistsInPacks(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return _fileIndex.ContainsKey(normalized);
    }

    internal static IEnumerable<string> GetIndexedProviderNames() => _providers.Keys;
}
