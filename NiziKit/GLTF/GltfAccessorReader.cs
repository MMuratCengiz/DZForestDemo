using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public readonly ref struct GltfAccessorReader
{
    private readonly ReadOnlySpan<byte> _data;
    private readonly GltfAccessor _accessor;
    private readonly int _stride;
    private readonly int _elementSize;

    public GltfAccessorReader(GltfDocument document, int accessorIndex)
    {
        _accessor = document.Root.Accessors![accessorIndex];
        _data = document.GetAccessorData(accessorIndex);
        _stride = document.GetAccessorStride(accessorIndex);

        var componentSize = GltfComponentType.GetSize(_accessor.ComponentType);
        var componentCount = GltfAccessorType.GetComponentCount(_accessor.Type);
        _elementSize = componentSize * componentCount;
    }

    public int Count => _accessor.Count;
    public int ComponentType => _accessor.ComponentType;
    public string Type => _accessor.Type;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat(int index)
    {
        var offset = index * _stride;
        if (offset + _elementSize > _data.Length)
        {
            return 0f;
        }
        return _accessor.ComponentType switch
        {
            GltfComponentType.Float => BinaryPrimitives.ReadSingleLittleEndian(_data[offset..]),
            GltfComponentType.Byte => (sbyte)_data[offset] / 127f,
            GltfComponentType.UnsignedByte => _data[offset] / 255f,
            GltfComponentType.Short => BinaryPrimitives.ReadInt16LittleEndian(_data[offset..]) / 32767f,
            GltfComponentType.UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(_data[offset..]) / 65535f,
            _ => 0f
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ReadVector2(int index)
    {
        var offset = index * _stride;
        if (offset + _elementSize > _data.Length)
        {
            return Vector2.Zero;
        }
        if (_accessor.ComponentType == GltfComponentType.Float)
        {
            return MemoryMarshal.Read<Vector2>(_data[offset..]);
        }
        return new Vector2(
            ReadComponentFloat(offset, 0),
            ReadComponentFloat(offset, 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 ReadVector3(int index)
    {
        var offset = index * _stride;
        if (offset + _elementSize > _data.Length)
        {
            return Vector3.Zero;
        }
        if (_accessor.ComponentType == GltfComponentType.Float)
        {
            return MemoryMarshal.Read<Vector3>(_data[offset..]);
        }
        return new Vector3(
            ReadComponentFloat(offset, 0),
            ReadComponentFloat(offset, 1),
            ReadComponentFloat(offset, 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 ReadVector4(int index)
    {
        var offset = index * _stride;
        if (offset + _elementSize > _data.Length)
        {
            return Vector4.Zero;
        }
        if (_accessor.ComponentType == GltfComponentType.Float)
        {
            return MemoryMarshal.Read<Vector4>(_data[offset..]);
        }
        return new Vector4(
            ReadComponentFloat(offset, 0),
            ReadComponentFloat(offset, 1),
            ReadComponentFloat(offset, 2),
            ReadComponentFloat(offset, 3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion ReadQuaternion(int index)
    {
        var v = ReadVector4(index);
        return new Quaternion(v.X, v.Y, v.Z, v.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4 ReadMatrix4x4(int index)
    {
        var offset = index * _stride;
        if (offset + _elementSize > _data.Length)
        {
            return Matrix4x4.Identity;
        }
        if (_accessor.ComponentType == GltfComponentType.Float)
        {
            return MemoryMarshal.Read<Matrix4x4>(_data[offset..]);
        }

        var result = new Matrix4x4();
        for (var i = 0; i < 16; i++)
        {
            Unsafe.Add(ref result.M11, i) = ReadComponentFloat(offset, i);
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (uint a, uint b, uint c, uint d) ReadUInt4(int index)
    {
        var offset = index * _stride;
        var size = _accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => 4,
            GltfComponentType.UnsignedShort => 8,
            GltfComponentType.UnsignedInt => 16,
            _ => 4
        };
        if (offset + size > _data.Length)
        {
            return (0u, 0u, 0u, 0u);
        }
        return _accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => (
                _data[offset],
                _data[offset + 1],
                _data[offset + 2],
                _data[offset + 3]),
            GltfComponentType.UnsignedShort => (
                BinaryPrimitives.ReadUInt16LittleEndian(_data[(offset)..]),
                BinaryPrimitives.ReadUInt16LittleEndian(_data[(offset + 2)..]),
                BinaryPrimitives.ReadUInt16LittleEndian(_data[(offset + 4)..]),
                BinaryPrimitives.ReadUInt16LittleEndian(_data[(offset + 6)..])
            ),
            GltfComponentType.UnsignedInt => (
                BinaryPrimitives.ReadUInt32LittleEndian(_data[(offset)..]),
                BinaryPrimitives.ReadUInt32LittleEndian(_data[(offset + 4)..]),
                BinaryPrimitives.ReadUInt32LittleEndian(_data[(offset + 8)..]),
                BinaryPrimitives.ReadUInt32LittleEndian(_data[(offset + 12)..])
            ),
            _ => (0u, 0u, 0u, 0u)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadIndex(int index)
    {
        var offset = index * _stride;
        var size = _accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => 1,
            GltfComponentType.UnsignedShort => 2,
            GltfComponentType.UnsignedInt => 4,
            _ => 4
        };
        if (offset + size > _data.Length)
        {
            return 0u;
        }
        return _accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => _data[offset],
            GltfComponentType.UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(_data[offset..]),
            GltfComponentType.UnsignedInt => BinaryPrimitives.ReadUInt32LittleEndian(_data[offset..]),
            _ => 0u
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ReadComponentFloat(int baseOffset, int componentIndex)
    {
        var componentSize = GltfComponentType.GetSize(_accessor.ComponentType);
        var offset = baseOffset + componentIndex * componentSize;

        if (offset + componentSize > _data.Length)
        {
            return 0f;
        }

        return _accessor.ComponentType switch
        {
            GltfComponentType.Float => BinaryPrimitives.ReadSingleLittleEndian(_data[offset..]),
            GltfComponentType.Byte => _accessor.Normalized ? (sbyte)_data[offset] / 127f : (sbyte)_data[offset],
            GltfComponentType.UnsignedByte => _accessor.Normalized ? _data[offset] / 255f : _data[offset],
            GltfComponentType.Short => _accessor.Normalized
                ? BinaryPrimitives.ReadInt16LittleEndian(_data[offset..]) / 32767f
                : BinaryPrimitives.ReadInt16LittleEndian(_data[offset..]),
            GltfComponentType.UnsignedShort => _accessor.Normalized
                ? BinaryPrimitives.ReadUInt16LittleEndian(_data[offset..]) / 65535f
                : BinaryPrimitives.ReadUInt16LittleEndian(_data[offset..]),
            GltfComponentType.UnsignedInt => BinaryPrimitives.ReadUInt32LittleEndian(_data[offset..]),
            _ => 0f
        };
    }
}
