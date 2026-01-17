using NiziKit.ContentPipeline;
using NiziKit.Offline;

var verbose = args.Contains("-v") || args.Contains("--verbose");

var projectDir = Solution.Project("DZForestDemo");
var sourceDir = Path.Combine(projectDir, "Assets", "Models");
var foxGltfPath = Path.Combine(sourceDir, "Fox.gltf");
var foxTexturePath = Path.Combine(sourceDir, "Texture.png");

var assetProject = AssetDirectories.ForProjectAssets(projectDir, projectDir);
assetProject.EnsureDirectories();

var meshesDir = Path.Combine(assetProject.Output, "Meshes");
Directory.CreateDirectory(meshesDir);

Log($"Project: {projectDir}");

var inspector = new GltfInspector();
var inspectionResult = inspector.Inspect(foxGltfPath);

if (!inspectionResult.Success)
{
    Console.WriteLine($"ERROR: {inspectionResult.ErrorMessage}");
    return 1;
}

LogVerbose($"Found {inspectionResult.Meshes.Count} meshes, {inspectionResult.Materials.Count} materials");

var meshExporter = new MeshExporter();
var meshResults = meshExporter.ExportAllMeshes(foxGltfPath, meshesDir, includeMaterials: true);

var meshSuccess = 0;
var meshFail = 0;
foreach (var result in meshResults)
{
    if (result.Success)
    {
        meshSuccess++;
        LogVerbose($"  {Path.GetFileName(result.OutputPath)}");
    }
    else
    {
        meshFail++;
        Console.WriteLine($"  FAILED: {result.ErrorMessage}");
    }
}
Log($"Meshes: {meshSuccess} exported" + (meshFail > 0 ? $", {meshFail} failed" : ""));

if (ImportAnimations(assetProject, foxGltfPath, "Fox", 0.1f))
{
    Log("Animations: exported");
}

if (ImportTexture(assetProject, foxTexturePath, "Fox"))
{
    Log("Textures: exported");
}

var manifest = AssetManifest.GenerateFromDirectory(assetProject.Output);
manifest.SaveToDirectory(assetProject.Output);
Log($"Manifest: {manifest.Assets.Count} assets");

Log("Done!");
return 0;

void Log(string msg) => Console.WriteLine(msg);
void LogVerbose(string msg) { if (verbose)
    {
        Console.WriteLine(msg);
    }
}

bool ImportAnimations(AssetDirectories project, string sourcePath, string assetName, float scale)
{
    using var exporter = new AssetExporter();

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Model not found: {sourcePath}");
        return false;
    }

    LogVerbose($"  Source: {Path.GetFileName(sourcePath)}");

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

    var result = exporter.Export(settings);

    if (result.Success)
    {
        LogVerbose($"  Skeleton: {Path.GetFileName(result.SkeletonPath ?? "")}");
        LogVerbose($"  Clips: {result.AnimationPaths.Count}");
        project.CopyToOutput(result, separateAnimations: true, separateSkeletons: true);
        return true;
    }

    Console.WriteLine($"ERROR: {result.ErrorMessage}");
    return false;
}

bool ImportTexture(AssetDirectories project, string sourcePath, string assetName)
{
    using var textureExporter = new TextureExporter();

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Texture not found: {sourcePath}");
        return false;
    }

    LogVerbose($"  Source: {Path.GetFileName(sourcePath)}");

    var settings = project.CreateTextureExportSettings(sourcePath, assetName);
    settings.GenerateMips = true;
    settings.FlipY = false;

    var result = textureExporter.Export(settings);

    if (result.Success)
    {
        LogVerbose($"  Output: {Path.GetFileName(result.OutputPath)}");
        return true;
    }

    Console.WriteLine($"ERROR: {result.ErrorMessage}");
    return false;
}
