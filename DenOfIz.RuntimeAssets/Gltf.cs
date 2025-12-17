using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RuntimeAssets;

public static class Gltf
{
    private const uint GlbMagic = 0x46546C67; // "glTF"
    private const uint ChunkTypeJson = 0x4E4F534A; // "JSON"
    private const uint ChunkTypeBin = 0x004E4942; // "BIN\0"

    public static GltfDocument Load(string path, GltfLoadOptions? options = null)
    {
        options ??= GltfLoadOptions.Default;
        var document = new GltfDocument(path, options);

        try
        {
            if (!File.Exists(path))
            {
                document.AddError($"File not found: {path}");
                return document;
            }

            var bytes = File.ReadAllBytes(path);
            ParseDocument(document, bytes.AsSpan(), path);
        }
        catch (Exception ex)
        {
            document.AddError($"Failed to load GLTF: {ex.Message}");
        }

        return document;
    }

    public static GltfDocument Load(ReadOnlySpan<byte> data, string basePath = "", GltfLoadOptions? options = null)
    {
        options ??= GltfLoadOptions.Default;
        var document = new GltfDocument(basePath, options);

        try
        {
            ParseDocument(document, data, basePath);
        }
        catch (Exception ex)
        {
            document.AddError($"Failed to parse GLTF: {ex.Message}");
        }

        return document;
    }

    public static async Task<GltfDocument> LoadAsync(string path, GltfLoadOptions? options = null, CancellationToken ct = default)
    {
        options ??= GltfLoadOptions.Default;
        var document = new GltfDocument(path, options);

        try
        {
            if (!File.Exists(path))
            {
                document.AddError($"File not found: {path}");
                return document;
            }

            var bytes = await File.ReadAllBytesAsync(path, ct);
            ParseDocument(document, bytes.AsSpan(), path);
        }
        catch (Exception ex)
        {
            document.AddError($"Failed to load GLTF: {ex.Message}");
        }

        return document;
    }

    private static void ParseDocument(GltfDocument document, ReadOnlySpan<byte> data, string path)
    {
        if (data.Length < 4)
        {
            document.AddError("File too small to be valid GLTF");
            return;
        }

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (magic == GlbMagic)
        {
            ParseGlb(document, data);
        }
        else
        {
            ParseGltfJson(document, data);
        }
    }

    private static void ParseGlb(GltfDocument document, ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
        {
            document.AddError("Invalid GLB header");
            return;
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4));
        var length = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8));

        if (version != 2)
        {
            document.AddWarning($"Unsupported GLB version: {version}, attempting to parse anyway");
        }

        if (length > data.Length)
        {
            document.AddWarning($"GLB header claims {length} bytes but file only has {data.Length}");
            length = (uint)data.Length;
        }

        var offset = 12;
        ReadOnlySpan<byte> jsonChunk = default;
        ReadOnlySpan<byte> binChunk = default;

        while (offset + 8 <= data.Length)
        {
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset));
            var chunkType = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4));
            offset += 8;

            if (offset + chunkLength > data.Length)
            {
                document.AddWarning($"Chunk extends beyond file boundary, truncating");
                chunkLength = (uint)(data.Length - offset);
            }

            var chunkData = data.Slice(offset, (int)chunkLength);
            offset += (int)chunkLength;

            if (chunkType == ChunkTypeJson)
            {
                jsonChunk = chunkData;
            }
            else if (chunkType == ChunkTypeBin)
            {
                binChunk = chunkData;
            }
        }

        if (jsonChunk.IsEmpty)
        {
            document.AddError("GLB file has no JSON chunk");
            return;
        }

        ParseGltfJson(document, jsonChunk);

        if (!binChunk.IsEmpty)
        {
            document.SetEmbeddedBinaryBuffer(binChunk.ToArray());
        }
    }

    private static void ParseGltfJson(GltfDocument document, ReadOnlySpan<byte> jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var root = JsonSerializer.Deserialize<GltfRoot>(jsonData, options);
            if (root == null)
            {
                document.AddError("Failed to parse GLTF JSON");
                return;
            }

            document.SetRoot(root);
        }
        catch (JsonException ex)
        {
            document.AddError($"JSON parse error: {ex.Message}");
        }
    }
}

public sealed class GltfLoadOptions
{
    public static readonly GltfLoadOptions Default = new();

    /// <summary>
    /// Options that preserve GLTF's native right-handed column-major coordinate system.
    /// </summary>
    public static readonly GltfLoadOptions PreserveCoordinateSystem = new()
    {
        ConvertToLeftHanded = false,
        ConvertToRowMajor = false
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
    /// Default: true
    /// </summary>
    public bool ConvertToLeftHanded { get; init; } = true;

    /// <summary>
    /// Convert matrices from GLTF's column-major order to row-major order.
    /// Default: true
    /// </summary>
    public bool ConvertToRowMajor { get; init; } = true;

    /// <summary>
    /// Custom logger for warnings and errors.
    /// </summary>
    public Action<GltfLogLevel, string>? Logger { get; init; }
}

/// <summary>
/// Utilities for converting GLTF coordinate system to game engine conventions.
/// GLTF uses right-handed Y-up coordinate system with column-major matrices.
/// </summary>
public static class GltfCoordinateConversion
{
    /// <summary>
    /// Converts a position from right-handed to left-handed by negating Z.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ConvertPosition(Vector3 v) => new(v.X, v.Y, -v.Z);

    /// <summary>
    /// Converts a normal from right-handed to left-handed by negating Z.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ConvertNormal(Vector3 v) => new(v.X, v.Y, -v.Z);

    /// <summary>
    /// Converts a tangent from right-handed to left-handed.
    /// Negates Z component, preserves W (handedness indicator).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ConvertTangent(Vector4 v) => new(v.X, v.Y, -v.Z, -v.W);

    /// <summary>
    /// Converts a quaternion from right-handed to left-handed rotation.
    /// Negates X and Y components to flip the rotation axis.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ConvertQuaternion(Quaternion q) => new(-q.X, -q.Y, q.Z, q.W);

    /// <summary>
    /// Converts a quaternion stored as Vector4 from right-handed to left-handed.
    /// GLTF quaternions are stored as [x, y, z, w].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ConvertQuaternionVec4(Vector4 v) => new(-v.X, -v.Y, v.Z, v.W);

    /// <summary>
    /// Transposes a matrix from column-major to row-major order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 TransposeMatrix(Matrix4x4 m) => Matrix4x4.Transpose(m);

    /// <summary>
    /// Converts a matrix from right-handed to left-handed coordinate system.
    /// This reflects the matrix across the Z axis.
    /// </summary>
    public static Matrix4x4 ConvertMatrixHandedness(Matrix4x4 m)
    {
        // Negate the third column and third row elements that involve Z
        return new Matrix4x4(
            m.M11, m.M12, -m.M13, m.M14,
            m.M21, m.M22, -m.M23, m.M24,
            -m.M31, -m.M32, m.M33, -m.M34,
            m.M41, m.M42, -m.M43, m.M44
        );
    }

    /// <summary>
    /// Fully converts a matrix: handedness conversion + transpose (column to row major).
    /// </summary>
    public static Matrix4x4 ConvertMatrix(Matrix4x4 m, bool convertHandedness, bool transpose)
    {
        if (convertHandedness)
        {
            m = ConvertMatrixHandedness(m);
        }
        if (transpose)
        {
            m = Matrix4x4.Transpose(m);
        }
        return m;
    }

    /// <summary>
    /// Converts position array in place.
    /// </summary>
    public static void ConvertPositionsInPlace(Span<Vector3> positions)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = ConvertPosition(positions[i]);
        }
    }

    /// <summary>
    /// Converts normal array in place.
    /// </summary>
    public static void ConvertNormalsInPlace(Span<Vector3> normals)
    {
        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = ConvertNormal(normals[i]);
        }
    }

    /// <summary>
    /// Converts tangent array in place.
    /// </summary>
    public static void ConvertTangentsInPlace(Span<Vector4> tangents)
    {
        for (var i = 0; i < tangents.Length; i++)
        {
            tangents[i] = ConvertTangent(tangents[i]);
        }
    }

    /// <summary>
    /// Converts quaternion array (as Vector4) in place.
    /// </summary>
    public static void ConvertQuaternionsInPlace(Span<Vector4> quaternions)
    {
        for (var i = 0; i < quaternions.Length; i++)
        {
            quaternions[i] = ConvertQuaternionVec4(quaternions[i]);
        }
    }

    /// <summary>
    /// Converts matrix array in place.
    /// </summary>
    public static void ConvertMatricesInPlace(Span<Matrix4x4> matrices, bool convertHandedness, bool transpose)
    {
        for (var i = 0; i < matrices.Length; i++)
        {
            matrices[i] = ConvertMatrix(matrices[i], convertHandedness, transpose);
        }
    }

    /// <summary>
    /// Reverses triangle winding order for a left-handed coordinate system.
    /// For each triangle (i, i+1, i+2), swaps indices i+1 and i+2.
    /// </summary>
    public static void ReverseWindingOrder(Span<uint> indices)
    {
        for (var i = 0; i + 2 < indices.Length; i += 3)
        {
            (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
        }
    }
}

public enum GltfLogLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Represents a loaded GLTF document with all its data.
/// </summary>
public sealed class GltfDocument
{
    private readonly string _basePath;
    private readonly GltfLoadOptions _options;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly Dictionary<int, byte[]> _loadedBuffers = [];
    private readonly Dictionary<int, byte[]> _loadedImages = [];

    private GltfRoot? _root;
    private byte[]? _embeddedBinaryBuffer;

    internal GltfDocument(string basePath, GltfLoadOptions options)
    {
        _basePath = Path.GetDirectoryName(basePath) ?? "";
        _options = options;
    }

    public bool HasErrors => _errors.Count > 0;
    public bool HasWarnings => _warnings.Count > 0;
    public IReadOnlyList<string> Warnings => _warnings;
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// The load options used for this document.
    /// </summary>
    public GltfLoadOptions Options => _options;

    /// <summary>
    /// Whether coordinate conversion to left-handed is enabled.
    /// </summary>
    public bool ConvertToLeftHanded => _options.ConvertToLeftHanded;

    /// <summary>
    /// Whether matrix conversion to row-major is enabled.
    /// </summary>
    public bool ConvertToRowMajor => _options.ConvertToRowMajor;

    public GltfRoot? Root => _root;
    public GltfAsset? Asset => _root?.Asset;
    public IReadOnlyList<GltfScene> Scenes => _root?.Scenes ?? [];
    public IReadOnlyList<GltfNode> Nodes => _root?.Nodes ?? [];
    public IReadOnlyList<GltfMesh> Meshes => _root?.Meshes ?? [];
    public IReadOnlyList<GltfMaterial> Materials => _root?.Materials ?? [];
    public IReadOnlyList<GltfTexture> Textures => _root?.Textures ?? [];
    public IReadOnlyList<GltfImage> Images => _root?.Images ?? [];
    public IReadOnlyList<GltfSampler> Samplers => _root?.Samplers ?? [];
    public IReadOnlyList<GltfAnimation> Animations => _root?.Animations ?? [];
    public IReadOnlyList<GltfSkin> Skins => _root?.Skins ?? [];
    public IReadOnlyList<GltfBuffer> Buffers => _root?.Buffers ?? [];
    public IReadOnlyList<GltfBufferView> BufferViews => _root?.BufferViews ?? [];
    public IReadOnlyList<GltfAccessor> Accessors => _root?.Accessors ?? [];
    public int? DefaultScene => _root?.Scene;

    internal void SetRoot(GltfRoot root) => _root = root;
    internal void SetEmbeddedBinaryBuffer(byte[] data) => _embeddedBinaryBuffer = data;

    internal void AddWarning(string message)
    {
        _warnings.Add(message);
        _options.Logger?.Invoke(GltfLogLevel.Warning, message);
    }

    internal void AddError(string message)
    {
        _errors.Add(message);
        _options.Logger?.Invoke(GltfLogLevel.Error, message);
    }

    /// <summary>
    /// Gets the raw bytes for a buffer, loading external data if necessary.
    /// </summary>
    public ReadOnlySpan<byte> GetBufferData(int bufferIndex)
    {
        if (_root == null || bufferIndex < 0 || bufferIndex >= _root.Buffers.Count)
        {
            AddWarning($"Invalid buffer index: {bufferIndex}");
            return default;
        }

        if (_loadedBuffers.TryGetValue(bufferIndex, out var cached))
        {
            return cached;
        }

        var buffer = _root.Buffers[bufferIndex];

        // GLB embedded buffer (index 0, no URI)
        if (bufferIndex == 0 && string.IsNullOrEmpty(buffer.Uri) && _embeddedBinaryBuffer != null)
        {
            _loadedBuffers[bufferIndex] = _embeddedBinaryBuffer;
            return _embeddedBinaryBuffer;
        }

        // Data URI
        if (buffer.Uri != null && buffer.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var data = ParseDataUri(buffer.Uri);
            if (data != null)
            {
                _loadedBuffers[bufferIndex] = data;
                return data;
            }
            AddWarning($"Failed to parse data URI for buffer {bufferIndex}");
            return default;
        }

        // External file
        if (!string.IsNullOrEmpty(buffer.Uri) && _options.LoadExternalBuffers)
        {
            var data = LoadExternalFile(buffer.Uri);
            if (data != null)
            {
                _loadedBuffers[bufferIndex] = data;
                return data;
            }
        }

        AddWarning($"Could not load buffer {bufferIndex}: {buffer.Uri ?? "(embedded)"}");
        return default;
    }

    /// <summary>
    /// Gets the raw bytes for a buffer view.
    /// </summary>
    public ReadOnlySpan<byte> GetBufferViewData(int bufferViewIndex)
    {
        if (_root == null || bufferViewIndex < 0 || bufferViewIndex >= _root.BufferViews.Count)
        {
            AddWarning($"Invalid buffer view index: {bufferViewIndex}");
            return default;
        }

        var view = _root.BufferViews[bufferViewIndex];
        var bufferData = GetBufferData(view.Buffer);

        if (bufferData.IsEmpty)
        {
            return default;
        }

        var offset = view.ByteOffset;
        var length = view.ByteLength;

        if (offset + length > bufferData.Length)
        {
            AddWarning($"Buffer view {bufferViewIndex} extends beyond buffer boundary");
            return default;
        }

        return bufferData.Slice(offset, length);
    }

    /// <summary>
    /// Gets image data, loading external files if necessary.
    /// </summary>
    public ReadOnlySpan<byte> GetImageData(int imageIndex)
    {
        if (_root == null || imageIndex < 0 || imageIndex >= _root.Images.Count)
        {
            AddWarning($"Invalid image index: {imageIndex}");
            return default;
        }

        if (_loadedImages.TryGetValue(imageIndex, out var cached))
        {
            return cached;
        }

        var image = _root.Images[imageIndex];

        // Image stored in buffer view
        if (image.BufferView.HasValue)
        {
            var data = GetBufferViewData(image.BufferView.Value);
            if (!data.IsEmpty)
            {
                var copy = data.ToArray();
                _loadedImages[imageIndex] = copy;
                return copy;
            }
            return default;
        }

        // Data URI
        if (image.Uri != null && image.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var data = ParseDataUri(image.Uri);
            if (data != null)
            {
                _loadedImages[imageIndex] = data;
                return data;
            }
            AddWarning($"Failed to parse data URI for image {imageIndex}");
            return default;
        }

        // External file
        if (!string.IsNullOrEmpty(image.Uri) && _options.LoadExternalImages)
        {
            var data = LoadExternalFile(image.Uri);
            if (data != null)
            {
                _loadedImages[imageIndex] = data;
                return data;
            }
        }

        AddWarning($"Could not load image {imageIndex}: {image.Uri ?? "(embedded)"}");
        return default;
    }

    /// <summary>
    /// Gets the resolved file path for an image (for external loading).
    /// Returns null if the image is embedded or has an invalid URI.
    /// </summary>
    public string? GetImageFilePath(int imageIndex)
    {
        if (_root == null || imageIndex < 0 || imageIndex >= _root.Images.Count)
        {
            return null;
        }

        var image = _root.Images[imageIndex];

        if (image.BufferView.HasValue)
        {
            return null; // Embedded in buffer
        }

        if (string.IsNullOrEmpty(image.Uri))
        {
            return null;
        }

        if (image.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Data URI
        }

        if (IsAbsoluteUri(image.Uri))
        {
            AddWarning($"Skipping absolute/external URI for image {imageIndex}: {image.Uri}");
            return null;
        }

        var resolvedPath = Path.Combine(_basePath, Uri.UnescapeDataString(image.Uri));
        return Path.GetFullPath(resolvedPath);
    }

    /// <summary>
    /// Reads accessor data as a typed array.
    /// </summary>
    public T[] ReadAccessor<T>(int accessorIndex) where T : unmanaged
    {
        if (_root == null || accessorIndex < 0 || accessorIndex >= _root.Accessors.Count)
        {
            AddWarning($"Invalid accessor index: {accessorIndex}");
            return [];
        }

        var accessor = _root.Accessors[accessorIndex];
        var count = accessor.Count;

        if (count == 0)
        {
            return [];
        }

        // Handle sparse accessors and accessors without buffer views
        if (!accessor.BufferView.HasValue)
        {
            // Accessor might have sparse data or just zeros
            if (accessor.Sparse != null)
            {
                return ReadSparseAccessor<T>(accessor);
            }
            // Return zeros
            return new T[count];
        }

        var view = _root.BufferViews[accessor.BufferView.Value];
        var bufferData = GetBufferData(view.Buffer);

        if (bufferData.IsEmpty)
        {
            return new T[count];
        }

        var componentSize = GetComponentSize(accessor.ComponentType);
        var componentCount = GetTypeComponentCount(accessor.Type);
        var elementSize = componentSize * componentCount;
        var stride = view.ByteStride > 0 ? view.ByteStride : elementSize;

        var offset = view.ByteOffset + accessor.ByteOffset;
        var result = new T[count];
        var resultSpan = MemoryMarshal.AsBytes(result.AsSpan());

        for (var i = 0; i < count; i++)
        {
            var srcOffset = offset + i * stride;
            if (srcOffset + elementSize > bufferData.Length)
            {
                AddWarning($"Accessor {accessorIndex} data extends beyond buffer at element {i}");
                break;
            }

            var srcSlice = bufferData.Slice(srcOffset, elementSize);
            var dstOffset = i * Unsafe.SizeOf<T>();
            var copySize = Math.Min(elementSize, Unsafe.SizeOf<T>());
            srcSlice.Slice(0, copySize).CopyTo(resultSpan.Slice(dstOffset, copySize));
        }

        // Apply sparse data if present
        if (accessor.Sparse != null)
        {
            ApplySparseData(result, accessor);
        }

        return result;
    }

    /// <summary>
    /// Reads index accessor data as uint array, handling different component types.
    /// </summary>
    public uint[] ReadIndices(int accessorIndex)
    {
        if (_root == null || accessorIndex < 0 || accessorIndex >= _root.Accessors.Count)
        {
            AddWarning($"Invalid accessor index: {accessorIndex}");
            return [];
        }

        var accessor = _root.Accessors[accessorIndex];

        return accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => ReadAccessor<byte>(accessorIndex).Select(b => (uint)b).ToArray(),
            GltfComponentType.UnsignedShort => ReadAccessor<ushort>(accessorIndex).Select(s => (uint)s).ToArray(),
            GltfComponentType.UnsignedInt => ReadAccessor<uint>(accessorIndex),
            _ => []
        };
    }

    /// <summary>
    /// Reads accessor data as Vector2 array (no coordinate conversion needed for UVs).
    /// </summary>
    public Vector2[] ReadAccessorVec2(int accessorIndex) => ReadAccessor<Vector2>(accessorIndex);

    /// <summary>
    /// Reads accessor data as Vector3 array (raw, no conversion).
    /// </summary>
    public Vector3[] ReadAccessorVec3(int accessorIndex) => ReadAccessor<Vector3>(accessorIndex);

    /// <summary>
    /// Reads accessor data as Vector4 array (raw, no conversion).
    /// </summary>
    public Vector4[] ReadAccessorVec4(int accessorIndex) => ReadAccessor<Vector4>(accessorIndex);

    /// <summary>
    /// Reads accessor data as Matrix4x4 array (raw, no conversion).
    /// </summary>
    public Matrix4x4[] ReadAccessorMat4(int accessorIndex) => ReadAccessor<Matrix4x4>(accessorIndex);

    /// <summary>
    /// Reads accessor data as float array.
    /// </summary>
    public float[] ReadAccessorFloat(int accessorIndex) => ReadAccessor<float>(accessorIndex);

    /// <summary>
    /// Reads position data with coordinate system conversion applied.
    /// </summary>
    public Vector3[] ReadPositions(int accessorIndex)
    {
        var positions = ReadAccessor<Vector3>(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ConvertPositionsInPlace(positions);
        }
        return positions;
    }

    /// <summary>
    /// Reads normal data with coordinate system conversion applied.
    /// </summary>
    public Vector3[] ReadNormals(int accessorIndex)
    {
        var normals = ReadAccessor<Vector3>(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ConvertNormalsInPlace(normals);
        }
        return normals;
    }

    /// <summary>
    /// Reads tangent data with coordinate system conversion applied.
    /// </summary>
    public Vector4[] ReadTangents(int accessorIndex)
    {
        var tangents = ReadAccessor<Vector4>(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ConvertTangentsInPlace(tangents);
        }
        return tangents;
    }

    /// <summary>
    /// Reads quaternion rotation data with coordinate system conversion applied.
    /// </summary>
    public Vector4[] ReadRotations(int accessorIndex)
    {
        var rotations = ReadAccessor<Vector4>(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ConvertQuaternionsInPlace(rotations);
        }
        return rotations;
    }

    /// <summary>
    /// Reads translation data with coordinate system conversion applied.
    /// </summary>
    public Vector3[] ReadTranslations(int accessorIndex)
    {
        var translations = ReadAccessor<Vector3>(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ConvertPositionsInPlace(translations);
        }
        return translations;
    }

    /// <summary>
    /// Reads matrix data with coordinate system and row/column major conversion applied.
    /// </summary>
    public Matrix4x4[] ReadMatrices(int accessorIndex)
    {
        var matrices = ReadAccessor<Matrix4x4>(accessorIndex);
        if (_options.ConvertToLeftHanded || _options.ConvertToRowMajor)
        {
            GltfCoordinateConversion.ConvertMatricesInPlace(matrices, _options.ConvertToLeftHanded, _options.ConvertToRowMajor);
        }
        return matrices;
    }

    /// <summary>
    /// Reads indices with optional winding order reversal for left-handed conversion.
    /// </summary>
    public uint[] ReadIndicesConverted(int accessorIndex)
    {
        var indices = ReadIndices(accessorIndex);
        if (_options.ConvertToLeftHanded)
        {
            GltfCoordinateConversion.ReverseWindingOrder(indices);
        }
        return indices;
    }

    /// <summary>
    /// Computes the world transform matrix for a node with coordinate conversion applied.
    /// </summary>
    public Matrix4x4 GetNodeWorldTransform(int nodeIndex)
    {
        if (_root == null || nodeIndex < 0 || nodeIndex >= _root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var transform = GetNodeLocalTransform(nodeIndex);

        // Find parent and accumulate transforms
        for (var i = 0; i < _root.Nodes.Count; i++)
        {
            var potentialParent = _root.Nodes[i];
            if (potentialParent.Children?.Contains(nodeIndex) == true)
            {
                transform = transform * GetNodeWorldTransform(i);
                break;
            }
        }

        return transform;
    }

    /// <summary>
    /// Gets the local transform matrix for a node with coordinate conversion applied.
    /// </summary>
    public Matrix4x4 GetNodeLocalTransform(int nodeIndex)
    {
        if (_root == null || nodeIndex < 0 || nodeIndex >= _root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var node = _root.Nodes[nodeIndex];
        Matrix4x4 transform;

        if (node.Matrix != null && node.Matrix.Length == 16)
        {
            transform = new Matrix4x4(
                node.Matrix[0], node.Matrix[1], node.Matrix[2], node.Matrix[3],
                node.Matrix[4], node.Matrix[5], node.Matrix[6], node.Matrix[7],
                node.Matrix[8], node.Matrix[9], node.Matrix[10], node.Matrix[11],
                node.Matrix[12], node.Matrix[13], node.Matrix[14], node.Matrix[15]
            );
        }
        else
        {
            var t = node.Translation ?? [0, 0, 0];
            var r = node.Rotation ?? [0, 0, 0, 1];
            var s = node.Scale ?? [1, 1, 1];

            // Apply left-handed conversion to TRS components
            Vector3 translation;
            Quaternion rotation;

            if (_options.ConvertToLeftHanded)
            {
                translation = GltfCoordinateConversion.ConvertPosition(new Vector3(t[0], t[1], t[2]));
                rotation = GltfCoordinateConversion.ConvertQuaternion(new Quaternion(r[0], r[1], r[2], r[3]));
            }
            else
            {
                translation = new Vector3(t[0], t[1], t[2]);
                rotation = new Quaternion(r[0], r[1], r[2], r[3]);
            }

            var scale = new Vector3(s[0], s[1], s[2]);

            var translationMat = Matrix4x4.CreateTranslation(translation);
            var rotationMat = Matrix4x4.CreateFromQuaternion(rotation);
            var scaleMat = Matrix4x4.CreateScale(scale);

            transform = scaleMat * rotationMat * translationMat;

            // For TRS, we've already converted components, so only transpose if needed
            if (_options.ConvertToRowMajor)
            {
                transform = Matrix4x4.Transpose(transform);
            }

            return transform;
        }

        // For explicit matrix, apply full conversion
        return GltfCoordinateConversion.ConvertMatrix(transform, _options.ConvertToLeftHanded, _options.ConvertToRowMajor);
    }

    /// <summary>
    /// Gets the raw local transform matrix for a node without any coordinate conversion.
    /// </summary>
    public Matrix4x4 GetNodeLocalTransformRaw(int nodeIndex)
    {
        if (_root == null || nodeIndex < 0 || nodeIndex >= _root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var node = _root.Nodes[nodeIndex];

        if (node.Matrix != null && node.Matrix.Length == 16)
        {
            return new Matrix4x4(
                node.Matrix[0], node.Matrix[1], node.Matrix[2], node.Matrix[3],
                node.Matrix[4], node.Matrix[5], node.Matrix[6], node.Matrix[7],
                node.Matrix[8], node.Matrix[9], node.Matrix[10], node.Matrix[11],
                node.Matrix[12], node.Matrix[13], node.Matrix[14], node.Matrix[15]
            );
        }

        var t = node.Translation ?? [0, 0, 0];
        var r = node.Rotation ?? [0, 0, 0, 1];
        var s = node.Scale ?? [1, 1, 1];

        var translation = Matrix4x4.CreateTranslation(t[0], t[1], t[2]);
        var rotation = Matrix4x4.CreateFromQuaternion(new Quaternion(r[0], r[1], r[2], r[3]));
        var scale = Matrix4x4.CreateScale(s[0], s[1], s[2]);

        return scale * rotation * translation;
    }

    private T[] ReadSparseAccessor<T>(GltfAccessor accessor) where T : unmanaged
    {
        var result = new T[accessor.Count];
        ApplySparseData(result, accessor);
        return result;
    }

    private void ApplySparseData<T>(T[] data, GltfAccessor accessor) where T : unmanaged
    {
        if (_root == null || accessor.Sparse == null)
        {
            return;
        }

        var sparse = accessor.Sparse;

        // Read indices
        var indicesView = _root.BufferViews[sparse.Indices.BufferView];
        var indicesBuffer = GetBufferData(indicesView.Buffer);
        var indicesOffset = indicesView.ByteOffset + sparse.Indices.ByteOffset;

        // Read values
        var valuesView = _root.BufferViews[sparse.Values.BufferView];
        var valuesBuffer = GetBufferData(valuesView.Buffer);
        var valuesOffset = valuesView.ByteOffset + sparse.Values.ByteOffset;

        var componentSize = GetComponentSize(accessor.ComponentType);
        var componentCount = GetTypeComponentCount(accessor.Type);
        var elementSize = componentSize * componentCount;

        for (var i = 0; i < sparse.Count; i++)
        {
            var indexOffset = indicesOffset + i * GetComponentSize(sparse.Indices.ComponentType);
            var index = sparse.Indices.ComponentType switch
            {
                GltfComponentType.UnsignedByte => indicesBuffer[indexOffset],
                GltfComponentType.UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(indicesBuffer.Slice(indexOffset)),
                GltfComponentType.UnsignedInt => (int)BinaryPrimitives.ReadUInt32LittleEndian(indicesBuffer.Slice(indexOffset)),
                _ => 0
            };

            if (index < data.Length)
            {
                var valueOffset = valuesOffset + i * elementSize;
                var valueSlice = valuesBuffer.Slice(valueOffset, Math.Min(elementSize, Unsafe.SizeOf<T>()));
                data[index] = MemoryMarshal.Read<T>(valueSlice);
            }
        }
    }

    private byte[]? LoadExternalFile(string uri)
    {
        if (IsAbsoluteUri(uri))
        {
            AddWarning($"Skipping absolute/external URI: {uri}");
            return null;
        }

        try
        {
            var resolvedPath = Path.Combine(_basePath, Uri.UnescapeDataString(uri));
            var fullPath = Path.GetFullPath(resolvedPath);

            if (!File.Exists(fullPath))
            {
                AddWarning($"External file not found: {fullPath}");
                return null;
            }

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > _options.MaxExternalFileSize)
            {
                AddWarning($"External file too large ({fileInfo.Length} bytes): {fullPath}");
                return null;
            }

            return File.ReadAllBytes(fullPath);
        }
        catch (Exception ex)
        {
            AddWarning($"Failed to load external file '{uri}': {ex.Message}");
            return null;
        }
    }

    private static byte[]? ParseDataUri(string uri)
    {
        try
        {
            var commaIndex = uri.IndexOf(',');
            if (commaIndex < 0)
            {
                return null;
            }

            var data = uri.AsSpan(commaIndex + 1);
            var header = uri.AsSpan(5, commaIndex - 5); // Skip "data:"

            if (header.Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.FromBase64String(data.ToString());
            }

            // URL-encoded data
            return Encoding.UTF8.GetBytes(Uri.UnescapeDataString(data.ToString()));
        }
        catch
        {
            return null;
        }
    }

    private static bool IsAbsoluteUri(string uri)
    {
        return uri.Contains("://") ||
               uri.StartsWith("//", StringComparison.Ordinal) ||
               (uri.Length > 2 && uri[1] == ':'); // Windows absolute path
    }

    private static int GetComponentSize(GltfComponentType type)
    {
        return type switch
        {
            GltfComponentType.Byte => 1,
            GltfComponentType.UnsignedByte => 1,
            GltfComponentType.Short => 2,
            GltfComponentType.UnsignedShort => 2,
            GltfComponentType.UnsignedInt => 4,
            GltfComponentType.Float => 4,
            _ => 4
        };
    }

    private static int GetTypeComponentCount(string type)
    {
        return type.ToUpperInvariant() switch
        {
            "SCALAR" => 1,
            "VEC2" => 2,
            "VEC3" => 3,
            "VEC4" => 4,
            "MAT2" => 4,
            "MAT3" => 9,
            "MAT4" => 16,
            _ => 1
        };
    }
}

#region JSON Schema Types

/// <summary>
/// Root object of a GLTF file.
/// </summary>
public sealed class GltfRoot
{
    [JsonPropertyName("asset")]
    public GltfAsset? Asset { get; set; }

    [JsonPropertyName("scene")]
    public int? Scene { get; set; }

    [JsonPropertyName("scenes")]
    public List<GltfScene> Scenes { get; set; } = [];

    [JsonPropertyName("nodes")]
    public List<GltfNode> Nodes { get; set; } = [];

    [JsonPropertyName("meshes")]
    public List<GltfMesh> Meshes { get; set; } = [];

    [JsonPropertyName("materials")]
    public List<GltfMaterial> Materials { get; set; } = [];

    [JsonPropertyName("textures")]
    public List<GltfTexture> Textures { get; set; } = [];

    [JsonPropertyName("images")]
    public List<GltfImage> Images { get; set; } = [];

    [JsonPropertyName("samplers")]
    public List<GltfSampler> Samplers { get; set; } = [];

    [JsonPropertyName("buffers")]
    public List<GltfBuffer> Buffers { get; set; } = [];

    [JsonPropertyName("bufferViews")]
    public List<GltfBufferView> BufferViews { get; set; } = [];

    [JsonPropertyName("accessors")]
    public List<GltfAccessor> Accessors { get; set; } = [];

    [JsonPropertyName("animations")]
    public List<GltfAnimation> Animations { get; set; } = [];

    [JsonPropertyName("skins")]
    public List<GltfSkin> Skins { get; set; } = [];

    [JsonPropertyName("cameras")]
    public List<GltfCamera> Cameras { get; set; } = [];

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }

    [JsonPropertyName("extensionsUsed")]
    public List<string>? ExtensionsUsed { get; set; }

    [JsonPropertyName("extensionsRequired")]
    public List<string>? ExtensionsRequired { get; set; }
}

public sealed class GltfAsset
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    [JsonPropertyName("generator")]
    public string? Generator { get; set; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("minVersion")]
    public string? MinVersion { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfScene
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("nodes")]
    public List<int>? Nodes { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfNode
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("children")]
    public List<int>? Children { get; set; }

    [JsonPropertyName("mesh")]
    public int? Mesh { get; set; }

    [JsonPropertyName("skin")]
    public int? Skin { get; set; }

    [JsonPropertyName("camera")]
    public int? Camera { get; set; }

    [JsonPropertyName("matrix")]
    public float[]? Matrix { get; set; }

    [JsonPropertyName("translation")]
    public float[]? Translation { get; set; }

    [JsonPropertyName("rotation")]
    public float[]? Rotation { get; set; }

    [JsonPropertyName("scale")]
    public float[]? Scale { get; set; }

    [JsonPropertyName("weights")]
    public float[]? Weights { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfMesh
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("primitives")]
    public List<GltfPrimitive> Primitives { get; set; } = [];

    [JsonPropertyName("weights")]
    public float[]? Weights { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfPrimitive
{
    [JsonPropertyName("attributes")]
    public Dictionary<string, int> Attributes { get; set; } = [];

    [JsonPropertyName("indices")]
    public int? Indices { get; set; }

    [JsonPropertyName("material")]
    public int? Material { get; set; }

    [JsonPropertyName("mode")]
    public GltfPrimitiveMode Mode { get; set; } = GltfPrimitiveMode.Triangles;

    [JsonPropertyName("targets")]
    public List<Dictionary<string, int>>? Targets { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

[JsonConverter(typeof(JsonNumberEnumConverter<GltfPrimitiveMode>))]
public enum GltfPrimitiveMode
{
    Points = 0,
    Lines = 1,
    LineLoop = 2,
    LineStrip = 3,
    Triangles = 4,
    TriangleStrip = 5,
    TriangleFan = 6
}

public sealed class GltfMaterial
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("pbrMetallicRoughness")]
    public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; set; }

    [JsonPropertyName("normalTexture")]
    public GltfNormalTextureInfo? NormalTexture { get; set; }

    [JsonPropertyName("occlusionTexture")]
    public GltfOcclusionTextureInfo? OcclusionTexture { get; set; }

    [JsonPropertyName("emissiveTexture")]
    public GltfTextureInfo? EmissiveTexture { get; set; }

    [JsonPropertyName("emissiveFactor")]
    public float[]? EmissiveFactor { get; set; }

    [JsonPropertyName("alphaMode")]
    public string? AlphaMode { get; set; }

    [JsonPropertyName("alphaCutoff")]
    public float? AlphaCutoff { get; set; }

    [JsonPropertyName("doubleSided")]
    public bool DoubleSided { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfPbrMetallicRoughness
{
    [JsonPropertyName("baseColorFactor")]
    public float[]? BaseColorFactor { get; set; }

    [JsonPropertyName("baseColorTexture")]
    public GltfTextureInfo? BaseColorTexture { get; set; }

    [JsonPropertyName("metallicFactor")]
    public float MetallicFactor { get; set; } = 1.0f;

    [JsonPropertyName("roughnessFactor")]
    public float RoughnessFactor { get; set; } = 1.0f;

    [JsonPropertyName("metallicRoughnessTexture")]
    public GltfTextureInfo? MetallicRoughnessTexture { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public class GltfTextureInfo
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("texCoord")]
    public int TexCoord { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfNormalTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1.0f;
}

public sealed class GltfOcclusionTextureInfo : GltfTextureInfo
{
    [JsonPropertyName("strength")]
    public float Strength { get; set; } = 1.0f;
}

public sealed class GltfTexture
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("sampler")]
    public int? Sampler { get; set; }

    [JsonPropertyName("source")]
    public int? Source { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfImage
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfSampler
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("magFilter")]
    public int? MagFilter { get; set; }

    [JsonPropertyName("minFilter")]
    public int? MinFilter { get; set; }

    [JsonPropertyName("wrapS")]
    public int WrapS { get; set; } = 10497; // REPEAT

    [JsonPropertyName("wrapT")]
    public int WrapT { get; set; } = 10497; // REPEAT

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfBuffer
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfBufferView
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("buffer")]
    public int Buffer { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("byteLength")]
    public int ByteLength { get; set; }

    [JsonPropertyName("byteStride")]
    public int ByteStride { get; set; }

    [JsonPropertyName("target")]
    public int? Target { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfAccessor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("componentType")]
    public GltfComponentType ComponentType { get; set; }

    [JsonPropertyName("normalized")]
    public bool Normalized { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SCALAR";

    [JsonPropertyName("max")]
    public float[]? Max { get; set; }

    [JsonPropertyName("min")]
    public float[]? Min { get; set; }

    [JsonPropertyName("sparse")]
    public GltfSparse? Sparse { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

[JsonConverter(typeof(JsonNumberEnumConverter<GltfComponentType>))]
public enum GltfComponentType
{
    Byte = 5120,
    UnsignedByte = 5121,
    Short = 5122,
    UnsignedShort = 5123,
    UnsignedInt = 5125,
    Float = 5126
}

public sealed class GltfSparse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("indices")]
    public GltfSparseIndices Indices { get; set; } = new();

    [JsonPropertyName("values")]
    public GltfSparseValues Values { get; set; } = new();

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfSparseIndices
{
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("componentType")]
    public GltfComponentType ComponentType { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfSparseValues
{
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfAnimation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("channels")]
    public List<GltfAnimationChannel> Channels { get; set; } = [];

    [JsonPropertyName("samplers")]
    public List<GltfAnimationSampler> Samplers { get; set; } = [];

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfAnimationChannel
{
    [JsonPropertyName("sampler")]
    public int Sampler { get; set; }

    [JsonPropertyName("target")]
    public GltfAnimationTarget Target { get; set; } = new();

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfAnimationTarget
{
    [JsonPropertyName("node")]
    public int? Node { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfAnimationSampler
{
    [JsonPropertyName("input")]
    public int Input { get; set; }

    [JsonPropertyName("output")]
    public int Output { get; set; }

    [JsonPropertyName("interpolation")]
    public string Interpolation { get; set; } = "LINEAR";

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfSkin
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("inverseBindMatrices")]
    public int? InverseBindMatrices { get; set; }

    [JsonPropertyName("skeleton")]
    public int? Skeleton { get; set; }

    [JsonPropertyName("joints")]
    public List<int> Joints { get; set; } = [];

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfCamera
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "perspective";

    [JsonPropertyName("perspective")]
    public GltfCameraPerspective? Perspective { get; set; }

    [JsonPropertyName("orthographic")]
    public GltfCameraOrthographic? Orthographic { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfCameraPerspective
{
    [JsonPropertyName("aspectRatio")]
    public float? AspectRatio { get; set; }

    [JsonPropertyName("yfov")]
    public float Yfov { get; set; }

    [JsonPropertyName("zfar")]
    public float? Zfar { get; set; }

    [JsonPropertyName("znear")]
    public float Znear { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

public sealed class GltfCameraOrthographic
{
    [JsonPropertyName("xmag")]
    public float Xmag { get; set; }

    [JsonPropertyName("ymag")]
    public float Ymag { get; set; }

    [JsonPropertyName("zfar")]
    public float Zfar { get; set; }

    [JsonPropertyName("znear")]
    public float Znear { get; set; }

    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    [JsonPropertyName("extras")]
    public JsonElement? Extras { get; set; }
}

#endregion

#region JSON Converters

internal sealed class JsonNumberEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return Unsafe.As<int, T>(ref value);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (Enum.TryParse<T>(str, true, out var result))
            {
                return result;
            }
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var intValue = Unsafe.As<T, int>(ref value);
        writer.WriteNumberValue(intValue);
    }
}

#endregion
