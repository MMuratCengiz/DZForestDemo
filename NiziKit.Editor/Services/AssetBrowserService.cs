using System.Reflection;
using NiziKit.Assets;
using NiziKit.Assets.Pack;
using NiziKit.Components;

namespace NiziKit.Editor.Services;

public class AssetInfo
{
    public required string Name { get; init; }
    public required string Pack { get; init; }
    public string FullReference => $"{Pack}:{Name}";

    public override string ToString() => Name;
}

public class AssetBrowserService
{
    public IReadOnlyList<string> GetLoadedPacks()
    {
        var packs = new List<string>();

        var field = typeof(AssetPacks).GetField("_packs",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is Dictionary<string, AssetPack> packsDict)
        {
            packs.AddRange(packsDict.Keys);
        }

        return packs;
    }

    public IReadOnlyList<AssetInfo> GetAssetsOfType(AssetRefType assetType, string packName)
    {
        return assetType switch
        {
            AssetRefType.Mesh => GetMeshesFromPack(packName),
            AssetRefType.Texture => GetTexturesFromPack(packName),
            AssetRefType.Skeleton => GetSkeletonsFromPack(packName),
            AssetRefType.Animation => GetAnimationsFromPack(packName),
            _ => []
        };
    }

    public object? ResolveAsset(AssetRefType assetType, string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        var colonIndex = reference.IndexOf(':');
        if (colonIndex <= 0)
        {
            return null;
        }

        var packName = reference.Substring(0, colonIndex);
        var assetName = reference.Substring(colonIndex + 1);

        return assetType switch
        {
            AssetRefType.Mesh => ResolvePackMesh(packName, assetName),
            AssetRefType.Texture => AssetPacks.GetTexture(packName, assetName),
            AssetRefType.Skeleton => ResolvePackSkeleton(packName, assetName),
            AssetRefType.Animation => ResolvePackAnimation(packName, assetName),
            _ => null
        };
    }

    private Mesh? ResolvePackMesh(string packName, string assetName)
    {
        var (modelName, meshSelector) = ParseMeshSelector(assetName);
        var model = AssetPacks.GetModel(packName, modelName);
        return GetMeshFromModel(model, meshSelector);
    }

    private Skeleton? ResolvePackSkeleton(string packName, string modelName)
    {
        var model = AssetPacks.GetModel(packName, modelName);
        return model.Skeleton;
    }

    private NiziKit.Assets.Animation? ResolvePackAnimation(string packName, string assetName)
    {
        var (modelName, animSelector) = ParseMeshSelector(assetName);
        var model = AssetPacks.GetModel(packName, modelName);
        if (model.Skeleton == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(animSelector))
        {
            return model.Skeleton.GetAnimation(0);
        }

        if (uint.TryParse(animSelector, out var index))
        {
            return model.Skeleton.GetAnimation(index);
        }

        return model.Skeleton.GetAnimation(animSelector);
    }

    private static (string modelName, string? selector) ParseMeshSelector(string assetReference)
    {
        var slashIndex = assetReference.IndexOf('/');
        if (slashIndex > 0)
        {
            return (assetReference.Substring(0, slashIndex), assetReference.Substring(slashIndex + 1));
        }

        return (assetReference, null);
    }

    private static Mesh? GetMeshFromModel(Model model, string? meshSelector)
    {
        if (model.Meshes.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrEmpty(meshSelector))
        {
            return model.Meshes[0];
        }

        if (int.TryParse(meshSelector, out var index))
        {
            if (index < 0 || index >= model.Meshes.Count)
            {
                return null;
            }

            return model.Meshes[index];
        }

        return model.Meshes.FirstOrDefault(m => m.Name == meshSelector);
    }

    public IReadOnlyList<AssetInfo> GetMeshesFromPack(string packName)
    {
        var meshes = new List<AssetInfo>();
        if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
        {
            return meshes;
        }

        foreach (var modelKey in pack.Models.Keys)
        {
            var model = pack.Models[modelKey];
            if (model.Meshes.Count == 1)
            {
                meshes.Add(new AssetInfo { Name = modelKey, Pack = packName });
            }
            else
            {
                for (var i = 0; i < model.Meshes.Count; i++)
                {
                    var meshName = model.Meshes[i].Name;
                    var reference = string.IsNullOrEmpty(meshName) ? $"{modelKey}/{i}" : $"{modelKey}/{meshName}";
                    meshes.Add(new AssetInfo { Name = reference, Pack = packName });
                }
            }
        }

        return meshes;
    }

    public IReadOnlyList<AssetInfo> GetAllMeshes()
    {
        var meshes = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            meshes.AddRange(GetMeshesFromPack(packName));
        }

        return meshes;
    }

    public IReadOnlyList<AssetInfo> GetTexturesFromPack(string packName)
    {
        var textures = new List<AssetInfo>();
        if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
        {
            return textures;
        }

        foreach (var textureKey in pack.Textures.Keys)
        {
            textures.Add(new AssetInfo { Name = textureKey, Pack = packName });
        }

        return textures;
    }

    public IReadOnlyList<AssetInfo> GetSkeletonsFromPack(string packName)
    {
        var skeletons = new List<AssetInfo>();
        if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
        {
            return skeletons;
        }

        foreach (var modelKey in pack.Models.Keys)
        {
            var model = pack.Models[modelKey];
            if (model.Skeleton != null)
            {
                skeletons.Add(new AssetInfo { Name = modelKey, Pack = packName });
            }
        }

        return skeletons;
    }

    public IReadOnlyList<AssetInfo> GetAnimationsFromPack(string packName)
    {
        var animations = new List<AssetInfo>();

        if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
        {
            return animations;
        }

        foreach (var modelKey in pack.Models.Keys)
        {
            var model = pack.Models[modelKey];
            if (model.Skeleton != null)
            {
                var animNames = model.Skeleton.AnimationNames;
                for (var i = 0; i < animNames.Count; i++)
                {
                    var animName = string.IsNullOrEmpty(animNames[i])
                        ? $"{modelKey}/{i}"
                        : $"{modelKey}/{animNames[i]}";
                    animations.Add(new AssetInfo { Name = animName, Pack = packName });
                }
            }

            foreach (var animation in model.Animations)
            {
                animations.Add(new AssetInfo { Name = $"{modelKey}/{animation.Name}", Pack = packName });
            }
        }

        return animations;
    }
}
