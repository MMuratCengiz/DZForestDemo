using Microsoft.Extensions.Logging;
using NiziKit.Core;
using NiziKit.Offline;

Log.Initialize();
var logger = Log.Get("AssetImport");

var projectDir = Solution.Project("DZForestDemo");
var assetProject = AssetDirectories.ForProjectAssets(projectDir, projectDir);
assetProject.EnsureDirectories();

var syntySourceDir = Path.Combine(projectDir, "Assets_Source", "NiziKit_SyntyAssets");
var syntyOutputDir = Path.Combine(assetProject.Output, "Synty");
ImportSyntyAssets(syntySourceDir, syntyOutputDir);

return 0;

void LogInfo(string msg) => logger.LogInformation("{Message}", msg);

void ImportSyntyAssets(string sourceDirectory, string outputDirectory)
{
    if (!Directory.Exists(sourceDirectory))
    {
        logger.LogWarning("Synty assets not found at, synty assets are private and inaccessible unless added to the synty team: {SourceDirectory}", sourceDirectory);
        return;
    }

    using var importer = new BulkAssetImporter();
    var result = importer.Import(new BulkImportDesc
    {
        SourceDirectory = sourceDirectory,
        OutputDirectory = outputDirectory,
        ImportModels = true,
        ImportTextures = true,
        PreserveDirectoryStructure = true,
        ModelScale = 0.01f,
        GenerateMips = true,
        OnProgress = LogInfo
    });

    logger.LogInformation("Synty: {ModelsExported} models, {TexturesExported} textures exported", result.ModelsExported, result.TexturesExported);
    if (result.TotalFailed > 0)
    {
        logger.LogWarning("Synty: {TotalFailed} failed", result.TotalFailed);
        foreach (var error in result.Errors.Take(10))
        {
            logger.LogError("  {Error}", error);
        }
    }
}
