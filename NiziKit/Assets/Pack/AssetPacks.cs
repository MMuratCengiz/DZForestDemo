using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Assets.Pack;

public static class AssetPacks
{
    private static readonly Dictionary<string, AssetPack> _packs = new();

    public static void Register(string name, AssetPack pack)
    {
        _packs[name] = pack;
    }

    public static void Unregister(string name)
    {
        _packs.Remove(name);
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

    public static Material GetMaterial(string packName, string assetName)
        => Get(packName).GetMaterial(assetName);

    public static Model GetModel(string packName, string assetName)
        => Get(packName).GetModel(assetName);

    public static void LoadFromManifest()
    {
        if (Content.Manifest?.Packs == null)
        {
            return;
        }

        var packsToLoad = Content.Manifest.Packs
            .Select(path => (path, name: GetPackNameFromPath(path)))
            .Where(p => !IsLoaded(p.name))
            .ToList();

        Parallel.ForEach(packsToLoad, p => AssetPack.Load(p.path));
    }

    public static async Task LoadFromManifestAsync(CancellationToken ct = default)
    {
        if (Content.Manifest?.Packs == null)
        {
            return;
        }

        var packsToLoad = Content.Manifest.Packs
            .Select(path => (path, name: GetPackNameFromPath(path)))
            .Where(p => !IsLoaded(p.name))
            .ToList();

        var tasks = packsToLoad.Select(p => AssetPack.LoadAsync(p.path, ct));
        await Task.WhenAll(tasks);
    }

    private static string GetPackNameFromPath(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.EndsWith(".nizipack.json", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^".nizipack.json".Length];
        }
        if (fileName.EndsWith(".nizipack", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^".nizipack".Length];
        }
        return fileName;
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
}
