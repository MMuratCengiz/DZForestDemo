using DenOfIz;
using NiziKit.Assets;
using NiziKit.Assets.Serde;
using NiziKit.GLTF;

namespace NiziKit.Offline;

public sealed class AssetExporter : IDisposable
{
    private readonly GltfExporter _gltfExporter = new();

    public IReadOnlyList<string> SupportedExtensions => _gltfExporter.SupportedExtensions;

    public void Dispose()
    {
        _gltfExporter.Dispose();
    }

    public bool CanProcess(string filePath)
    {
        return _gltfExporter.ValidateFile(filePath);
    }

    public AssetExportResult Export(AssetExportDesc desc)
    {
        if (string.IsNullOrEmpty(desc.SourcePath))
        {
            return AssetExportResult.Failed("Source path is required.");
        }

        if (!File.Exists(desc.SourcePath))
        {
            return AssetExportResult.Failed($"Source file not found: {desc.SourcePath}");
        }

        if (string.IsNullOrEmpty(desc.OutputDirectory))
        {
            return AssetExportResult.Failed("Output directory is required.");
        }

        if (string.IsNullOrEmpty(desc.AssetName))
        {
            desc.AssetName = Path.GetFileNameWithoutExtension(desc.SourcePath);
        }

        Directory.CreateDirectory(desc.OutputDirectory);

        var gltfResult = ExportGltfToBytes(desc);
        if (!gltfResult.Success || gltfResult.GltfBytes == null)
        {
            return AssetExportResult.Failed(gltfResult.ErrorMessage ?? "GLTF export failed.");
        }

        var gltfBytes = gltfResult.GltfBytes;

        if (desc.RepairSkeletonTransforms)
        {
            gltfBytes = SkeletonRepair.RepairGlb(gltfBytes);
        }

        var meshPaths = ExportMeshes(desc, gltfBytes);

        string? skeletonPath = null;
        OzzSkeleton? ozzSkeleton = null;
        List<string> animationPaths = [];

        if (desc.ExportSkeleton || desc.ExportAnimations)
        {
            var ozzResult = ExportOzz(desc, gltfBytes, Path.GetDirectoryName(desc.SourcePath) ?? "");
            if (ozzResult.Success)
            {
                skeletonPath = ozzResult.SkeletonPath;
                ozzSkeleton = ozzResult.OzzSkeleton;
                animationPaths = ozzResult.AnimationPaths.ToList();
            }
            else if (desc.ExternalSkeleton != null)
            {
                return ozzResult;
            }
        }

        return AssetExportResult.Succeeded(meshPaths, skeletonPath, animationPaths, ozzSkeleton);
    }

    private List<string> ExportMeshes(AssetExportDesc desc, byte[] gltfBytes)
    {
        var gltfModel = GltfModel.LoadMeshesOnly(gltfBytes, desc.AssetName, new GltfLoadOptions { SkipFallbackTangents = true });
        var meshPaths = new List<string>();

        for (var i = 0; i < gltfModel.Meshes.Count; i++)
        {
            var mesh = gltfModel.Meshes[i];
            if (mesh.SourceAttributes == null)
            {
                continue;
            }

            var meshFileName = gltfModel.Meshes.Count == 1
                ? $"{desc.AssetName}.nizimesh"
                : $"{mesh.Name}.nizimesh";

            var meshPath = Path.Combine(desc.OutputDirectory, meshFileName);

            using var stream = File.Create(meshPath);
            NiziMeshWriter.Write(stream, mesh);

            meshPaths.Add(meshPath);
        }

        return meshPaths;
    }

    private GltfExportResult ExportGltfToBytes(AssetExportDesc desc)
    {
        var gltfExportDesc = desc.ToGltfExportDesc();
        return _gltfExporter.ExportToBytes(in gltfExportDesc);
    }

    private AssetExportResult ExportOzz(AssetExportDesc desc, byte[] gltfBytes, string basePath)
    {
        using var ozzExporter = new OzzExporter();
        var ozzExportDesc = desc.ToOzzExportDesc(gltfBytes, basePath);

        if (!string.IsNullOrEmpty(desc.ReferenceSourcePath) && File.Exists(desc.ReferenceSourcePath))
        {
            var refGltfExportDesc = new GltfExportDesc
            {
                SourceFilePath = desc.ReferenceSourcePath,
                TargetDirectory = desc.OutputDirectory,
                AssetNamePrefix = "_ref_temp",
                OutputFormat = GltfExportFormat.Glb,
                ScaleFactor = desc.Scale,
                PreTransformVertices = false
            };
            var refResult = _gltfExporter.ExportToBytes(in refGltfExportDesc);
            if (refResult.Success && refResult.GltfBytes != null)
            {
                ozzExportDesc.ReferenceGltfData = ByteArrayView.Create(refResult.GltfBytes);
                ozzExportDesc.ReferenceGltfBasePath = StringView.Create(
                    Path.GetDirectoryName(desc.ReferenceSourcePath) ?? "");
            }
        }

        var result = ozzExporter.Export(in ozzExportDesc);

        try
        {
            if (result.ResultCode != OzzExportResultCode.Success)
            {
                var error = result.ErrorMessage.ToString();
                return AssetExportResult.Failed(string.IsNullOrEmpty(error) ? "Ozz export failed." : error);
            }

            string? skeletonPath = null;
            OzzSkeleton? ozzSkeleton = null;
            if (desc.ExternalSkeleton == null && result.SkeletonFilePath.NumChars > 0)
            {
                var ozzSkelPath = result.SkeletonFilePath.ToString();
                if (!File.Exists(ozzSkelPath))
                {
                    var dir = Path.GetDirectoryName(ozzSkelPath) ?? "";
                    var files = Directory.Exists(dir)
                        ? string.Join(", ", Directory.GetFiles(dir).Select(Path.GetFileName))
                        : "directory not found";
                    return AssetExportResult.Failed(
                        $"Ozz skeleton file not found: {ozzSkelPath} (files in dir: {files})");
                }

                skeletonPath = ozzSkelPath;
                var ozzSkelBytes = File.ReadAllBytes(ozzSkelPath);
                ozzSkeleton = OzzSkeleton.CreateFromBinaryContainer(
                    Skeleton.CreateBinaryContainer(ozzSkelBytes));
            }

            var animationPaths = new List<string>();
            var animArray = result.AnimationFilePaths;
            var animElements = animArray.ToArray();
            for (var i = 0u; i < animArray.NumElements; i++)
            {
                var ozzAnimPath = animElements[i].ToString();
                if (string.IsNullOrEmpty(ozzAnimPath) || !File.Exists(ozzAnimPath))
                {
                    continue;
                }

                animationPaths.Add(ozzAnimPath);
            }

            return AssetExportResult.Succeeded([], skeletonPath, animationPaths, ozzSkeleton);
        }
        finally
        {
            result.Destroy();
        }
    }
}
