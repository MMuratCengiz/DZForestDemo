using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RuntimeAssets.GltfModels;

public sealed class GltfDocument
{
    private readonly string _basePath;
    private readonly List<string> _warnings = [];
    private readonly List<string> _errors = [];
    private readonly Dictionary<int, byte[]> _loadedBuffers = [];
    private readonly Dictionary<int, byte[]> _loadedImages = [];
    private readonly GltfDocumentDesc _desc;
    private GltfRoot? _root;

    private byte[]? _embeddedBinaryBuffer;

    internal GltfDocument(string basePath, GltfDocumentDesc desc)
    {
        _basePath = Path.GetDirectoryName(basePath) ?? "";
        _desc = desc;
    }

    public bool HasErrors => _errors.Count > 0;
    public bool HasWarnings => _warnings.Count > 0;
    public IReadOnlyList<string> Warnings => _warnings;
    public IReadOnlyList<string> Errors => _errors;

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
        _desc.Logger?.Invoke(GltfLogLevel.Warning, message);
    }

    internal void AddError(string message)
    {
        _errors.Add(message);
        _desc.Logger?.Invoke(GltfLogLevel.Error, message);
    }

    private ReadOnlySpan<byte> GetBufferData(int bufferIndex)
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
        if (bufferIndex == 0 && string.IsNullOrEmpty(buffer.Uri) && _embeddedBinaryBuffer != null)
        {
            _loadedBuffers[bufferIndex] = _embeddedBinaryBuffer;
            return _embeddedBinaryBuffer;
        }

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

        if (!string.IsNullOrEmpty(buffer.Uri) && _desc.LoadExternalBuffers)
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

    private ReadOnlySpan<byte> GetBufferViewData(int bufferViewIndex)
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
        if (image.BufferView.HasValue)
        {
            var data = GetBufferViewData(image.BufferView.Value);
            if (data.IsEmpty)
            {
                return default;
            }

            var copy = data.ToArray();
            _loadedImages[imageIndex] = copy;
            return copy;
        }

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

        if (!string.IsNullOrEmpty(image.Uri) && _desc.LoadExternalImages)
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

    public string? GetImageFilePath(int imageIndex)
    {
        if (_root == null || imageIndex < 0 || imageIndex >= _root.Images.Count)
        {
            return null;
        }

        var image = _root.Images[imageIndex];
        if (image.BufferView.HasValue)
        {
            return null;
        }

        if (string.IsNullOrEmpty(image.Uri))
        {
            return null;
        }

        if (image.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (IsAbsoluteUri(image.Uri))
        {
            AddWarning($"Skipping absolute/external URI for image {imageIndex}: {image.Uri}");
            return null;
        }

        var resolvedPath = Path.Combine(_basePath, Uri.UnescapeDataString(image.Uri));
        return Path.GetFullPath(resolvedPath);
    }

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

        if (!accessor.BufferView.HasValue)
        {
            return accessor.Sparse != null ? ReadSparseAccessor<T>(accessor) : new T[count];
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
            srcSlice[..copySize].CopyTo(resultSpan.Slice(dstOffset, copySize));
        }

        if (accessor.Sparse != null)
        {
            ApplySparseData(result, accessor);
        }

        return result;
    }

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

    public Vector2[] ReadAccessorVec2(int accessorIndex) => ReadAccessor<Vector2>(accessorIndex);
    public Vector3[] ReadAccessorVec3(int accessorIndex) => ReadAccessor<Vector3>(accessorIndex);
    public Vector4[] ReadAccessorVec4(int accessorIndex) => ReadAccessor<Vector4>(accessorIndex);
    public float[] ReadAccessorFloat(int accessorIndex) => ReadAccessor<float>(accessorIndex);

    public Quaternion[] ReadAccessorQuat(int accessorIndex) => ReadAccessor<Quaternion>(accessorIndex);

    public Matrix4x4[] ReadAccessorMat4(int accessorIndex)
    {
        var matrices = ReadAccessor<Matrix4x4>(accessorIndex);
        for (var i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.Transpose(matrices[i]);
        }

        return matrices;
    }

    public Matrix4x4 GetNodeWorldTransform(int nodeIndex)
    {
        if (_root == null || nodeIndex < 0 || nodeIndex >= _root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var transform = GetNodeLocalTransform(nodeIndex);
        for (var i = 0; i < _root.Nodes.Count; i++)
        {
            var potentialParent = _root.Nodes[i];
            if (potentialParent.Children?.Contains(nodeIndex) == true)
            {
                transform *= GetNodeWorldTransform(i);
                break;
            }
        }

        return transform;
    }

    public Matrix4x4 GetNodeLocalTransform(int nodeIndex)
    {
        if (_root == null || nodeIndex < 0 || nodeIndex >= _root.Nodes.Count)
        {
            return Matrix4x4.Identity;
        }

        var node = _root.Nodes[nodeIndex];

        if (node.Matrix is { Length: 16 })
        {
            return new Matrix4x4(
                node.Matrix[0], node.Matrix[4], node.Matrix[8], node.Matrix[12],
                node.Matrix[1], node.Matrix[5], node.Matrix[9], node.Matrix[13],
                node.Matrix[2], node.Matrix[6], node.Matrix[10], node.Matrix[14],
                node.Matrix[3], node.Matrix[7], node.Matrix[11], node.Matrix[15]
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

        var indicesView = _root.BufferViews[sparse.Indices.BufferView];
        var indicesBuffer = GetBufferData(indicesView.Buffer);
        var indicesOffset = indicesView.ByteOffset + sparse.Indices.ByteOffset;

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
                GltfComponentType.UnsignedShort =>
                    BinaryPrimitives.ReadUInt16LittleEndian(indicesBuffer[indexOffset..]),
                GltfComponentType.UnsignedInt => (int)BinaryPrimitives.ReadUInt32LittleEndian(
                    indicesBuffer[indexOffset..]),
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
            if (fileInfo.Length > _desc.MaxExternalFileSize)
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
            var header = uri.AsSpan(5, commaIndex - 5);
            return header.Contains("base64", StringComparison.OrdinalIgnoreCase)
                ? Convert.FromBase64String(data.ToString())
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(data.ToString()));
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
               (uri.Length > 2 && uri[1] == ':');
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