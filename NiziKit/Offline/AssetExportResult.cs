namespace NiziKit.Offline;

public sealed class AssetExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }
    public string? SkeletonPath { get; init; }
    public IReadOnlyList<string> AnimationPaths { get; init; } = [];

    public static AssetExportResult Failed(string error)
    {
        return new AssetExportResult
        {
            Success = false,
            ErrorMessage = error
        };
    }

    public static AssetExportResult Succeeded(string outputPath, string? skeletonPath = null,
        IReadOnlyList<string>? animationPaths = null)
    {
        return new AssetExportResult
        {
            Success = true,
            OutputPath = outputPath,
            SkeletonPath = skeletonPath,
            AnimationPaths = animationPaths ?? []
        };
    }
}