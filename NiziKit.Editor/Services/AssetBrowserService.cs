using System.Reflection;
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
        var packs = new List<string>();

        var field = typeof(AssetPacks).GetField("_packs",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is Dictionary<string, AssetPack> packsDict)
        {
            packs.AddRange(packsDict.Keys);
        }

        return packs;
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
            AssetRefType.Animation => ResolveAnimation(reference),
            AssetRefType.Shader => ResolveShader(reference),
            _ => null
        };
    }

    private Mesh? ResolveMesh(string reference)
    {
        var (filePath, meshSelector) = ParsePathWithSelector(reference);
        var model = AssetPacks.GetModelByPath(filePath);
        return model != null ? GetMeshFromModel(model, meshSelector) : null;
    }

    private Skeleton? ResolveSkeleton(string reference)
    {
        var (filePath, _) = ParsePathWithSelector(reference);
        var model = AssetPacks.GetModelByPath(filePath);
        return model?.Skeleton;
    }

    private NiziKit.Assets.Animation? ResolveAnimation(string reference)
    {
        var (filePath, animSelector) = ParsePathWithSelector(reference);
        var model = AssetPacks.GetModelByPath(filePath);
        if (model?.Skeleton == null)
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

    private static (string filePath, string? selector) ParsePathWithSelector(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return (reference, null);
        }

        var extensions = new[] { ".glb", ".gltf", ".fbx", ".obj", ".png", ".jpg", ".jpeg", ".tga", ".dds" };
        foreach (var ext in extensions)
        {
            var extIndex = reference.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
            if (extIndex > 0)
            {
                var afterExt = extIndex + ext.Length;
                if (afterExt < reference.Length && reference[afterExt] == '/')
                {
                    return (reference.Substring(0, afterExt), reference.Substring(afterExt + 1));
                }
                if (afterExt == reference.Length)
                {
                    return (reference, null);
                }
            }
        }

        return (reference, null);
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

    public IReadOnlyList<AssetInfo> GetAllMeshes()
    {
        var meshes = new List<AssetInfo>();
        foreach (var packName in GetLoadedPacks())
        {
            if (!AssetPacks.TryGet(packName, out var pack) || pack == null)
            {
                continue;
            }

            foreach (var modelPath in pack.GetModelPaths())
            {
                var model = pack.Models[modelPath];
                var fileName = System.IO.Path.GetFileName(modelPath);

                if (model.Meshes.Count == 1)
                {
                    meshes.Add(new AssetInfo { Name = fileName, Path = modelPath, Pack = packName });
                }
                else
                {
                    for (var i = 0; i < model.Meshes.Count; i++)
                    {
                        var meshName = model.Meshes[i].Name;
                        var selector = string.IsNullOrEmpty(meshName) ? i.ToString() : meshName;
                        var reference = $"{modelPath}/{selector}";
                        meshes.Add(new AssetInfo { Name = $"{fileName}/{selector}", Path = reference, Pack = packName });
                    }
                }
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

            foreach (var modelPath in pack.GetModelPaths())
            {
                var model = pack.Models[modelPath];
                if (model.Skeleton != null)
                {
                    var fileName = System.IO.Path.GetFileName(modelPath);
                    skeletons.Add(new AssetInfo { Name = fileName, Path = modelPath, Pack = packName });
                }
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

            foreach (var modelPath in pack.GetModelPaths())
            {
                var model = pack.Models[modelPath];
                var fileName = System.IO.Path.GetFileName(modelPath);

                if (model.Skeleton != null)
                {
                    var animNames = model.Skeleton.AnimationNames;
                    for (var i = 0; i < animNames.Count; i++)
                    {
                        var animName = string.IsNullOrEmpty(animNames[i]) ? i.ToString() : animNames[i];
                        var reference = $"{modelPath}/{animName}";
                        animations.Add(new AssetInfo { Name = $"{fileName}/{animName}", Path = reference, Pack = packName });
                    }
                }

                foreach (var animation in model.Animations)
                {
                    var reference = $"{modelPath}/{animation.Name}";
                    animations.Add(new AssetInfo { Name = $"{fileName}/{animation.Name}", Path = reference, Pack = packName });
                }
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
