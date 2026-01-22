using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.Assets;

public static class VertexPacker
{
    public static byte[] Pack(MeshAttributeSet source, VertexFormat targetFormat)
    {
        var vertexCount = source.VertexCount;
        var result = new byte[vertexCount * targetFormat.Stride];
        var resultSpan = result.AsSpan();

        foreach (var attr in targetFormat.Attributes)
        {
            var gltfName = MeshAttributeSet.MapSemanticToGltf(attr.Semantic, attr.SemanticIndex);
            var sourceAttr = source.GetAttribute(gltfName);

            if (sourceAttr != null)
            {
                WriteAttributeData(sourceAttr, resultSpan, attr, targetFormat.Stride, vertexCount);
            }
            else
            {
                WriteDefaultValues(resultSpan, attr, targetFormat.Stride, vertexCount);
            }
        }

        return result;
    }

    private static void WriteAttributeData(
        MeshAttributeData source,
        Span<byte> dest,
        VertexAttribute attr,
        int stride,
        int vertexCount)
    {
        var sourceSpan = source.Data.AsSpan();
        var sourceSizePerVertex = GetTypeSizeInBytes(source.Type);
        var destSizePerVertex = attr.SizeInBytes;

        for (var i = 0; i < vertexCount; i++)
        {
            var srcOffset = i * sourceSizePerVertex;
            var dstOffset = i * stride + attr.Offset;

            if (srcOffset + sourceSizePerVertex > sourceSpan.Length)
            {
                WriteDefaultValue(dest, attr, dstOffset);
                continue;
            }

            if (source.Type == attr.Type)
            {
                sourceSpan.Slice(srcOffset, sourceSizePerVertex).CopyTo(dest.Slice(dstOffset, destSizePerVertex));
            }
            else
            {
                ConvertAndWrite(sourceSpan.Slice(srcOffset, sourceSizePerVertex), source.Type, dest, attr, dstOffset);
            }
        }
    }

    private static void ConvertAndWrite(
        ReadOnlySpan<byte> source,
        VertexAttributeType sourceType,
        Span<byte> dest,
        VertexAttribute destAttr,
        int destOffset)
    {
        if (sourceType == VertexAttributeType.UByte4 && destAttr.Type == VertexAttributeType.UInt4)
        {
            var joint = new UInt4
            {
                X = source[0],
                Y = source[1],
                Z = source[2],
                W = source[3]
            };
            MemoryMarshal.Write(dest[destOffset..], in joint);
            return;
        }

        source.Slice(0, Math.Min(source.Length, destAttr.SizeInBytes)).CopyTo(dest.Slice(destOffset, destAttr.SizeInBytes));
    }

    private static void WriteDefaultValues(
        Span<byte> dest,
        VertexAttribute attr,
        int stride,
        int vertexCount)
    {
        for (var i = 0; i < vertexCount; i++)
        {
            var offset = i * stride + attr.Offset;
            WriteDefaultValue(dest, attr, offset);
        }
    }

    private static void WriteDefaultValue(Span<byte> dest, VertexAttribute attr, int offset)
    {
        switch (attr.Semantic)
        {
            case "POSITION":
                {
                    var value = Vector3.Zero;
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "NORMAL":
                {
                    var value = Vector3.UnitY;
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "TANGENT":
                {
                    var value = new Vector4(1, 0, 0, 1);
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "TEXCOORD":
                {
                    var value = Vector2.Zero;
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "COLOR":
                {
                    var value = Vector4.One;
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "BLENDWEIGHT":
                {
                    var value = new Vector4(1, 0, 0, 0);
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            case "BLENDINDICES":
                {
                    var value = new UInt4 { X = 0, Y = 0, Z = 0, W = 0 };
                    MemoryMarshal.Write(dest[offset..], in value);
                    break;
                }
            default:
                {
                    dest.Slice(offset, attr.SizeInBytes).Clear();
                    break;
                }
        }
    }

    private static int GetTypeSizeInBytes(VertexAttributeType type)
    {
        return type switch
        {
            VertexAttributeType.Float => 4,
            VertexAttributeType.Float2 => 8,
            VertexAttributeType.Float3 => 12,
            VertexAttributeType.Float4 => 16,
            VertexAttributeType.UInt4 => 16,
            VertexAttributeType.UByte4 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
