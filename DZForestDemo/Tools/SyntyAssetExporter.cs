using NiziKit.Offline;

namespace DZForestDemo.Tools;

/// <summary>
/// Utility to export Synty PolygonStreetRacer assets from FBX to GLB format.
/// Run with: dotnet run -- --export-synty [source-folder] [output-folder]
/// </summary>
public static class SyntyAssetExporter
{
    // Key assets for racing game
    private static readonly string[] CarModels =
    [
        "SM_Veh_Exotic_01.fbx",      // Supercar
        "SM_Veh_Sports_01.fbx",      // Sports car
        "SM_Veh_Muscle_01.fbx",      // Muscle car
        "SM_Veh_Hatch_01.fbx",       // Hatchback
        "SM_Veh_Sedan_01.fbx",       // Sedan (may be skeletal)
    ];

    private static readonly string[] WheelModels =
    [
        "SM_Veh_Attach_Wheel_01.fbx",
        "SM_Veh_Attach_Wheel_02.fbx",
        "SM_Veh_Attach_Wheel_03.fbx",
        "SM_Veh_Attach_Wheel_04.fbx",
        "SM_Veh_Attach_Wheel_05.fbx",
    ];

    private static readonly string[] TrackProps =
    [
        "SM_Env_Ground_01.fbx",
        "SM_Env_Ground_Barrier_01.fbx",
        "SM_Prop_Barrier_Roadblock_01.fbx",
        "FX_Checkpoint_Cylinder_01.fbx",
        // Buildings
        "SM_Bld_SingleGarage_01.fbx",
        "SM_Bld_Warehouse_01.fbx",
        "SM_Bld_Shelter_01.fbx",
        "SM_Bld_Shelter_02.fbx",
        "SM_Bld_RepairShop_Medium_01.fbx",
        // Tire stacks
        "SM_Prop_TyreStack_01.fbx",
        "SM_Prop_TyreStack_02.fbx",
        // Barriers and cones
        "SM_Prop_Barrier_Cone_01.fbx",
        "SM_Prop_Barrier_Cone_02.fbx",
        "SM_Prop_Barrier_Concrete_01.fbx",
        "SM_Prop_Barrier_Concrete_02.fbx",
        "SM_Prop_Barrier_CrashBarrel_01.fbx",
        "SM_Prop_Barrier_Barrel_01.fbx",
        "SM_Prop_Barrier_Plastic_01.fbx",
        // Containers
        "SM_Prop_Container_01.fbx",
        "SM_Prop_Container_Stack_01.fbx",
        "SM_Prop_Container_Large_01.fbx",
        // Signs
        "SM_Prop_Sign_Start_01.fbx",
        "SM_Prop_Sign_Finish_01.fbx",
        "SM_Prop_Sign_Checkpoint_01.fbx",
        "SM_Prop_Sign_Arrow_Up_01.fbx",
        "SM_Prop_Sign_Arrow_Corner_01.fbx",
        "SM_Prop_Sign_Caution_01.fbx",
        // Lights
        "SM_Prop_LightsFlat_01.fbx",
        "SM_Prop_LightsAngle_01.fbx",
        // Misc
        "SM_Prop_Stand_01.fbx",
        "SM_Prop_OilCan_01.fbx",
        "SM_Prop_Fence_Wire_Single_01.fbx",
    ];

    public static void ExportRacingAssets(string sourceFbxFolder, string outputFolder)
    {
        Console.WriteLine("=== Synty PolygonStreetRacer Asset Exporter ===");
        Console.WriteLine($"Source: {sourceFbxFolder}");
        Console.WriteLine($"Output: {outputFolder}");
        Console.WriteLine();

        using var exporter = new AssetExporter();
        Directory.CreateDirectory(outputFolder);

        // Export all to same folder for simpler asset loading
        Console.WriteLine("Exporting car models...");
        foreach (var model in CarModels)
        {
            ExportModel(exporter, sourceFbxFolder, model, outputFolder);
        }

        Console.WriteLine("\nExporting wheel models...");
        foreach (var model in WheelModels)
        {
            ExportModel(exporter, sourceFbxFolder, model, outputFolder);
        }

        Console.WriteLine("\nExporting track props...");
        foreach (var model in TrackProps)
        {
            ExportModel(exporter, sourceFbxFolder, model, outputFolder);
        }

        Console.WriteLine("\n=== Export Complete ===");
    }

    public static void ExportAllModels(string sourceFbxFolder, string outputFolder)
    {
        Console.WriteLine("=== Exporting ALL Synty FBX Models ===");
        Console.WriteLine($"Source: {sourceFbxFolder}");
        Console.WriteLine($"Output: {outputFolder}");
        Console.WriteLine();

        using var exporter = new AssetExporter();
        Directory.CreateDirectory(outputFolder);

        var fbxFiles = Directory.GetFiles(sourceFbxFolder, "*.fbx");
        var successCount = 0;
        var failCount = 0;

        foreach (var fbxFile in fbxFiles)
        {
            var fileName = Path.GetFileName(fbxFile);
            if (ExportModel(exporter, sourceFbxFolder, fileName, outputFolder))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        Console.WriteLine($"\n=== Export Complete: {successCount} succeeded, {failCount} failed ===");
    }

    private static bool ExportModel(AssetExporter exporter, string sourceFolder, string modelName, string outputFolder)
    {
        var sourcePath = Path.Combine(sourceFolder, modelName);
        if (!File.Exists(sourcePath))
        {
            Console.WriteLine($"  [SKIP] {modelName} - not found");
            return false;
        }

        var assetName = Path.GetFileNameWithoutExtension(modelName);
        var desc = new AssetExportDesc
        {
            SourcePath = sourcePath,
            OutputDirectory = outputFolder,
            AssetName = assetName,
            Format = ExportFormat.Glb,
            Scale = 1.0f,
            ExportSkeleton = false,  // Static meshes don't need skeleton
            ExportAnimations = false,
            OptimizeMeshes = true,
            GenerateNormals = true,
            CalculateTangents = true,
            TriangulateMeshes = true,
            JoinIdenticalVertices = true
        };

        var result = exporter.Export(desc);
        if (result.Success)
        {
            Console.WriteLine($"  [OK] {modelName} -> {assetName}.glb");
            return true;
        }
        else
        {
            Console.WriteLine($"  [FAIL] {modelName}: {result.ErrorMessage}");
            return false;
        }
    }

    public static void CopyTextures(string sourceTextureFolder, string outputFolder)
    {
        Console.WriteLine("=== Copying Textures ===");

        var texturesOutput = Path.Combine(outputFolder, "Textures");
        Directory.CreateDirectory(texturesOutput);

        // Copy vehicle textures
        var vehicleTextures = Path.Combine(sourceTextureFolder, "Vehicles");
        if (Directory.Exists(vehicleTextures))
        {
            var files = Directory.GetFiles(vehicleTextures, "*.png");
            foreach (var file in files)
            {
                var destPath = Path.Combine(texturesOutput, Path.GetFileName(file));
                File.Copy(file, destPath, overwrite: true);
                Console.WriteLine($"  Copied: {Path.GetFileName(file)}");
            }
        }

        // Copy road textures
        var roadTextures = Path.Combine(sourceTextureFolder, "Roads");
        if (Directory.Exists(roadTextures))
        {
            var files = Directory.GetFiles(roadTextures, "*.png");
            foreach (var file in files)
            {
                var destPath = Path.Combine(texturesOutput, Path.GetFileName(file));
                File.Copy(file, destPath, overwrite: true);
                Console.WriteLine($"  Copied: {Path.GetFileName(file)}");
            }
        }

        // Copy main atlas textures
        var atlasFiles = Directory.GetFiles(sourceTextureFolder, "PolygonStreetRacer_Texture_*.png");
        foreach (var file in atlasFiles)
        {
            var destPath = Path.Combine(texturesOutput, Path.GetFileName(file));
            File.Copy(file, destPath, overwrite: true);
            Console.WriteLine($"  Copied: {Path.GetFileName(file)}");
        }

        Console.WriteLine("=== Textures Copied ===");
    }
}
