namespace OfflineAssets;

public sealed class AssetProject
{
    public AssetProject(string rootDirectory)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
        SourceDirectory = Path.Combine(RootDirectory, "Source");
        OutputDirectory = Path.Combine(RootDirectory, "Output");
        ModelsDirectory = Path.Combine(OutputDirectory, "Models");
        TexturesDirectory = Path.Combine(OutputDirectory, "Textures");
        AnimationsDirectory = Path.Combine(OutputDirectory, "Animations");
        SkeletonsDirectory = Path.Combine(OutputDirectory, "Skeletons");
        ShadersDirectory = Path.Combine(OutputDirectory, "Shaders");
    }

    private AssetProject(string rootDirectory, string sourceDirectory, string outputDirectory)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
        SourceDirectory = Path.GetFullPath(sourceDirectory);
        OutputDirectory = Path.GetFullPath(outputDirectory);
        ModelsDirectory = Path.Combine(OutputDirectory, "Models");
        TexturesDirectory = Path.Combine(OutputDirectory, "Textures");
        AnimationsDirectory = Path.Combine(OutputDirectory, "Animations");
        SkeletonsDirectory = Path.Combine(OutputDirectory, "Skeletons");
        ShadersDirectory = Path.Combine(OutputDirectory, "Shaders");
    }

    public string RootDirectory { get; }
    public string SourceDirectory { get; }
    public string OutputDirectory { get; }
    public string ModelsDirectory { get; }
    public string TexturesDirectory { get; }
    public string AnimationsDirectory { get; }
    public string SkeletonsDirectory { get; }
    public string ShadersDirectory { get; }

    public static AssetProject ForProjectAssets(string projectDirectory, string? sourceDirectory = null)
    {
        var fullProjectDir = Path.GetFullPath(projectDirectory);
        var assetsDir = Path.Combine(fullProjectDir, "Assets");
        sourceDirectory ??= Path.Combine(fullProjectDir, "AssetSources");

        return new AssetProject(fullProjectDir, sourceDirectory, assetsDir);
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(SourceDirectory);
        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(TexturesDirectory);
        Directory.CreateDirectory(AnimationsDirectory);
        Directory.CreateDirectory(SkeletonsDirectory);
        Directory.CreateDirectory(ShadersDirectory);
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
            var destPath = Path.Combine(ShadersDirectory, relativePath);
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
        var destDir = Path.Combine(ShadersDirectory, libraryName);

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
            SourcePath = Path.IsPathRooted(sourceFile) ? sourceFile : Path.Combine(SourceDirectory, sourceFile),
            OutputDirectory = ModelsDirectory,
            AssetName = name
        };
    }

    public TextureExportSettings CreateTextureExportSettings(string sourceFile, string? assetName = null)
    {
        var name = assetName ?? Path.GetFileNameWithoutExtension(sourceFile);
        return new TextureExportSettings
        {
            SourcePath = Path.IsPathRooted(sourceFile) ? sourceFile : Path.Combine(SourceDirectory, sourceFile),
            OutputDirectory = TexturesDirectory,
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
                    var destPath = Path.Combine(AnimationsDirectory, Path.GetFileName(animPath));
                    File.Copy(animPath, destPath, true);
                }
            }
        }

        if (separateSkeletons && !string.IsNullOrEmpty(result.SkeletonPath) && File.Exists(result.SkeletonPath))
        {
            var destPath = Path.Combine(SkeletonsDirectory, Path.GetFileName(result.SkeletonPath));
            File.Copy(result.SkeletonPath, destPath, true);
        }
    }

    public string GetRelativePath(string absolutePath)
    {
        return Path.GetRelativePath(RootDirectory, absolutePath);
    }

    public IEnumerable<string> EnumerateSourceFiles(string pattern = "*.*")
    {
        if (!Directory.Exists(SourceDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(SourceDirectory, pattern, SearchOption.AllDirectories))
        {
            yield return file;
        }
    }

    public IEnumerable<string> EnumerateOutputModels(string pattern = "*.glb")
    {
        if (!Directory.Exists(ModelsDirectory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(ModelsDirectory, pattern, SearchOption.AllDirectories))
        {
            yield return file;
        }
    }
}