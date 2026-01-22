using DenOfIz;

namespace NiziKit.Offline;

public sealed class AssetExporter : IDisposable
{
    private readonly GltfExporter _gltfExporter = new();
    private readonly OzzExporter _ozzExporter = new();

    public IReadOnlyList<string> SupportedExtensions => _gltfExporter.SupportedExtensions;

    public void Dispose()
    {
        _gltfExporter.Dispose();
        _ozzExporter.Dispose();
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

        var gltfResult = ExportGltf(desc);
        if (!gltfResult.Success || desc is { ExportSkeleton: false, ExportAnimations: false })
        {
            return gltfResult;
        }

        var ozzResult = ExportOzz(desc, gltfResult.OutputPath!);
        if (!ozzResult.Success)
        {
            return gltfResult;
        }

        return AssetExportResult.Succeeded(
            gltfResult.OutputPath!,
            ozzResult.SkeletonPath,
            ozzResult.AnimationPaths
        );
    }

    private AssetExportResult ExportGltf(AssetExportDesc desc)
    {
        var gltfExportDesc = desc.ToGltfExportDesc();
        var result = _gltfExporter.Export(in gltfExportDesc);

        if (result.Success)
        {
            return AssetExportResult.Succeeded(result.GltfFilePath!);
        }

        var error = result.ErrorMessage;
        return AssetExportResult.Failed(string.IsNullOrEmpty(error) ? "GLTF export failed." : error);
    }

    private AssetExportResult ExportOzz(AssetExportDesc desc, string gltfOutputPath)
    {
        if (!_ozzExporter.ValidateGltf(StringView.Create(gltfOutputPath)))
        {
            return AssetExportResult.Failed("GLTF file is not valid for Ozz export.");
        }

        var gltfExportDesc = desc.ToOzzExportDesc(gltfOutputPath);
        var result = _ozzExporter.Export(in gltfExportDesc);

        try
        {
            if (result.ResultCode != OzzExportResultCode.Success)
            {
                var error = result.ErrorMessage.ToString();
                return AssetExportResult.Failed(string.IsNullOrEmpty(error) ? "Ozz export failed." : error);
            }

            var skeletonPath = result.SkeletonFilePath.NumChars > 0
                ? result.SkeletonFilePath.ToString()
                : null;

            var animationPaths = new List<string>();
            var animArray = result.AnimationFilePaths;
            for (var i = 0u; i < animArray.NumElements; i++)
            {
                var path = animArray.ToArray()[i];
                if (path.NumChars > 0)
                {
                    animationPaths.Add(path.ToString());
                }
            }

            return AssetExportResult.Succeeded(gltfOutputPath, skeletonPath, animationPaths);
        }
        finally
        {
            result.Destroy();
        }
    }
}
