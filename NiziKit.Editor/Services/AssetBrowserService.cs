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
            AssetRefType.Shader => GetAllShaders(),
            AssetRefType.Material => GetAllMaterials(),
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
            AssetRefType.Shader => ResolveShader(reference),
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

    private NiziKit.Graphics.GpuShader? ResolveShader(string reference)
    {
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            if (pack.TryGetShader(reference, out var shader))
            {
                return shader;
            }
        }
        return null;
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
                var fileName = System.IO.Path.GetFileName(meshPath);
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
                var fileName = System.IO.Path.GetFileName(texturePath);
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
                var fileName = System.IO.Path.GetFileName(skelPath);
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
                var fileName = System.IO.Path.GetFileName(animPath);
                animations.Add(new AssetInfo { Name = fileName, Path = animPath, Pack = packName });
            }
        }
        return animations;
    }

    public IReadOnlyList<AssetInfo> GetAllShaders()
    {
        var shaders = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var shaderPath in pack.GetShaderPaths())
            {
                var fileName = System.IO.Path.GetFileName(shaderPath);
                shaders.Add(new AssetInfo { Name = fileName, Path = shaderPath, Pack = packName });
            }
        }
        return shaders;
    }

    public IReadOnlyList<AssetInfo> GetAllMaterials()
    {
        var materials = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            var packDir = System.IO.Path.GetDirectoryName(pack.Name) ?? "";
            var materialsDir = System.IO.Path.Combine(packDir, "Materials");
            if (System.IO.Directory.Exists(materialsDir))
            {
                foreach (var materialFile in System.IO.Directory.GetFiles(materialsDir, "*.nizimat.json"))
                {
                    var fileName = System.IO.Path.GetFileName(materialFile);
                    materials.Add(new AssetInfo { Name = fileName, Path = materialFile, Pack = packName });
                }
            }
        }
        return materials;
    }
}
