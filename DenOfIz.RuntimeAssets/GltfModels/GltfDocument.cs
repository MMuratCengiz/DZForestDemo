using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RuntimeAssets.GltfModels;

/// <summary>
/// Represents a loaded GLTF document with all its data.
/// </summary>
public sealed class GltfDocument
{
    private readonly string _basePath;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly Dictionary<int, byte[]> _loadedBuffers = [];
    private readonly Dictionary<int, byte[]> _loadedImages = [];

    private byte[]? _embeddedBinaryBuffer;

    internal GltfDocument(string basePath, GltfLoadOptions options)
    {
        _basePath = Path.GetDirectoryName(basePath) ?? "";
        Options = options;
    }

    public bool HasErrors => _errors.Count > 0;
    public bool HasWarnings => _warnings.Count > 0;
    public IReadOnlyList<string> Warnings => _warnings;
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// The load options used for this document.
    /// </summary>
    public GltfLoadOptions Options { get; }

    /// <summary>
    /// Whether coordinate conversion to left-handed is enabled.
    /// </summary>
    public bool ConvertToLeftHanded => Options.ConvertToLeftHanded;

    /// <summary>
    /// Whether matrix conversion to row-major is enabled.
    /// </summary>
    public bool ConvertToRowMajor => Options.ConvertToRowMajor;

    public GltfRoot? Root { get; private set; }

    public GltfAsset? Asset => Root?.Asset;
    public IReadOnlyList<GltfScene> Scenes => Root?.Scenes ?? [];
    public IReadOnlyList<GltfNode> Nodes => Root?.Nodes ?? [];
    public IReadOnlyList<GltfMesh> Meshes => Root?.Meshes ?? [];
    public IReadOnlyList<GltfMaterial> Materials => Root?.Materials ?? [];
    public IReadOnlyList<GltfTexture> Textures => Root?.Textures ?? [];
    public IReadOnlyList<GltfImage> Images => Root?.Images ?? [];
    public IReadOnlyList<GltfSampler> Samplers => Root?.Samplers ?? [];
    public IReadOnlyList<GltfAnimation> Animations => Root?.Animations ?? [];
    public IReadOnlyList<GltfSkin> Skins => Root?.Skins ?? [];
    public IReadOnlyList<GltfBuffer> Buffers => Root?.Buffers ?? [];
    public IReadOnlyList<GltfBufferView> BufferViews => Root?.BufferViews ?? [];
    public IReadOnlyList<GltfAccessor> Accessors => Root?.Accessors ?? [];
    public int? DefaultScene => Root?.Scene;

    internal void SetRoot(GltfRoot root) => Root = root;
    internal void SetEmbeddedBinaryBuffer(byte[] data) => _embeddedBinaryBuffer = data;

    internal void AddWarning(string message)
    {
        _warnings.Add(message);
        Options.Logger?.Invoke(GltfLogLevel.Warning, message);
    }

    internal void AddError(string message)
    {
        _errors.Add(message);
        Options.Logger?.Invoke(GltfLogLevel.Error, message);
    }

    /// <summary>
    /// Gets the raw bytes for a buffer, loading external data if necessary.
    /// </summary>
    public ReadOnlySpan<byte> GetBufferData(int bufferIndex)
    {
        if (Root == null || bufferIndex < 0 || bufferIndex >= Root.Buffers.Count)
        {
            AddWarning($"Invalid buffer index: {bufferIndex}");
            return default;
        }

        if (_loadedBuffers.TryGetValue(bufferIndex, out var cached))
        {
            return cached;
        }

        var buffer = Root.Buffers[bufferIndex];

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
        if (!string.IsNullOrEmpty(buffer.Uri) && Options.LoadExternalBuffers)
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
        if (Root == null || bufferViewIndex < 0 || bufferViewIndex >= Root.BufferViews.Count)
        {
            AddWarning($"Invalid buffer view index: {bufferViewIndex}");
            return default;
        }

        var view = Root.BufferViews[bufferViewIndex];
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
        if (Root == null || imageIndex < 0 || imageIndex >= Root.Images.Count)
        {
            AddWarning($"Invalid image index: {imageIndex}");
            return default;
        }

        if (_loadedImages.TryGetValue(imageIndex, out var cached))
        {
            return cached;
        }

        var image = Root.Images[imageIndex];

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
        if (!string.IsNullOrEmpty(image.Uri) && Options.LoadExternalImages)
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
        if (Root == null || imageIndex < 0 || imageIndex >= Root.Images.Count)
        {
            return null;
        }

        var image = Root.Images[imageIndex];

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
        if (Root == null || accessorIndex < 0 || accessorIndex >= Root.Accessors.Count)
        {
            AddWarning($"Invalid accessor index: {accessorIndex}");
            return [];
        }

        var accessor = Root.Accessors[accessorIndex];
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

        var view = Root.BufferViews[accessor.BufferView.Value];
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
            srcSlice[..copySize].CopyTo(resultSpan.Slice(dstOffset, copySize));
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
        if (Root == null || accessorIndex < 0 || accessorIndex >= Root.Accessors.Count)
        {
            AddWarning($"Invalid accessor index: {accessorIndex}");
            return [];
        }

        var accessor = Root.Accessors[accessorIndex];

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
    /// Computes the world transform matrix for a node in glTF's native format (column-major, right-handed).
    /// Note: Matrix4x4 is row-major in memory, so glTF column-major data appears transposed.
    /// </summary>
    public Matrix4x4 GetNodeWorldTransform(int nodeIndex)
    {
        if (Root == null || nodeIndex < 0 || nodeIndex >= Root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var transform = GetNodeLocalTransform(nodeIndex);

        // Find parent and accumulate transforms
        for (var i = 0; i < Root.Nodes.Count; i++)
        {
            var potentialParent = Root.Nodes[i];
            if (potentialParent.Children?.Contains(nodeIndex) == true)
            {
                transform = transform * GetNodeWorldTransform(i);
                break;
            }
        }

        return transform;
    }

    /// <summary>
    /// Gets the local transform matrix for a node in glTF's native format (column-major, right-handed).
    /// Note: Matrix4x4 is row-major in memory, so glTF column-major data appears transposed.
    /// </summary>
    public Matrix4x4 GetNodeLocalTransform(int nodeIndex)
    {
        if (Root == null || nodeIndex < 0 || nodeIndex >= Root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var node = Root.Nodes[nodeIndex];

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
        if (Root == null || accessor.Sparse == null)
        {
            return;
        }

        var sparse = accessor.Sparse;

        // Read indices
        var indicesView = Root.BufferViews[sparse.Indices.BufferView];
        var indicesBuffer = GetBufferData(indicesView.Buffer);
        var indicesOffset = indicesView.ByteOffset + sparse.Indices.ByteOffset;

        // Read values
        var valuesView = Root.BufferViews[sparse.Values.BufferView];
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
                GltfComponentType.UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(indicesBuffer[indexOffset..]),
                GltfComponentType.UnsignedInt => (int)BinaryPrimitives.ReadUInt32LittleEndian(indicesBuffer[indexOffset..]),
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
            if (fileInfo.Length > Options.MaxExternalFileSize)
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