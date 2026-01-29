using DenOfIz;

namespace NiziKit.Offline;

public sealed class AssetExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }
    public IReadOnlyList<string> MeshPaths { get; init; } = [];
    public string? SkeletonPath { get; init; }
    public OzzSkeleton? OzzSkeleton { get; init; }
    public IReadOnlyList<string> AnimationPaths { get; init; } = [];

    public static AssetExportResult Failed(string error)
    {
        return new AssetExportResult
        {
            Success = false,
            ErrorMessage = error
        };
    }

    public static AssetExportResult Succeeded(IReadOnlyList<string> meshPaths, string? skeletonPath = null,
        IReadOnlyList<string>? animationPaths = null, OzzSkeleton? ozzSkeleton = null)
    {
        return new AssetExportResult
        {
            Success = true,
            MeshPaths = meshPaths,
            SkeletonPath = skeletonPath,
            OzzSkeleton = ozzSkeleton,
            AnimationPaths = animationPaths ?? []
        };
    }
}
