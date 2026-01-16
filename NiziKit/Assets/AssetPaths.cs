using NiziKit.ContentPIpeline;

namespace NiziKit.Assets;

public static class AssetPaths
{
    public static string Shaders => Content.ResolvePath("Shaders");
    public static string Models => Content.ResolvePath("Models");
    public static string Meshes => Content.ResolvePath("Meshes");
    public static string Textures => Content.ResolvePath("Textures");
    public static string Animations => Content.ResolvePath("Animations");
    public static string Skeletons => Content.ResolvePath("Skeletons");

    public static string Resolve(string relativePath) => Content.ResolvePath(relativePath);

    public static string ResolveShader(string shaderPath)
    {
        if (Path.IsPathRooted(shaderPath))
        {
            return shaderPath;
        }
        return Content.ResolvePath($"Shaders/{shaderPath}");
    }

    public static string ResolveModel(string modelPath)
    {
        if (Path.IsPathRooted(modelPath))
        {
            return modelPath;
        }
        return Content.ResolvePath($"Models/{modelPath}");
    }

    public static string ResolveMesh(string meshPath)
    {
        if (Path.IsPathRooted(meshPath))
        {
            return meshPath;
        }
        return Content.ResolvePath($"Meshes/{meshPath}");
    }

    public static string ResolveTexture(string texturePath)
    {
        if (Path.IsPathRooted(texturePath))
        {
            return texturePath;
        }
        return Content.ResolvePath($"Textures/{texturePath}");
    }

    public static string ResolveAnimation(string animationPath)
    {
        if (Path.IsPathRooted(animationPath))
        {
            return animationPath;
        }
        return Content.ResolvePath($"Animations/{animationPath}");
    }

    public static string ResolveSkeleton(string skeletonPath)
    {
        if (Path.IsPathRooted(skeletonPath))
        {
            return skeletonPath;
        }
        return Content.ResolvePath($"Skeletons/{skeletonPath}");
    }

    public static bool Exists(string relativePath) => Content.Exists(relativePath);
}
