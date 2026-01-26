using System.Collections.Concurrent;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Assets.Pack;

public static class AssetPacks
{
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

    public static Texture2d GetTexture(string packName, string assetName)
        => Get(packName).GetTexture(assetName);

    public static GpuShader GetShader(string packName, string assetName)
        => Get(packName).GetShader(assetName);

    public static Model GetModel(string packName, string assetName)
        => Get(packName).GetModel(assetName);

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
        _providers[packName] = provider;
        foreach (var path in provider.GetFilePaths())
        {
            _fileIndex.TryAdd(path, (packName, provider));
        }
    }

    internal static void UnregisterProvider(string packName)
    {
        if (_providers.TryRemove(packName, out var provider))
        {
            foreach (var path in provider.GetFilePaths())
            {
                _fileIndex.TryRemove(path, out _);
            }
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
