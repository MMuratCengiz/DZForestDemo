namespace RuntimeAssets.GltfModels;

public sealed class GltfDocumentDesc
{
    public bool LoadExternalBuffers { get; init; } = true;
    public bool LoadExternalImages { get; init; } = true;
    public long MaxExternalFileSize { get; init; } = 256 * 1024 * 1024;

    public bool ConvertToLeftHanded { get; init; } = false;
    public bool ConvertToRowMajor { get; init; } = false;

    /// <summary>
    /// Custom logger for warnings and errors.
    /// </summary>
    public Action<GltfLogLevel, string>? Logger { get; init; }
}