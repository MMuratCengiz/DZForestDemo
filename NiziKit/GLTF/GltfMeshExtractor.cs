using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public static class GltfMeshExtractor
{
    public static List<Mesh> ExtractMeshes(GltfDocument document, HashSet<int>? skinnedMeshIndices = null, bool convertToLeftHanded = true, bool skipFallbackTangents = false)
    {
        var result = new List<Mesh>();
        var root = document.Root;

        if (root.Meshes == null)
        {
            return result;
        }

        for (var meshIndex = 0; meshIndex < root.Meshes.Count; meshIndex++)
        {
            var gltfMesh = root.Meshes[meshIndex];
            var isSkinned = skinnedMeshIndices?.Contains(meshIndex) ?? false;
            var mesh = ExtractMesh(document, gltfMesh, meshIndex, isSkinned, convertToLeftHanded, skipFallbackTangents);
            result.Add(mesh);
        }

        return result;
    }

    public static HashSet<int> GetSkinnedMeshIndices(GltfDocument document)
    {
        var result = new HashSet<int>();
        var root = document.Root;

        if (root.Skins == null || root.Nodes == null)
        {
            return result;
        }

        foreach (var node in root.Nodes)
        {
            if (node.Skin.HasValue && node.Mesh.HasValue)
            {
                result.Add(node.Mesh.Value);
            }
        }

        return result;
    }

    private static Mesh ExtractMesh(GltfDocument document, GltfMesh gltfMesh, int meshIndex, bool isSkinned, bool convertToLeftHanded, bool skipFallbackTangents = false)
    {
        var attributeData = new Dictionary<string, List<byte>>();
        var allIndices = new List<uint>();
        var baseVertex = 0u;
        var totalVertexCount = 0;

        foreach (var primitive in gltfMesh.Primitives)
        {
            if (!primitive.Attributes.TryGetValue("POSITION", out var positionAccessorIndex))
            {
                continue;
            }

            var posAccessor = document.Root.Accessors?[positionAccessorIndex];
            if (posAccessor == null || posAccessor.Count == 0)
            {
                continue;
            }

            var vertexCount = posAccessor.Count;
            totalVertexCount += vertexCount;

            foreach (var (attrName, accessorIndex) in primitive.Attributes)
            {
                if (!attributeData.ContainsKey(attrName))
                {
                    attributeData[attrName] = [];
                }

                ExtractAttributeData(document, accessorIndex, attrName, vertexCount, convertToLeftHanded, attributeData[attrName]);
            }

            if (!skipFallbackTangents)
            {
                if (!primitive.Attributes.ContainsKey("NORMAL"))
                {
                    if (!attributeData.ContainsKey("NORMAL"))
                    {
                        attributeData["NORMAL"] = [];
                    }
                    GenerateDefaultNormals(vertexCount, attributeData["NORMAL"]);
                }

                if (!primitive.Attributes.ContainsKey("TANGENT"))
                {
                    if (!attributeData.ContainsKey("TANGENT"))
                    {
                        attributeData["TANGENT"] = [];
                    }

                    var normalBytes = attributeData.GetValueOrDefault("NORMAL");
                    GenerateDefaultTangents(vertexCount, normalBytes, (int)(baseVertex * 12), attributeData["TANGENT"]);
                }
            }

            ExtractIndices(document, primitive, vertexCount, baseVertex, convertToLeftHanded, allIndices);
            baseVertex += (uint)vertexCount;
        }

        var attributes = new Dictionary<string, MeshAttributeData>();
        foreach (var (attrName, data) in attributeData)
        {
            var type = GetAttributeType(attrName);
            attributes[attrName] = new MeshAttributeData
            {
                Name = attrName,
                Data = data.ToArray(),
                Type = type
            };
        }

        var indices = allIndices.ToArray();

        var sourceAttributes = new MeshAttributeSet
        {
            Attributes = attributes,
            VertexCount = totalVertexCount,
            Indices = indices
        };

        return new Mesh
        {
            Name = gltfMesh.Name ?? $"Mesh_{meshIndex}",
            SourceAttributes = sourceAttributes,
            Bounds = ComputeBoundsFromAttributes(sourceAttributes)
        };
    }

    private static void ExtractAttributeData(
        GltfDocument document,
        int accessorIndex,
        string attrName,
        int vertexCount,
        bool convertToLeftHanded,
        List<byte> output)
    {
        var data = document.GetAccessorData(accessorIndex);
        var stride = document.GetAccessorStride(accessorIndex);
        var accessor = document.Root.Accessors?[accessorIndex];

        if (accessor == null || data.IsEmpty)
        {
            return;
        }

        switch (attrName)
        {
            case "POSITION":
                ExtractVector3Attribute(data, stride, vertexCount, convertToLeftHanded, true, output);
                break;
            case "NORMAL":
                ExtractVector3Attribute(data, stride, vertexCount, convertToLeftHanded, true, output);
                break;
            case "TANGENT":
                ExtractVector4Attribute(data, stride, vertexCount, convertToLeftHanded, true, output);
                break;
            case "JOINTS_0":
                ExtractJointsAttribute(data, stride, vertexCount, accessor.ComponentType, output);
                break;
            case "WEIGHTS_0":
                ExtractWeightsAttribute(data, stride, vertexCount, output);
                break;
            default:
                if (attrName.StartsWith("TEXCOORD_"))
                {
                    ExtractVector2Attribute(data, stride, vertexCount, output);
                }
                else if (attrName.StartsWith("COLOR_"))
                {
                    ExtractVector4Attribute(data, stride, vertexCount, false, false, output);
                }
                break;
        }
    }

    private static void ExtractVector2Attribute(ReadOnlySpan<byte> data, int stride, int vertexCount, List<byte> output)
    {
        var bytes = new byte[vertexCount * 8];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var value = ReadVector2(data, i, stride);
            MemoryMarshal.Write(span[(i * 8)..], in value);
        }

        output.AddRange(bytes);
    }

    private static void ExtractVector3Attribute(ReadOnlySpan<byte> data, int stride, int vertexCount, bool convertToLeftHanded, bool negateZ, List<byte> output)
    {
        var bytes = new byte[vertexCount * 12];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var value = ReadVector3(data, i, stride);
            if (convertToLeftHanded && negateZ)
            {
                value.Z = -value.Z;
            }
            MemoryMarshal.Write(span[(i * 12)..], in value);
        }

        output.AddRange(bytes);
    }

    private static void ExtractVector4Attribute(ReadOnlySpan<byte> data, int stride, int vertexCount, bool convertToLeftHanded, bool negateZ, List<byte> output)
    {
        var bytes = new byte[vertexCount * 16];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var value = ReadVector4(data, i, stride);
            if (convertToLeftHanded && negateZ)
            {
                value.Z = -value.Z;
            }
            MemoryMarshal.Write(span[(i * 16)..], in value);
        }

        output.AddRange(bytes);
    }

    private static void ExtractJointsAttribute(ReadOnlySpan<byte> data, int stride, int vertexCount, int componentType, List<byte> output)
    {
        var bytes = new byte[vertexCount * 16];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var value = ReadJoints(data, i, stride, componentType);
            MemoryMarshal.Write(span[(i * 16)..], in value);
        }

        output.AddRange(bytes);
    }

    private static void ExtractWeightsAttribute(ReadOnlySpan<byte> data, int stride, int vertexCount, List<byte> output)
    {
        var bytes = new byte[vertexCount * 16];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var value = ReadVector4(data, i, stride);
            value = NormalizeWeights(value);
            MemoryMarshal.Write(span[(i * 16)..], in value);
        }

        output.AddRange(bytes);
    }

    private static void GenerateDefaultNormals(int vertexCount, List<byte> output)
    {
        var bytes = new byte[vertexCount * 12];
        var span = bytes.AsSpan();
        var defaultNormal = Vector3.UnitY;

        for (var i = 0; i < vertexCount; i++)
        {
            MemoryMarshal.Write(span[(i * 12)..], in defaultNormal);
        }

        output.AddRange(bytes);
    }

    private static void GenerateDefaultTangents(int vertexCount, List<byte>? normalBytes, int normalOffset, List<byte> output)
    {
        var bytes = new byte[vertexCount * 16];
        var span = bytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            Vector3 normal;
            if (normalBytes != null && normalOffset + (i + 1) * 12 <= normalBytes.Count)
            {
                normal = MemoryMarshal.Read<Vector3>(normalBytes.ToArray().AsSpan()[(normalOffset + i * 12)..]);
            }
            else
            {
                normal = Vector3.UnitY;
            }

            var tangent = ComputeTangent(normal);
            MemoryMarshal.Write(span[(i * 16)..], in tangent);
        }

        output.AddRange(bytes);
    }

    private static VertexAttributeType GetAttributeType(string attrName)
    {
        return attrName switch
        {
            "POSITION" => VertexAttributeType.Float3,
            "NORMAL" => VertexAttributeType.Float3,
            "TANGENT" => VertexAttributeType.Float4,
            "JOINTS_0" => VertexAttributeType.UInt4,
            "WEIGHTS_0" => VertexAttributeType.Float4,
            _ when attrName.StartsWith("TEXCOORD_") => VertexAttributeType.Float2,
            _ when attrName.StartsWith("COLOR_") => VertexAttributeType.Float4,
            _ => VertexAttributeType.Float4
        };
    }

    private static void ExtractIndices(GltfDocument document, GltfPrimitive primitive, int vertexCount, uint baseVertex, bool convertToLeftHanded, List<uint> allIndices)
    {
        if (primitive.Indices.HasValue)
        {
            var indexData = document.GetAccessorData(primitive.Indices.Value);
            var indexAccessor = document.Root.Accessors?[primitive.Indices.Value];
            if (indexAccessor == null || indexData.IsEmpty)
            {
                return;
            }

            var indexCount = indexAccessor.Count;

            switch (primitive.Mode)
            {
                case GltfPrimitiveMode.Triangles:
                    ExtractTriangles(indexData, indexAccessor.ComponentType, indexCount, baseVertex, convertToLeftHanded, allIndices);
                    break;
                case GltfPrimitiveMode.TriangleStrip:
                    ExtractTriangleStrip(indexData, indexAccessor.ComponentType, indexCount, baseVertex, convertToLeftHanded, allIndices);
                    break;
                case GltfPrimitiveMode.TriangleFan:
                    ExtractTriangleFan(indexData, indexAccessor.ComponentType, indexCount, baseVertex, convertToLeftHanded, allIndices);
                    break;
                default:
                    break;
            }
        }
        else
        {
            switch (primitive.Mode)
            {
                case GltfPrimitiveMode.Triangles:
                    for (uint i = 0; i + 2 < vertexCount; i += 3)
                    {
                        AddTriangle(allIndices, baseVertex + i, baseVertex + i + 1, baseVertex + i + 2, convertToLeftHanded);
                    }
                    break;
                case GltfPrimitiveMode.TriangleStrip:
                    for (uint i = 0; i + 2 < vertexCount; i++)
                    {
                        if ((i & 1) == 0)
                        {
                            AddTriangle(allIndices, baseVertex + i, baseVertex + i + 1, baseVertex + i + 2, convertToLeftHanded);
                        }
                        else
                        {
                            AddTriangle(allIndices, baseVertex + i + 1, baseVertex + i, baseVertex + i + 2, convertToLeftHanded);
                        }
                    }
                    break;
                case GltfPrimitiveMode.TriangleFan:
                    for (uint i = 1; i + 1 < vertexCount; i++)
                    {
                        AddTriangle(allIndices, baseVertex, baseVertex + i, baseVertex + i + 1, convertToLeftHanded);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private static void ExtractTriangles(ReadOnlySpan<byte> data, int componentType, int count, uint baseVertex, bool convertToLeftHanded, List<uint> indices)
    {
        for (var i = 0; i + 2 < count; i += 3)
        {
            var a = baseVertex + ReadIndex(data, i, componentType);
            var b = baseVertex + ReadIndex(data, i + 1, componentType);
            var c = baseVertex + ReadIndex(data, i + 2, componentType);

            if (!IsDegenerate(a, b, c))
            {
                AddTriangle(indices, a, b, c, convertToLeftHanded);
            }
        }
    }

    private static void ExtractTriangleStrip(ReadOnlySpan<byte> data, int componentType, int count, uint baseVertex, bool convertToLeftHanded, List<uint> indices)
    {
        for (var i = 0; i + 2 < count; i++)
        {
            var a = baseVertex + ReadIndex(data, i, componentType);
            var b = baseVertex + ReadIndex(data, i + 1, componentType);
            var c = baseVertex + ReadIndex(data, i + 2, componentType);

            if (!IsDegenerate(a, b, c))
            {
                if ((i & 1) == 0)
                {
                    AddTriangle(indices, a, b, c, convertToLeftHanded);
                }
                else
                {
                    AddTriangle(indices, b, a, c, convertToLeftHanded);
                }
            }
        }
    }

    private static void ExtractTriangleFan(ReadOnlySpan<byte> data, int componentType, int count, uint baseVertex, bool convertToLeftHanded, List<uint> indices)
    {
        if (count < 3)
        {
            return;
        }

        var center = baseVertex + ReadIndex(data, 0, componentType);

        for (var i = 1; i + 1 < count; i++)
        {
            var b = baseVertex + ReadIndex(data, i, componentType);
            var c = baseVertex + ReadIndex(data, i + 1, componentType);

            if (!IsDegenerate(center, b, c))
            {
                AddTriangle(indices, center, b, c, convertToLeftHanded);
            }
        }
    }

    private static void AddTriangle(List<uint> indices, uint a, uint b, uint c, bool convertToLeftHanded)
    {
        if (convertToLeftHanded)
        {
            indices.Add(a);
            indices.Add(c);
            indices.Add(b);
        }
        else
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }
    }

    private static bool IsDegenerate(uint a, uint b, uint c)
    {
        return a == b || b == c || a == c;
    }

    private static uint ReadIndex(ReadOnlySpan<byte> data, int index, int componentType)
    {
        var stride = componentType switch
        {
            GltfComponentType.UnsignedByte => 1,
            GltfComponentType.UnsignedShort => 2,
            GltfComponentType.UnsignedInt => 4,
            _ => 4
        };

        var offset = index * stride;
        if (offset + stride > data.Length)
        {
            return 0;
        }

        return componentType switch
        {
            GltfComponentType.UnsignedByte => data[offset],
            GltfComponentType.UnsignedShort => BitConverter.ToUInt16(data[offset..]),
            GltfComponentType.UnsignedInt => BitConverter.ToUInt32(data[offset..]),
            _ => 0
        };
    }

    private static Vector2 ReadVector2(ReadOnlySpan<byte> data, int index, int stride)
    {
        var offset = index * stride;
        if (offset + 8 > data.Length)
        {
            return Vector2.Zero;
        }
        return MemoryMarshal.Read<Vector2>(data[offset..]);
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> data, int index, int stride)
    {
        var offset = index * stride;
        if (offset + 12 > data.Length)
        {
            return Vector3.Zero;
        }
        return MemoryMarshal.Read<Vector3>(data[offset..]);
    }

    private static Vector4 ReadVector4(ReadOnlySpan<byte> data, int index, int stride)
    {
        var offset = index * stride;
        if (offset + 16 > data.Length)
        {
            return Vector4.Zero;
        }
        return MemoryMarshal.Read<Vector4>(data[offset..]);
    }

    private static UInt4 ReadJoints(ReadOnlySpan<byte> data, int index, int stride, int componentType)
    {
        var offset = index * stride;

        switch (componentType)
        {
            case GltfComponentType.UnsignedByte:
                if (offset + 4 > data.Length)
                {
                    return default;
                }
                return new UInt4
                {
                    X = data[offset],
                    Y = data[offset + 1],
                    Z = data[offset + 2],
                    W = data[offset + 3]
                };
            case GltfComponentType.UnsignedShort:
                if (offset + 8 > data.Length)
                {
                    return default;
                }
                return new UInt4
                {
                    X = BitConverter.ToUInt16(data[offset..]),
                    Y = BitConverter.ToUInt16(data[(offset + 2)..]),
                    Z = BitConverter.ToUInt16(data[(offset + 4)..]),
                    W = BitConverter.ToUInt16(data[(offset + 6)..])
                };
            default:
                return default;
        }
    }

    private static Vector4 NormalizeWeights(Vector4 weights)
    {
        var sum = weights.X + weights.Y + weights.Z + weights.W;
        if (sum > 0.0001f)
        {
            return weights / sum;
        }
        return new Vector4(1, 0, 0, 0);
    }

    private static Vector4 ComputeTangent(Vector3 normal)
    {
        var up = MathF.Abs(normal.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        return new Vector4(tangent, 1.0f);
    }

    private static BoundingBox ComputeBoundsFromAttributes(MeshAttributeSet attributes)
    {
        var positionData = attributes.GetAttribute("POSITION");
        if (positionData == null || positionData.Data.Length == 0)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var span = positionData.Data.AsSpan();

        for (var i = 0; i < attributes.VertexCount; i++)
        {
            var pos = MemoryMarshal.Read<Vector3>(span[(i * 12)..]);
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        return new BoundingBox(min, max);
    }
}
