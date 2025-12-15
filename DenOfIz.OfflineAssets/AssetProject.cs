namespace OfflineAssets;

public sealed class AssetProject
{
    public string RootDirectory { get; }
    public string SourceDirectory { get; }
    public string OutputDirectory { get; }
    public string ModelsDirectory { get; }
    public string TexturesDirectory { get; }
    public string AnimationsDirectory { get; }

    public AssetProject(string rootDirectory)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
        SourceDirectory = Path.Combine(RootDirectory, "Source");
        OutputDirectory = Path.Combine(RootDirectory, "Output");
        ModelsDirectory = Path.Combine(OutputDirectory, "Models");
        TexturesDirectory = Path.Combine(OutputDirectory, "Textures");
        AnimationsDirectory = Path.Combine(OutputDirectory, "Animations");
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(SourceDirectory);
        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(ModelsDirectory);
        Directory.CreateDirectory(TexturesDirectory);
        Directory.CreateDirectory(AnimationsDirectory);
    }

    public AssetExportSettings CreateExportSettings(string sourceFile, string? assetName = null)
    {
        var name = assetName ?? Path.GetFileNameWithoutExtension(sourceFile);
        return new AssetExportSettings
        {
            SourcePath = Path.IsPathRooted(sourceFile) ? sourceFile : Path.Combine(SourceDirectory, sourceFile),
            OutputDirectory = ModelsDirectory,
            AssetName = name
        };
    }

    public void CopyToOutput(AssetExportResult result, bool separateAnimations = true)
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
