using System.Numerics;

namespace RuntimeAssets;

public sealed class ModelLoadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<RuntimeMeshHandle> MeshHandles { get; init; } = [];
    public IReadOnlyList<MaterialData> Materials { get; init; } = [];
    public IReadOnlyList<Matrix4x4> InverseBindMatrices { get; init; } = [];

    public static ModelLoadResult Failed(string error)
    {
        return new ModelLoadResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}