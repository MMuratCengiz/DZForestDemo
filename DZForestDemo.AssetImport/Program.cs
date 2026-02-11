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

var skyboxSourceDir = Path.Combine(projectDir, "Assets_Source", "SkyBox");
var skyboxOutputDir = Path.Combine(assetProject.Output, "SkyBox");
ImportSkyboxTextures(skyboxSourceDir, skyboxOutputDir);

return 0;

void ImportSyntyAssets(string sourceDirectory, string outputDirectory)
{
    if (!Directory.Exists(sourceDirectory))
    {
        logger.LogWarning("Synty assets not found at, synty assets are private and inaccessible unless added to the synty team: {SourceDirectory}", sourceDirectory);
        return;
    }

    var baseLocoDir = Path.Combine(sourceDirectory, "BaseLocomotion");
    var baseLocoOutputDir = Path.Combine(outputDirectory, "Models", "BaseLocomotion");

    if (Directory.Exists(baseLocoDir))
    {
        ImportBaseLocomotion(baseLocoDir, baseLocoOutputDir);
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
        ExcludeDirectories = ["BaseLocomotion/Character", "BaseLocomotion/Animations", "Unreal_Characters"]
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

void ImportBaseLocomotion(string baseLocoDir, string outputDir)
{
    var characterDir = Path.Combine(baseLocoDir, "Character");
    var animationsDir = Path.Combine(baseLocoDir, "Animations");

    if (!Directory.Exists(characterDir))
    {
        return;
    }

    using var exporter = new AssetExporter();

    var characters = new[]
    {
        new { Name = "PolygonSyntyCharacter", AnimSubDir = "Polygon" },
        new { Name = "SidekickSyntyCharacter", AnimSubDir = "Sidekick" }
    };

    foreach (var character in characters)
    {
        var characterFile = Path.Combine(characterDir, $"{character.Name}.fbx");
        if (!File.Exists(characterFile))
        {
            logger.LogWarning("BaseLocomotion: {Name}.fbx not found, skipping", character.Name);
            continue;
        }

        var characterOutputDir = Path.Combine(outputDir, "Character");
        Directory.CreateDirectory(characterOutputDir);

        var characterExportDesc = new AssetExportDesc
        {
            SourcePath = characterFile,
            OutputDirectory = characterOutputDir,
            AssetName = character.Name,
            Format = ExportFormat.Glb,
            Scale = 0.01f,
            EmbedTextures = false,
            OverwriteExisting = true,
            OptimizeMeshes = true,
            GenerateNormals = true,
            CalculateTangents = true,
            TriangulateMeshes = true,
            JoinIdenticalVertices = true,
            SmoothNormals = true,
            SmoothNormalsAngle = 80.0f,
            ExportSkeleton = true,
            ExportAnimations = true
        };

        var charResult = exporter.Export(characterExportDesc);
        if (!charResult.Success)
        {
            logger.LogError("BaseLocomotion: {Name} export failed: {Error}", character.Name, charResult.ErrorMessage);
            continue;
        }

        var charAnimDir = Path.Combine(animationsDir, character.AnimSubDir);
        if (!Directory.Exists(charAnimDir) || charResult.OzzSkeleton == null)
        {
            continue;
        }

        foreach (var animFile in Directory.EnumerateFiles(charAnimDir, "*.fbx", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(animationsDir, animFile);
            var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
            var animName = Path.GetFileNameWithoutExtension(animFile);
            var animOutputDir = Path.Combine(outputDir, "Animations", relativeDir);
            Directory.CreateDirectory(animOutputDir);

            var animExportDesc = new AssetExportDesc
            {
                SourcePath = animFile,
                OutputDirectory = animOutputDir,
                AssetName = animName,
                Format = ExportFormat.Glb,
                Scale = 0.01f,
                EmbedTextures = false,
                OverwriteExisting = true,
                OptimizeMeshes = true,
                GenerateNormals = true,
                CalculateTangents = true,
                TriangulateMeshes = true,
                JoinIdenticalVertices = true,
                SmoothNormals = true,
                SmoothNormalsAngle = 80.0f,
                ExportSkeleton = true,
                ExportAnimations = true,
                ExternalSkeleton = charResult.OzzSkeleton
            };

            var animResult = exporter.Export(animExportDesc);
            if (!animResult.Success)
            {
                logger.LogError("  Animation failed: {Name}: {Error}", animName, animResult.ErrorMessage);
            }
        }
    }
}

void ImportSkyboxTextures(string sourceDirectory, string outputDirectory)
{
    if (!Directory.Exists(sourceDirectory))
    {
        logger.LogWarning("Skybox assets not found at: {SourceDirectory}", sourceDirectory);
        return;
    }

    using var importer = new BulkAssetImporter();
    var result = importer.Import(new BulkImportDesc
    {
        SourceDirectory = sourceDirectory,
        OutputDirectory = outputDirectory,
        ImportModels = false,
        ImportTextures = true,
        PreserveDirectoryStructure = true,
        GenerateMips = false
    });

    logger.LogInformation("Skybox: {TexturesExported} textures exported", result.TexturesExported);
    if (result.TotalFailed > 0)
    {
        logger.LogWarning("Skybox: {TotalFailed} failed", result.TotalFailed);
        foreach (var error in result.Errors.Take(10))
        {
            logger.LogError("  {Error}", error);
        }
    }
}
