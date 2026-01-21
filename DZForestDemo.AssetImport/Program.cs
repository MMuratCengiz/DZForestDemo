using NiziKit.Offline;

var projectDir = Solution.Project("DZForestDemo");
var assetProject = AssetDirectories.ForProjectAssets(projectDir, projectDir);
assetProject.EnsureDirectories();

var syntySourceDir = Path.Combine(projectDir, "Assets_Source", "NiziKit_SyntyAssets");
var syntyOutputDir = Path.Combine(assetProject.Output, "Synty");
ImportSyntyAssets(syntySourceDir, syntyOutputDir);

return 0;

void Log(string msg) => Console.WriteLine(msg);

void ImportSyntyAssets(string sourceDirectory, string outputDirectory)
{
    if (!Directory.Exists(sourceDirectory))
    {
        Log($"Synty assets not found at, synty assets are private and inaccessible unless added to the synty team: {sourceDirectory}");
        return;
    }

    using var importer = new BulkAssetImporter();
    var result = importer.Import(new DirectoryImportSettings
    {
        SourceDirectory = sourceDirectory,
        OutputDirectory = outputDirectory,
        ImportModels = true,
        ImportTextures = true,
        PreserveDirectoryStructure = true,
        ModelScale = 0.01f,
        GenerateMips = true,
        OnProgress = Log
    });

    Log($"Synty: {result.ModelsExported} models, {result.TexturesExported} textures exported");
    if (result.TotalFailed > 0)
    {
        Log($"Synty: {result.TotalFailed} failed");
        foreach (var error in result.Errors.Take(10))
        {
            Console.WriteLine($"  {error}");
        }
    }
}
