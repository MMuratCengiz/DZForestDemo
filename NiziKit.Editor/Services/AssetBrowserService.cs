using NiziKit.Assets;
using NiziKit.Assets.Pack;
using NiziKit.Components;

namespace NiziKit.Editor.Services;

public class AssetInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public string? Pack { get; init; }
    public string FullReference => Path;

    public override string ToString() => Name;
}

public class AssetBrowserService
{
    public IReadOnlyList<string> GetLoadedPacks()
    {
        return AssetPacks.GetLoadedPackNames().ToList();
    }

    public IReadOnlyList<AssetInfo> GetAllAssetsOfType(AssetRefType assetType)
    {
        return assetType switch
        {
            AssetRefType.Mesh => GetAllMeshes(),
            AssetRefType.Texture => GetAllTextures(),
            AssetRefType.Skeleton => GetAllSkeletons(),
            AssetRefType.Animation => GetAllAnimations(),
            _ => []
        };
    }

    public object? ResolveAsset(AssetRefType assetType, string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return assetType switch
        {
            AssetRefType.Mesh => ResolveMesh(reference),
            AssetRefType.Texture => AssetPacks.GetTextureByPath(reference),
            AssetRefType.Skeleton => ResolveSkeleton(reference),
            AssetRefType.Animation => ResolveAnimationData(reference),
            _ => null
        };
    }

    private Mesh? ResolveMesh(string reference)
    {
        return AssetPacks.GetMeshByPath(reference);
    }

    private Skeleton? ResolveSkeleton(string reference)
    {
        return AssetPacks.GetSkeletonByPath(reference);
    }

    private byte[]? ResolveAnimationData(string reference)
    {
        return AssetPacks.GetAnimationDataByPath(reference);
    }

    public IReadOnlyList<AssetInfo> GetAllMeshes()
    {
        var meshes = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var meshPath in pack.GetMeshPaths())
            {
                var fileName = Path.GetFileName(meshPath);
                meshes.Add(new AssetInfo { Name = fileName, Path = meshPath, Pack = packName });
            }
        }
        return meshes;
    }

    public IReadOnlyList<AssetInfo> GetAllTextures()
    {
        var textures = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var texturePath in pack.GetTexturePaths())
            {
                var fileName = Path.GetFileName(texturePath);
                textures.Add(new AssetInfo { Name = fileName, Path = texturePath, Pack = packName });
            }
        }
        return textures;
    }

    public IReadOnlyList<AssetInfo> GetAllSkeletons()
    {
        var skeletons = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var skelPath in pack.GetSkeletonPaths())
            {
                var fileName = Path.GetFileName(skelPath);
                skeletons.Add(new AssetInfo { Name = fileName, Path = skelPath, Pack = packName });
            }
        }
        return skeletons;
    }

    public IReadOnlyList<AssetInfo> GetAllAnimations()
    {
        var animations = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var animPath in pack.GetAnimationPaths())
            {
                var fileName = Path.GetFileName(animPath);
                animations.Add(new AssetInfo { Name = fileName, Path = animPath, Pack = packName });
            }
        }
        return animations;
    }
}
