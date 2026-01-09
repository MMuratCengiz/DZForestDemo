using OfflineAssets;

const string projectDir = @"/Users/muratcengiz/RiderProjects/DZForestDemo/DZForestDemo/";

const string foxGltfPath = @"/Users/muratcengiz/RiderProjects/DZForestDemo/DZForestDemo/Assets/Models/Fox.gltf";
const string foxTexturePath = @"/Users/muratcengiz/RiderProjects/DZForestDemo/DZForestDemo/Assets/Models/Texture.png";

var assetProject = AssetDirectories.ForProjectAssets(projectDir, projectDir);
assetProject.EnsureDirectories();

var meshesDir = Path.Combine(assetProject.Output, "Meshes");
Directory.CreateDirectory(meshesDir);

Console.WriteLine("Asset Import Tool");
Console.WriteLine("=================");
Console.WriteLine($"Output Models: {assetProject.Models}");
Console.WriteLine($"Output Meshes: {meshesDir}");
Console.WriteLine($"Output Textures: {assetProject.Textures}");
Console.WriteLine($"Output Animations: {assetProject.Animations}");
Console.WriteLine($"Output Skeletons: {assetProject.Skeletons}");
Console.WriteLine();

Console.WriteLine("=== INSPECT GLTF ===");
var inspector = new GltfInspector();
var inspectionResult = inspector.Inspect(foxGltfPath);

if (!inspectionResult.Success)
{
    Console.WriteLine($"Failed to inspect glTF: {inspectionResult.ErrorMessage}");
    return 1;
}

Console.WriteLine($"Found {inspectionResult.Meshes.Count} meshes:");
foreach (var mesh in inspectionResult.Meshes)
{
    Console.WriteLine($"  [{mesh.Index}] {mesh.Name}");
    Console.WriteLine($"      Vertices: {mesh.VertexCount}, Indices: {mesh.IndexCount}");
    Console.WriteLine($"      Primitives: {mesh.PrimitiveCount}, Skinned: {mesh.HasSkinning}");
    Console.WriteLine($"      Material Index: {mesh.MaterialIndex}");
}

Console.WriteLine();
Console.WriteLine($"Found {inspectionResult.Materials.Count} materials:");
foreach (var mat in inspectionResult.Materials)
{
    Console.WriteLine($"  [{mat.Index}] {mat.Name}");
    Console.WriteLine($"      BaseColor: ({mat.BaseColor.X:F2}, {mat.BaseColor.Y:F2}, {mat.BaseColor.Z:F2}, {mat.BaseColor.W:F2})");
    Console.WriteLine($"      Metallic: {mat.Metallic:F2}, Roughness: {mat.Roughness:F2}");
    if (mat.BaseColorTexturePath != null)
    {
        Console.WriteLine($"      BaseColorTexture: {Path.GetFileName(mat.BaseColorTexturePath)}");
    }
}

Console.WriteLine();
Console.WriteLine($"Has Animations: {inspectionResult.HasAnimations}");
Console.WriteLine($"Has Skins: {inspectionResult.HasSkins}");

Console.WriteLine();
Console.WriteLine("=== EXPORT MESHES TO .DZMESH ===");
var meshExporter = new MeshExporter();
var meshResults = meshExporter.ExportAllMeshes(foxGltfPath, meshesDir, includeMaterials: true);

foreach (var result in meshResults)
{
    if (result.Success)
    {
        Console.WriteLine($"Exported: {Path.GetFileName(result.OutputPath)}");
        Console.WriteLine($"  Vertices: {result.VertexCount}, Indices: {result.IndexCount}");
    }
    else
    {
        Console.WriteLine($"Failed: {result.ErrorMessage}");
    }
}

Console.WriteLine();
Console.WriteLine("=== EXPORT ANIMATIONS (OZZ) ===");
ImportAnimations(assetProject, foxGltfPath, "Fox", scale: 0.1f);

Console.WriteLine();
Console.WriteLine("=== EXPORT TEXTURES ===");
ImportTexture(assetProject, foxTexturePath, "Fox");

Console.WriteLine();
Console.WriteLine("Import complete!");
return 0;

bool ImportAnimations(AssetDirectories project, string sourcePath, string assetName, float scale = 1.0f)
{
    using var exporter = new AssetExporter();

    if (!File.Exists(sourcePath))
    {
        Console.WriteLine($"ERROR: Model file not found: {sourcePath}");
        return false;
    }

    Console.WriteLine($"Exporting animations from: {Path.GetFileName(sourcePath)}");

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
        Console.WriteLine("Animation export SUCCESS!");
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

    Console.WriteLine($"Animation export FAILED: {result.ErrorMessage}");
    return false;
}

bool ImportTexture(AssetDirectories project, string sourcePath, string assetName)
{
    using var textureExporter = new TextureExporter();

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
