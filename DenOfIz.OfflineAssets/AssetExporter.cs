using DenOfIz;

namespace OfflineAssets;

public sealed class AssetExporter : IDisposable
{
    private readonly GltfExporter _gltfExporter;
    private readonly OzzExporter _ozzExporter;
    private bool _disposed;

    public AssetExporter()
    {
        _gltfExporter = new GltfExporter();
        _ozzExporter = new OzzExporter();
    }

    public bool CanProcess(string filePath)
    {
        return _gltfExporter.ValidateFile(StringView.Create(filePath));
    }

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

    public AssetExportResult Export(AssetExportSettings settings)
    {
        if (string.IsNullOrEmpty(settings.SourcePath))
        {
            return AssetExportResult.Failed("Source path is required.");
        }

        if (!File.Exists(settings.SourcePath))
        {
            return AssetExportResult.Failed($"Source file not found: {settings.SourcePath}");
        }

        if (string.IsNullOrEmpty(settings.OutputDirectory))
        {
            return AssetExportResult.Failed("Output directory is required.");
        }

        if (string.IsNullOrEmpty(settings.AssetName))
        {
            settings.AssetName = Path.GetFileNameWithoutExtension(settings.SourcePath);
        }

        Directory.CreateDirectory(settings.OutputDirectory);

        var gltfResult = ExportGltf(settings);
        if (!gltfResult.Success || settings is { ExportSkeleton: false, ExportAnimations: false })
        {
            return gltfResult;
        }

        var ozzResult = ExportOzz(settings, gltfResult.OutputPath!);
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

    private AssetExportResult ExportGltf(AssetExportSettings settings)
    {
        var desc = settings.ToGltfExportDesc();
        var result = _gltfExporter.Export(ref desc);

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
            GltfExportResult.Free(ref result);
        }
    }

    private AssetExportResult ExportOzz(AssetExportSettings settings, string gltfOutputPath)
    {
        if (!_ozzExporter.ValidateGltf(StringView.Create(gltfOutputPath)))
        {
            return AssetExportResult.Failed("GLTF file is not valid for Ozz export.");
        }

        var desc = settings.ToOzzExportDesc(gltfOutputPath);
        var result = _ozzExporter.Export(ref desc);

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
            OzzExportResult.Free(ref result);
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
}
