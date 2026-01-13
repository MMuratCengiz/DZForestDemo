using System.Reflection;

namespace NiziKit.Assets;

public static class AssetPaths
{
    private static string? _contentRoot;
    private static readonly Lock Lock = new();

    public static string ContentRoot
    {
        get
        {
            if (_contentRoot != null)
            {
                return _contentRoot;
            }

            lock (Lock)
            {
                _contentRoot ??= ResolveDefaultContentRoot();
                return _contentRoot;
            }
        }
        set
        {
            lock (Lock)
            {
                _contentRoot = Path.GetFullPath(value);
            }
        }
    }

    public static string Shaders => Path.Combine(ContentRoot, "Shaders");
    public static string Models => Path.Combine(ContentRoot, "Models");
    public static string Meshes => Path.Combine(ContentRoot, "Meshes");
    public static string Textures => Path.Combine(ContentRoot, "Textures");
    public static string Animations => Path.Combine(ContentRoot, "Animations");
    public static string Skeletons => Path.Combine(ContentRoot, "Skeletons");

    public static string Resolve(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(ContentRoot, relativePath));
    }

    public static string ResolveShader(string shaderPath)
    {
        if (Path.IsPathRooted(shaderPath))
        {
            return shaderPath;
        }

        return Path.GetFullPath(Path.Combine(Shaders, shaderPath));
    }

    public static string ResolveModel(string modelPath)
    {
        if (Path.IsPathRooted(modelPath))
        {
            return modelPath;
        }

        return Path.GetFullPath(Path.Combine(Models, modelPath));
    }

    public static string ResolveMesh(string meshPath)
    {
        if (Path.IsPathRooted(meshPath))
        {
            return meshPath;
        }

        return Path.GetFullPath(Path.Combine(Meshes, meshPath));
    }

    public static string ResolveTexture(string texturePath)
    {
        if (Path.IsPathRooted(texturePath))
        {
            return texturePath;
        }

        return Path.GetFullPath(Path.Combine(Textures, texturePath));
    }

    public static string ResolveAnimation(string animationPath)
    {
        if (Path.IsPathRooted(animationPath))
        {
            return animationPath;
        }

        return Path.GetFullPath(Path.Combine(Animations, animationPath));
    }

    public static string ResolveSkeleton(string skeletonPath)
    {
        if (Path.IsPathRooted(skeletonPath))
        {
            return skeletonPath;
        }

        return Path.GetFullPath(Path.Combine(Skeletons, skeletonPath));
    }

    public static bool Exists(string relativePath)
    {
        return File.Exists(Resolve(relativePath));
    }

    public static IEnumerable<string> EnumerateFiles(string subDirectory, string pattern = "*.*",
        bool recursive = false)
    {
        var directory = Path.Combine(ContentRoot, subDirectory);
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(directory, pattern, searchOption))
        {
            yield return file;
        }
    }

    private static string ResolveDefaultContentRoot()
    {
        var exePath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(exePath))
        {
            var exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDir))
            {
                var assetsDir = Path.Combine(exeDir, "Assets");
                if (Directory.Exists(assetsDir))
                {
                    return assetsDir;
                }
            }
        }

        var currentAssets = Path.Combine(Environment.CurrentDirectory, "Assets");
        if (Directory.Exists(currentAssets))
        {
            return currentAssets;
        }

        if (!string.IsNullOrEmpty(exePath))
        {
            var exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(exeDir))
            {
                return Path.Combine(exeDir, "Assets");
            }
        }

        return Path.Combine(Environment.CurrentDirectory, "Assets");
    }
}