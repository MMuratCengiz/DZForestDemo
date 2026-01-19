using NiziKit.Assets;
using NiziKit.Graphics;

namespace NiziKit.AssetPacks;

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

    public static bool IsLoaded(string name) => _packs.ContainsKey(name);

    public static Texture2d GetTexture(string packName, string assetName)
        => Get(packName).GetTexture(assetName);

    public static GpuShader GetShader(string packName, string assetName)
        => Get(packName).GetShader(assetName);

    public static Material GetMaterial(string packName, string assetName)
        => Get(packName).GetMaterial(assetName);

    public static Model GetModel(string packName, string assetName)
        => Get(packName).GetModel(assetName);

    public static void Clear()
    {
        foreach (var pack in _packs.Values)
        {
            pack.Dispose();
        }
        _packs.Clear();
    }
}
