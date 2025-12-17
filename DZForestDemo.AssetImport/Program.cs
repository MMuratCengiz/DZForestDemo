using OfflineAssets;

const string vikingSourceDir = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles";
const string fbxPath = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles\FBX\VikingRealm_Characters.fbx";
const string texturePath = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles\Textures\Alts\PolygonVikingRealm_Texture_01_A.png";
const string projectDir = @"C:\Workspace\DZForestDemo\DZForestDemo";

var assetProject = AssetProject.ForProjectAssets(projectDir, vikingSourceDir);
assetProject.EnsureDirectories();

Console.WriteLine("Asset Import Tool");
Console.WriteLine("=================");
Console.WriteLine($"Source: {vikingSourceDir}");
Console.WriteLine($"Output Models: {assetProject.ModelsDirectory}");
Console.WriteLine($"Output Textures: {assetProject.TexturesDirectory}");
Console.WriteLine($"Output Animations: {assetProject.AnimationsDirectory}");
Console.WriteLine();

var modelResult = ImportModel(assetProject, fbxPath);
if (!modelResult)
{
    return 1;
}

var textureResult = ImportTexture(assetProject, texturePath);
if (!textureResult)
{
    return 1;
}

Console.WriteLine();
Console.WriteLine("Import complete!");
return 0;

bool ImportModel(AssetProject project, string sourcePath)
{
    using var exporter = new AssetExporter();

    Console.WriteLine($"Supported model extensions: {string.Join(", ", exporter.SupportedExtensions)}");
    Console.WriteLine();

    if (!exporter.CanProcess(sourcePath))
    {
        Console.WriteLine($"ERROR: Cannot process file: {sourcePath}");
        return false;
    }

    Console.WriteLine($"Importing model: {Path.GetFileName(sourcePath)}");

    var settings = project.CreateExportSettings(sourcePath, "VikingRealm_Characters");
    settings.Format = ExportFormat.Glb;
    settings.Scale = 1.0f;
    settings.EmbedTextures = false;
    settings.OverwriteExisting = true;
    settings.OptimizeMeshes = true;
    settings.GenerateNormals = true;
    settings.CalculateTangents = true;
    settings.TriangulateMeshes = true;
    settings.JoinIdenticalVertices = true;
    settings.SmoothNormals = true;
    settings.SmoothNormalsAngle = 80.0f;
    settings.ExportSkeleton = true;
    settings.ExportAnimations = true;
    settings.OutputHandedness = Handedness.Left;

    Console.WriteLine($"  Format: {settings.Format}");
    Console.WriteLine($"  Scale: {settings.Scale}");
    Console.WriteLine($"  Embed Textures: {settings.EmbedTextures}");
    Console.WriteLine($"  Output Handedness: {settings.OutputHandedness}");
    Console.WriteLine();

    var result = exporter.Export(settings);

    if (result.Success)
    {
        Console.WriteLine("Model import SUCCESS!");
        Console.WriteLine($"  Output: {result.OutputPath}");

        if (!string.IsNullOrEmpty(result.SkeletonPath))
        {
            Console.WriteLine($"  Skeleton: {result.SkeletonPath}");
        }

        if (result.AnimationPaths.Count > 0)
        {
            Console.WriteLine($"  Animations ({result.AnimationPaths.Count}):");
            foreach (var animPath in result.AnimationPaths)
            {
                Console.WriteLine($"    - {Path.GetFileName(animPath)}");
            }
        }

        project.CopyToOutput(result, separateAnimations: true);
        return true;
    }

    Console.WriteLine($"Model import FAILED: {result.ErrorMessage}");
    return false;
}

bool ImportTexture(AssetProject project, string sourcePath)
{
    using var textureExporter = new TextureExporter();

    Console.WriteLine();
    Console.WriteLine($"Supported texture extensions: {string.Join(", ", textureExporter.SupportedExtensions)}");

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Texture file not found: {sourcePath}");
        return false;
    }

    Console.WriteLine($"Importing texture: {Path.GetFileName(sourcePath)}");

    var settings = project.CreateTextureExportSettings(sourcePath, "VikingRealm_Texture_01_A");
    settings.GenerateMips = true;
    settings.FlipY = false;

    var result = textureExporter.Export(settings);

    if (result.Success)
    {
        Console.WriteLine("Texture import SUCCESS!");
        Console.WriteLine($"  Output: {result.OutputPath}");
        return true;
    }

    Console.WriteLine($"Texture import FAILED: {result.ErrorMessage}");
    return false;
}
