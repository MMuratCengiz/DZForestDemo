using OfflineAssets;

const string projectDir = @"C:\Workspace\DZForestDemo\DZForestDemo";

// Viking asset paths
const string vikingSourceDir = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles";
const string vikingFbxPath = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles\FBX\VikingRealm_Characters.fbx";
const string vikingTexturePath = @"C:\Users\cengi\Downloads\POLYGON_Viking_Realm_SourceFiles_v3\SourceFiles\Textures\Alts\PolygonVikingRealm_Texture_01_A.png";

// Fox asset paths
const string foxSourceDir = @"C:\Workspace\DenOfIz\Examples\Assets\Models";
const string foxGltfPath = @"C:\Workspace\DenOfIz\Examples\Assets\Models\Fox.gltf";
const string foxTexturePath = @"C:\Workspace\DenOfIz\Examples\Assets\Models\Texture.png";

var assetProject = AssetProject.ForProjectAssets(projectDir, vikingSourceDir);
assetProject.EnsureDirectories();

Console.WriteLine("Asset Import Tool");
Console.WriteLine("=================");
Console.WriteLine($"Output Models: {assetProject.ModelsDirectory}");
Console.WriteLine($"Output Textures: {assetProject.TexturesDirectory}");
Console.WriteLine($"Output Animations: {assetProject.AnimationsDirectory}");
Console.WriteLine($"Output Skeletons: {assetProject.SkeletonsDirectory}");
Console.WriteLine();

// Import Fox assets
Console.WriteLine("=== FOX ASSETS ===");
var foxModelResult = ImportModel(assetProject, foxGltfPath, "Fox", scale: 0.1f);
if (foxModelResult)
{
    ImportTexture(assetProject, foxTexturePath, "Fox");
}

Console.WriteLine();

// Import Viking assets
Console.WriteLine("=== VIKING ASSETS ===");
var vikingModelResult = ImportModel(assetProject, vikingFbxPath, "VikingRealm_Characters", scale: 1.0f);
if (vikingModelResult)
{
    ImportTexture(assetProject, vikingTexturePath, "VikingRealm_Texture_01_A");
}

Console.WriteLine();
Console.WriteLine("Import complete!");
return 0;

bool ImportModel(AssetProject project, string sourcePath, string assetName, float scale = 1.0f)
{
    using var exporter = new AssetExporter();

    Console.WriteLine($"Supported model extensions: {string.Join(", ", exporter.SupportedExtensions)}");
    Console.WriteLine();

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Model file not found: {sourcePath}");
        return false;
    }

    if (!exporter.CanProcess(sourcePath))
    {
        Console.WriteLine($"ERROR: Cannot process file: {sourcePath}");
        return false;
    }

    Console.WriteLine($"Importing model: {Path.GetFileName(sourcePath)} as '{assetName}'");

    var settings = project.CreateExportSettings(sourcePath, assetName);
    settings.Format = ExportFormat.Glb;
    settings.Scale = scale;
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

        project.CopyToOutput(result, separateAnimations: true, separateSkeletons: true);
        return true;
    }

    Console.WriteLine($"Model import FAILED: {result.ErrorMessage}");
    return false;
}

bool ImportTexture(AssetProject project, string sourcePath, string assetName)
{
    using var textureExporter = new TextureExporter();

    Console.WriteLine();
    Console.WriteLine($"Supported texture extensions: {string.Join(", ", textureExporter.SupportedExtensions)}");

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Texture file not found: {sourcePath}");
        return false;
    }

    Console.WriteLine($"Importing texture: {Path.GetFileName(sourcePath)} as '{assetName}'");

    var settings = project.CreateTextureExportSettings(sourcePath, assetName);
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
