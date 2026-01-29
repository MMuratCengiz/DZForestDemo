using Silk.NET.Assimp;
using File = System.IO.File;

namespace NiziKit.Offline;

public enum GltfExportFormat
{
    Glb,
    Gltf
}

public sealed class GltfExportDesc
{
    public string SourceFilePath { get; set; } = string.Empty;
    public string TargetDirectory { get; set; } = string.Empty;
    public string AssetNamePrefix { get; set; } = string.Empty;
    public GltfExportFormat OutputFormat { get; set; } = GltfExportFormat.Glb;
    public float ScaleFactor { get; set; } = 1.0f;
    public bool TriangulateMeshes { get; set; } = true;
    public bool JoinIdenticalVertices { get; set; } = true;
    public bool CalculateTangentSpace { get; set; } = true;
    public bool FixInfacingNormals { get; set; }
    public bool LimitBoneWeights { get; set; } = true;
    public uint MaxBoneWeightsPerVertex { get; set; } = 4;
    public bool RemoveRedundantMaterials { get; set; } = true;
    public bool GenerateNormals { get; set; } = true;
    public bool SmoothNormals { get; set; } = true;
    public float SmoothNormalsAngle { get; set; } = 80.0f;
    public bool PreTransformVertices { get; set; }
    public bool OptimizeGraph { get; set; }
    public bool OptimizeMeshes { get; set; } = true;
    public bool MergeMeshes { get; set; }
    public bool DropNormals { get; set; }
    public bool FbxPreservePivots { get; set; } = false;
}

public sealed class GltfExportResult
{
    public bool Success { get; init; }
    public string? GltfFilePath { get; init; }
    public byte[]? GltfBytes { get; init; }
    public string? ErrorMessage { get; init; }

    public static GltfExportResult Succeeded(string gltfPath)
    {
        return new GltfExportResult
        {
            Success = true,
            GltfFilePath = gltfPath
        };
    }

    public static GltfExportResult SucceededWithBytes(byte[] bytes)
    {
        return new GltfExportResult
        {
            Success = true,
            GltfBytes = bytes
        };
    }

    public static GltfExportResult Failed(string error)
    {
        return new GltfExportResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}

public sealed class GltfExporter : IDisposable
{
    private const uint AiProcessGlobalScale = 0x8000000;
    private const string AiConfigGlobalScaleFactorKey = "GLOBAL_SCALE_FACTOR_KEY";
    private const string AiConfigFbxPreservePivotsKey = "IMPORT_FBX_PRESERVE_PIVOTS";

    private static readonly string[] SupportedExtensionsList =
    [
        ".fbx", ".gltf", ".glb", ".obj", ".dae", ".blend", ".3ds", ".ase", ".ifc", ".xgl",
        ".zgl", ".ply", ".dxf", ".lwo", ".lws", ".lxo", ".stl", ".x", ".ac", ".ms3d"
    ];

    private readonly Assimp _assimp = Assimp.GetApi();

    public IReadOnlyList<string> SupportedExtensions => SupportedExtensionsList;

    public bool CanProcessFileExtension(string extension)
    {
        var ext = extension.ToLowerInvariant();
        return SupportedExtensionsList.Contains(ext);
    }

    public bool ValidateFile(string filePath)
    {
        return File.Exists(filePath);
    }

    public unsafe GltfExportResult Export(in GltfExportDesc desc)
    {
        if (string.IsNullOrEmpty(desc.SourceFilePath))
        {
            return GltfExportResult.Failed("Source file path is required.");
        }

        if (!File.Exists(desc.SourceFilePath))
        {
            return GltfExportResult.Failed($"Source file not found: {desc.SourceFilePath}");
        }

        var importFlags = PostProcessSteps.ImproveCacheLocality |
                          PostProcessSteps.SortByPrimitiveType |
                          PostProcessSteps.ValidateDataStructure;

        if (desc.TriangulateMeshes)
        {
            importFlags |= PostProcessSteps.Triangulate;
        }

        if (desc.JoinIdenticalVertices)
        {
            importFlags |= PostProcessSteps.JoinIdenticalVertices;
        }

        if (desc.CalculateTangentSpace)
        {
            importFlags |= PostProcessSteps.CalculateTangentSpace;
        }

        if (desc.FixInfacingNormals)
        {
            importFlags |= PostProcessSteps.FixInFacingNormals;
        }

        if (desc.LimitBoneWeights)
        {
            importFlags |= PostProcessSteps.LimitBoneWeights;
        }

        if (desc.RemoveRedundantMaterials)
        {
            importFlags |= PostProcessSteps.RemoveRedundantMaterials;
        }

        if (desc.GenerateNormals && !desc.DropNormals)
        {
            importFlags |= desc.SmoothNormals
                ? PostProcessSteps.GenerateSmoothNormals
                : PostProcessSteps.GenerateNormals;
        }

        if (desc.PreTransformVertices)
        {
            importFlags |= PostProcessSteps.PreTransformVertices;
        }
        else if (desc.OptimizeGraph)
        {
            importFlags |= PostProcessSteps.OptimizeGraph;
        }

        if (desc.OptimizeMeshes)
        {
            importFlags |= PostProcessSteps.OptimizeMeshes;
        }

        if (desc.MergeMeshes)
        {
            importFlags |= PostProcessSteps.OptimizeMeshes |
                           PostProcessSteps.JoinIdenticalVertices |
                           PostProcessSteps.SortByPrimitiveType;
        }

        var propertyStore = _assimp.CreatePropertyStore();
        _assimp.SetImportPropertyFloat(propertyStore, AiConfigGlobalScaleFactorKey, desc.ScaleFactor);
        _assimp.SetImportPropertyInteger(propertyStore, AiConfigFbxPreservePivotsKey, desc.FbxPreservePivots ? 1 : 0);

        var finalFlags = (uint)importFlags | AiProcessGlobalScale;
        var scene = _assimp.ImportFileExWithProperties(desc.SourceFilePath, finalFlags, null, propertyStore);
        _assimp.ReleasePropertyStore(propertyStore);

        if (scene == null || scene->MRootNode == null)
        {
            var errorPtr = _assimp.GetErrorString();
            var error = errorPtr != null ? new string((sbyte*)errorPtr) : "Unknown error";
            return GltfExportResult.Failed($"Failed to load scene: {error}");
        }

        try
        {
            Directory.CreateDirectory(desc.TargetDirectory);

            var baseName = string.IsNullOrEmpty(desc.AssetNamePrefix)
                ? Path.GetFileNameWithoutExtension(desc.SourceFilePath)
                : desc.AssetNamePrefix;

            var extension = desc.OutputFormat == GltfExportFormat.Glb ? ".glb" : ".gltf";
            var outputPath = Path.Combine(desc.TargetDirectory, baseName + extension);

            var formatId = desc.OutputFormat == GltfExportFormat.Glb ? "glb2" : "gltf2";

            var exportFlags = PostProcessSteps.JoinIdenticalVertices |
                              PostProcessSteps.Triangulate |
                              PostProcessSteps.SortByPrimitiveType;

            var result = _assimp.ExportScene(scene, formatId, outputPath, (uint)exportFlags);

            if (result != Return.Success)
            {
                var errorPtr = _assimp.GetErrorString();
                var error = errorPtr != null ? new string((sbyte*)errorPtr) : "Unknown error";
                return GltfExportResult.Failed($"Failed to write glTF file: {error}");
            }

            return GltfExportResult.Succeeded(outputPath);
        }
        finally
        {
            _assimp.ReleaseImport(scene);
        }
    }

    public unsafe GltfExportResult ExportToBytes(in GltfExportDesc desc)
    {
        if (string.IsNullOrEmpty(desc.SourceFilePath))
        {
            return GltfExportResult.Failed("Source file path is required.");
        }

        if (!System.IO.File.Exists(desc.SourceFilePath))
        {
            return GltfExportResult.Failed($"Source file not found: {desc.SourceFilePath}");
        }

        var importFlags = BuildImportFlags(desc);

        var propertyStore = _assimp.CreatePropertyStore();
        _assimp.SetImportPropertyFloat(propertyStore, AiConfigGlobalScaleFactorKey, desc.ScaleFactor);
        _assimp.SetImportPropertyInteger(propertyStore, AiConfigFbxPreservePivotsKey, desc.FbxPreservePivots ? 1 : 0);

        var finalFlags = (uint)importFlags | AiProcessGlobalScale;
        var scene = _assimp.ImportFileExWithProperties(desc.SourceFilePath, finalFlags, null, propertyStore);
        _assimp.ReleasePropertyStore(propertyStore);

        if (scene == null || scene->MRootNode == null)
        {
            var errorPtr = _assimp.GetErrorString();
            var error = errorPtr != null ? new string((sbyte*)errorPtr) : "Unknown error";
            return GltfExportResult.Failed($"Failed to load scene: {error}");
        }

        try
        {
            var formatId = desc.OutputFormat == GltfExportFormat.Glb ? "glb2" : "gltf2";

            var exportFlags = PostProcessSteps.JoinIdenticalVertices |
                              PostProcessSteps.Triangulate |
                              PostProcessSteps.SortByPrimitiveType;

            var blob = _assimp.ExportSceneToBlob(scene, formatId, (uint)exportFlags);
            if (blob == null)
            {
                var errorPtr = _assimp.GetErrorString();
                var error = errorPtr != null ? new string((sbyte*)errorPtr) : "Unknown error";
                return GltfExportResult.Failed($"Failed to export scene to blob: {error}");
            }

            try
            {
                var size = (int)blob->Size;
                var bytes = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy((nint)blob->Data, bytes, 0, size);
                return GltfExportResult.SucceededWithBytes(bytes);
            }
            finally
            {
                _assimp.ReleaseExportBlob(blob);
            }
        }
        finally
        {
            _assimp.ReleaseImport(scene);
        }
    }

    private PostProcessSteps BuildImportFlags(in GltfExportDesc desc)
    {
        var importFlags = PostProcessSteps.ImproveCacheLocality |
                          PostProcessSteps.SortByPrimitiveType |
                          PostProcessSteps.ValidateDataStructure;

        if (desc.TriangulateMeshes)
        {
            importFlags |= PostProcessSteps.Triangulate;
        }

        if (desc.JoinIdenticalVertices)
        {
            importFlags |= PostProcessSteps.JoinIdenticalVertices;
        }

        if (desc.CalculateTangentSpace)
        {
            importFlags |= PostProcessSteps.CalculateTangentSpace;
        }

        if (desc.FixInfacingNormals)
        {
            importFlags |= PostProcessSteps.FixInFacingNormals;
        }

        if (desc.LimitBoneWeights)
        {
            importFlags |= PostProcessSteps.LimitBoneWeights;
        }

        if (desc.RemoveRedundantMaterials)
        {
            importFlags |= PostProcessSteps.RemoveRedundantMaterials;
        }

        if (desc.GenerateNormals && !desc.DropNormals)
        {
            importFlags |= desc.SmoothNormals
                ? PostProcessSteps.GenerateSmoothNormals
                : PostProcessSteps.GenerateNormals;
        }
        if (desc.PreTransformVertices)
        {
            importFlags |= PostProcessSteps.PreTransformVertices;
        }
        else if (desc.OptimizeGraph)
        {
            importFlags |= PostProcessSteps.OptimizeGraph;
        }

        if (desc.OptimizeMeshes)
        {
            importFlags |= PostProcessSteps.OptimizeMeshes;
        }

        if (desc.MergeMeshes)
        {
            importFlags |= PostProcessSteps.OptimizeMeshes |
                           PostProcessSteps.JoinIdenticalVertices |
                           PostProcessSteps.SortByPrimitiveType;
        }

        return importFlags;
    }

    public void Dispose()
    {
        _assimp.Dispose();
    }
}
