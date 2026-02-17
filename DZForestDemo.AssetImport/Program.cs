using DenOfIz;
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
        ImportBaseLocomotion(baseLocoDir, sourceDirectory, baseLocoOutputDir);
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
        RepairSkeletonTransforms = true,
        ExcludeDirectories = ["BaseLocomotion/Character", "BaseLocomotion/Animations", "BowCombat/Animations", "MeleeCombat/Animations", "Idles/Animations", "Unreal_Characters"]
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

OzzSkeleton? ImportBaseLocomotion(string baseLocoDir, string syntySourceDir, string outputDir)
{
    var characterDir = Path.Combine(baseLocoDir, "Character");
    var animationsDir = Path.Combine(baseLocoDir, "Animations");

    if (!Directory.Exists(characterDir))
    {
        return null;
    }

    using var exporter = new AssetExporter();

    // Use Polygon character only for consistency
    var characterFile = Path.Combine(characterDir, "PolygonSyntyCharacter.fbx");
    if (!File.Exists(characterFile))
    {
        logger.LogWarning("BaseLocomotion: PolygonSyntyCharacter.fbx not found, skipping");
        return null;
    }

    var characterOutputDir = Path.Combine(outputDir, "Character");
    Directory.CreateDirectory(characterOutputDir);

    var characterExportDesc = new AssetExportDesc
    {
        SourcePath = characterFile,
        OutputDirectory = characterOutputDir,
        AssetName = "PolygonSyntyCharacter",
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
        SanitizeTransforms = true,
        RepairSkeletonTransforms = true
    };

    var charResult = exporter.Export(characterExportDesc);
    if (!charResult.Success)
    {
        logger.LogError("BaseLocomotion: PolygonSyntyCharacter export failed: {Error}", charResult.ErrorMessage);
        return null;
    }

    if (charResult.OzzSkeleton == null)
    {
        logger.LogWarning("BaseLocomotion: No skeleton exported for PolygonSyntyCharacter");
        return null;
    }

    // Import BaseLocomotion Polygon animations
    var polygonAnimDir = Path.Combine(animationsDir, "Polygon");
    if (Directory.Exists(polygonAnimDir))
    {
        ImportAnimationsWithSkeleton(exporter, polygonAnimDir, animationsDir, Path.Combine(outputDir, "Animations"), charResult.OzzSkeleton, "BaseLocomotion/Polygon");
    }

    // Import additional animation packs using the same Polygon skeleton
    var animationPacks = new[] { "BowCombat", "MeleeCombat", "Idles" };
    foreach (var pack in animationPacks)
    {
        var packAnimDir = Path.Combine(syntySourceDir, pack, "Animations");
        if (Directory.Exists(packAnimDir))
        {
            ImportAnimationsWithSkeleton(exporter, packAnimDir, packAnimDir, Path.Combine(outputDir, "Animations", pack), charResult.OzzSkeleton, pack);
        }
    }

    return charResult.OzzSkeleton;
}

void ImportAnimationsWithSkeleton(AssetExporter exporter, string animSourceDir, string baseDir, string outputDir, OzzSkeleton skeleton, string packName)
{
    foreach (var animFile in Directory.EnumerateFiles(animSourceDir, "*.fbx", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(baseDir, animFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
        var animName = Path.GetFileNameWithoutExtension(animFile);
        var animOutputDir = Path.Combine(outputDir, relativeDir);
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
            ExternalSkeleton = skeleton
        };

        var animResult = exporter.Export(animExportDesc);
        if (!animResult.Success)
        {
            logger.LogError("  {PackName} animation failed: {Name}: {Error}", packName, animName, animResult.ErrorMessage);
        }
    }

    logger.LogInformation("{PackName}: Animations imported", packName);
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
