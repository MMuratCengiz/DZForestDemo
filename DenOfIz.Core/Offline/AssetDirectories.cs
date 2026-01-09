namespace DenOfIz.World.Offline;

public sealed class AssetDirectories
{
    public AssetDirectories(string rootDirectory)
    {
        Root = Path.GetFullPath(rootDirectory);
        Source = Path.Combine(Root, "Source");
        Output = Path.Combine(Root, "Output");
        Models = Path.Combine(Output, "Models");
        Textures = Path.Combine(Output, "Textures");
        Animations = Path.Combine(Output, "Animations");
        Skeletons = Path.Combine(Output, "Skeletons");
        Shaders = Path.Combine(Output, "Shaders");
    }

    private AssetDirectories(string rootDirectory, string sourceDirectory, string outputDirectory)
    {
        Root = Path.GetFullPath(rootDirectory);
        Source = Path.GetFullPath(sourceDirectory);
        Output = Path.GetFullPath(outputDirectory);
        Models = Path.Combine(Output, "Models");
        Textures = Path.Combine(Output, "Textures");
        Animations = Path.Combine(Output, "Animations");
        Skeletons = Path.Combine(Output, "Skeletons");
        Shaders = Path.Combine(Output, "Shaders");
    }

    public string Root { get; }
    public string Source { get; }
    public string Output { get; }
    public string Models { get; }
    public string Textures { get; }
    public string Animations { get; }
    public string Skeletons { get; }
    public string Shaders { get; }

    public static AssetDirectories ForProjectAssets(string projectDirectory, string? sourceDirectory = null)
    {
        var fullProjectDir = Path.GetFullPath(projectDirectory);
        var assetsDir = Path.Combine(fullProjectDir, "Assets");
        sourceDirectory ??= Path.Combine(fullProjectDir, "AssetSources");

        return new AssetDirectories(fullProjectDir, sourceDirectory, assetsDir);
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Models);
        Directory.CreateDirectory(Textures);
        Directory.CreateDirectory(Animations);
        Directory.CreateDirectory(Skeletons);
        Directory.CreateDirectory(Shaders);
    }

    public void CopyShaders(string shaderSourceDirectory, bool recursive = true)
    {
        EnsureDirectories();

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var shaderExtensions = new[] { ".hlsl", ".glsl", ".frag", ".vert", ".comp", ".geom", ".tesc", ".tese" };

        foreach (var file in Directory.EnumerateFiles(shaderSourceDirectory, "*.*", searchOption))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (!shaderExtensions.Contains(extension))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(shaderSourceDirectory, file);
            var destPath = Path.Combine(Shaders, relativePath);
            var destDir = Path.GetDirectoryName(destPath);

            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(file, destPath, true);
        }
    }

    public void CopyShaderLibrary(string libraryDirectory, string? libraryName = null)
    {
        EnsureDirectories();

        libraryName ??= Path.GetFileName(libraryDirectory);
        var destDir = Path.Combine(Shaders, libraryName);

        CopyDirectoryRecursive(libraryDirectory, destDir);
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectoryRecursive(subDir, destSubDir);
        }
    }

    public AssetExportDesc CreateExportSettings(string sourceFile, string? assetName = null)
    {
        var name = assetName ?? Path.GetFileNameWithoutExtension(sourceFile);
        return new AssetExportDesc
        {
            SourcePath = Path.IsPathRooted(sourceFile) ? sourceFile : Path.Combine(Source, sourceFile),
            OutputDirectory = Models,
            AssetName = name
        };
    }

    public TextureExportSettings CreateTextureExportSettings(string sourceFile, string? assetName = null)
    {
        var name = assetName ?? Path.GetFileNameWithoutExtension(sourceFile);
        return new TextureExportSettings
        {
            SourcePath = Path.IsPathRooted(sourceFile) ? sourceFile : Path.Combine(Source, sourceFile),
            OutputDirectory = Textures,
            AssetName = name
        };
    }

    public void CopyToOutput(AssetExportResult result, bool separateAnimations = true, bool separateSkeletons = true)
    {
        if (!result.Success)
        {
            return;
        }

        if (separateAnimations && result.AnimationPaths.Count > 0)
        {
            foreach (var animPath in result.AnimationPaths)
            {
                if (File.Exists(animPath))
                {
                    var destPath = Path.Combine(Animations, Path.GetFileName(animPath));
                    File.Copy(animPath, destPath, true);
                }
            }
        }

        if (separateSkeletons && !string.IsNullOrEmpty(result.SkeletonPath) && File.Exists(result.SkeletonPath))
        {
            var destPath = Path.Combine(Skeletons, Path.GetFileName(result.SkeletonPath));
            File.Copy(result.SkeletonPath, destPath, true);
        }
    }

    public string GetRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(Root, absolutePath);
    }

    public IEnumerable<string> EnumerateSourceFiles(string pattern = "*.*")
    {
        if (!Directory.Exists(Source))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(Source, pattern, SearchOption.AllDirectories))
        {
            yield return file;
        }
    }

    public IEnumerable<string> EnumerateOutputModels(string pattern = "*.glb")
    {
        if (!Directory.Exists(Models))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(Models, pattern, SearchOption.AllDirectories))
        {
            yield return file;
        }
    }
}