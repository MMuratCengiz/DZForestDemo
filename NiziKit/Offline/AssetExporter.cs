using DenOfIz;

namespace NiziKit.Offline;

public sealed class AssetExporter : IDisposable
{
    private readonly GltfExporter _gltfExporter = new();
    private readonly OzzExporter _ozzExporter = new();
    private bool _disposed;

    public IReadOnlyList<string> SupportedExtensions
    {
        get
        {
            var extensions = _gltfExporter.GetSupportedExtensions();
            var result = new List<string>((int)extensions.NumElements);
            for (var i = 0u; i < extensions.NumElements; i++)
            {
                var ext = extensions.ToArray()[i];
                result.Add(ext.ToString());
            }

            return result;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _gltfExporter.Dispose();
        _ozzExporter.Dispose();
    }

    public bool CanProcess(string filePath)
    {
        return _gltfExporter.ValidateFile(StringView.Create(filePath));
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
            return AssetExportResult.Failed($"GLTF exported but Ozz export failed: {ozzResult.ErrorMessage}");
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

        try
        {
            if (result.ResultCode == GltfExportResultCode.Success)
            {
                return AssetExportResult.Succeeded(result.GltfFilePath.ToString());
            }

            var error = result.ErrorMessage.ToString();
            return AssetExportResult.Failed(string.IsNullOrEmpty(error) ? "GLTF export failed." : error);
        }
        finally
        {
            result.Destroy();
        }
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