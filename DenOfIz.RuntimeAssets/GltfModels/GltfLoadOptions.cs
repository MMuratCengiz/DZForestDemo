namespace RuntimeAssets.GltfModels;

public sealed class GltfLoadOptions
{
    /// <summary>
    /// Default options: preserve GLTF's native right-handed column-major coordinate system.
    /// Higher-level loaders (like GltfLoader) should handle conversions explicitly.
    /// </summary>
    public static readonly GltfLoadOptions Default = new()
    {
        ConvertToLeftHanded = false,
        ConvertToRowMajor = false
    };

    /// <summary>
    /// Options for DirectX/DenOfIz: left-handed Y-up with row-major matrices.
    /// </summary>
    public static readonly GltfLoadOptions DirectX = new()
    {
        ConvertToLeftHanded = true,
        ConvertToRowMajor = true
    };

    /// <summary>
    /// Whether to load external buffers (.bin files).
    /// </summary>
    public bool LoadExternalBuffers { get; init; } = true;

    /// <summary>
    /// Whether to load external images.
    /// </summary>
    public bool LoadExternalImages { get; init; } = true;

    /// <summary>
    /// Maximum file size to load for external resources (default 256MB).
    /// </summary>
    public long MaxExternalFileSize { get; init; } = 256 * 1024 * 1024;

    /// <summary>
    /// Whether to skip loading mesh data.
    /// </summary>
    public bool SkipMeshes { get; init; }

    /// <summary>
    /// Whether to skip loading animation data.
    /// </summary>
    public bool SkipAnimations { get; init; }

    /// <summary>
    /// Whether to skip loading skin data.
    /// </summary>
    public bool SkipSkins { get; init; }

    /// <summary>
    /// Convert from GLTF's right-handed coordinate system to left-handed.
    /// This negates the Z axis for positions, normals, and translations,
    /// and adjusts quaternion rotations accordingly.
    /// Default: false (preserves GLTF's native coordinate system)
    /// </summary>
    public bool ConvertToLeftHanded { get; init; } = false;

    /// <summary>
    /// Convert matrices from GLTF's column-major order to row-major order.
    /// Default: false (preserves GLTF's native column-major matrices)
    /// </summary>
    public bool ConvertToRowMajor { get; init; } = false;

    /// <summary>
    /// Custom logger for warnings and errors.
    /// </summary>
    public Action<GltfLogLevel, string>? Logger { get; init; }
}