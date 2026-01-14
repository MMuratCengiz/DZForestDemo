using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public static class GltfMeshExtractor
{
    public static List<Mesh> ExtractMeshes(GltfDocument document, HashSet<int>? skinnedMeshIndices = null, bool convertToLeftHanded = true)
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
            var mesh = ExtractMesh(document, gltfMesh, meshIndex, isSkinned, convertToLeftHanded);
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

    private static Mesh ExtractMesh(GltfDocument document, GltfMesh gltfMesh, int meshIndex, bool isSkinned, bool convertToLeftHanded)
    {
        var format = isSkinned ? VertexFormat.Skinned : VertexFormat.Static;
        var allVertices = new List<byte>();
        var allIndices = new List<uint>();
        var baseVertex = 0u;

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

            var hasNormal = primitive.Attributes.TryGetValue("NORMAL", out var normalAccIndex);
            var hasTexCoord = primitive.Attributes.TryGetValue("TEXCOORD_0", out var texCoordAccIndex);
            var hasTangent = primitive.Attributes.TryGetValue("TANGENT", out var tangentAccIndex);

            var hasJoints = false;
            var hasWeights = false;
            int jointsAccIndex = 0, weightsAccIndex = 0;
            if (isSkinned)
            {
                hasJoints = primitive.Attributes.TryGetValue("JOINTS_0", out jointsAccIndex);
                hasWeights = primitive.Attributes.TryGetValue("WEIGHTS_0", out weightsAccIndex);
            }

            var posData = document.GetAccessorData(positionAccessorIndex);
            var posStride = document.GetAccessorStride(positionAccessorIndex);

            var normalData = hasNormal ? document.GetAccessorData(normalAccIndex) : ReadOnlySpan<byte>.Empty;
            var normalStride = hasNormal ? document.GetAccessorStride(normalAccIndex) : 0;

            var texCoordData = hasTexCoord ? document.GetAccessorData(texCoordAccIndex) : ReadOnlySpan<byte>.Empty;
            var texCoordStride = hasTexCoord ? document.GetAccessorStride(texCoordAccIndex) : 0;

            var tangentData = hasTangent ? document.GetAccessorData(tangentAccIndex) : ReadOnlySpan<byte>.Empty;
            var tangentStride = hasTangent ? document.GetAccessorStride(tangentAccIndex) : 0;

            var jointsData = hasJoints ? document.GetAccessorData(jointsAccIndex) : ReadOnlySpan<byte>.Empty;
            var jointsStride = hasJoints ? document.GetAccessorStride(jointsAccIndex) : 0;
            var jointsAccessor = hasJoints ? document.Root.Accessors?[jointsAccIndex] : null;

            var weightsData = hasWeights ? document.GetAccessorData(weightsAccIndex) : ReadOnlySpan<byte>.Empty;
            var weightsStride = hasWeights ? document.GetAccessorStride(weightsAccIndex) : 0;

            var vertexBytes = new byte[vertexCount * format.Stride];
            var span = vertexBytes.AsSpan();

            for (var i = 0; i < vertexCount; i++)
            {
                var offset = i * format.Stride;

                var pos = ReadVector3(posData, i, posStride);
                if (convertToLeftHanded)
                {
                    pos.Z = -pos.Z;
                }
                MemoryMarshal.Write(span[offset..], in pos);

                Vector3 normal;
                if (hasNormal && !normalData.IsEmpty)
                {
                    normal = ReadVector3(normalData, i, normalStride);
                }
                else
                {
                    normal = Vector3.UnitY;
                }
                if (convertToLeftHanded)
                {
                    normal.Z = -normal.Z;
                }
                MemoryMarshal.Write(span[(offset + 12)..], in normal);

                Vector2 uv;
                if (hasTexCoord && !texCoordData.IsEmpty)
                {
                    uv = ReadVector2(texCoordData, i, texCoordStride);
                }
                else
                {
                    uv = Vector2.Zero;
                }
                MemoryMarshal.Write(span[(offset + 24)..], in uv);

                Vector4 tangent;
                if (hasTangent && !tangentData.IsEmpty)
                {
                    tangent = ReadVector4(tangentData, i, tangentStride);
                }
                else
                {
                    tangent = ComputeTangent(normal);
                }
                if (convertToLeftHanded)
                {
                    tangent.Z = -tangent.Z;
                }
                MemoryMarshal.Write(span[(offset + 32)..], in tangent);

                if (isSkinned)
                {
                    Vector4 weight;
                    if (hasWeights && !weightsData.IsEmpty)
                    {
                        weight = ReadVector4(weightsData, i, weightsStride);
                        weight = NormalizeWeights(weight);
                    }
                    else
                    {
                        weight = new Vector4(1, 0, 0, 0);
                    }
                    MemoryMarshal.Write(span[(offset + 48)..], in weight);

                    UInt4 jointData;
                    if (hasJoints && !jointsData.IsEmpty && jointsAccessor != null)
                    {
                        jointData = ReadJoints(jointsData, i, jointsStride, jointsAccessor.ComponentType);
                    }
                    else
                    {
                        jointData = default;
                    }
                    MemoryMarshal.Write(span[(offset + 64)..], in jointData);
                }
            }

            allVertices.AddRange(vertexBytes);

            ExtractIndices(document, primitive, vertexCount, baseVertex, convertToLeftHanded, allIndices);

            baseVertex += (uint)vertexCount;
        }

        var vertices = allVertices.ToArray();
        var indices = allIndices.ToArray();

        return new Mesh
        {
            Name = gltfMesh.Name ?? $"Mesh_{meshIndex}",
            Format = format,
            MeshType = isSkinned ? MeshType.Skinned : MeshType.Static,
            CpuVertices = vertices,
            CpuIndices = indices,
            Bounds = ComputeBounds(vertices, format)
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

    private static BoundingBox ComputeBounds(byte[] vertices, VertexFormat format)
    {
        if (vertices.Length == 0)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var vertexCount = vertices.Length / format.Stride;
        var span = vertices.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var pos = MemoryMarshal.Read<Vector3>(span[(i * format.Stride)..]);
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        return new BoundingBox(min, max);
    }
}
