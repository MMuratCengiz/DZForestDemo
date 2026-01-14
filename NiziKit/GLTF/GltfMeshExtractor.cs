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
            if (primitive.Mode != GltfPrimitiveMode.Triangles)
            {
                continue;
            }

            if (!primitive.Attributes.TryGetValue("POSITION", out var positionAccessor))
            {
                continue;
            }

            var posReader = new GltfAccessorReader(document, positionAccessor);
            var vertexCount = posReader.Count;

            var hasNormal = primitive.Attributes.TryGetValue("NORMAL", out var normalAcc);
            var hasTexCoord = primitive.Attributes.TryGetValue("TEXCOORD_0", out var texCoordAcc);
            var hasTangent = primitive.Attributes.TryGetValue("TANGENT", out var tangentAcc);

            var hasJoints = false;
            var hasWeights = false;
            int jointsAcc = 0, weightsAcc = 0;
            if (isSkinned)
            {
                hasJoints = primitive.Attributes.TryGetValue("JOINTS_0", out jointsAcc);
                hasWeights = primitive.Attributes.TryGetValue("WEIGHTS_0", out weightsAcc);
            }

            var vertexBytes = new byte[vertexCount * format.Stride];
            var span = vertexBytes.AsSpan();

            for (var i = 0; i < vertexCount; i++)
            {
                var offset = i * format.Stride;

                var pos = posReader.ReadVector3(i);
                if (convertToLeftHanded)
                {
                    pos.Z = -pos.Z;
                }
                MemoryMarshal.Write(span[offset..], in pos);

                Vector3 normal;
                if (hasNormal)
                {
                    var normalReader = new GltfAccessorReader(document, normalAcc);
                    normal = normalReader.ReadVector3(i);
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
                if (hasTexCoord)
                {
                    var texCoordReader = new GltfAccessorReader(document, texCoordAcc);
                    uv = texCoordReader.ReadVector2(i);
                }
                else
                {
                    uv = Vector2.Zero;
                }
                MemoryMarshal.Write(span[(offset + 24)..], in uv);

                Vector4 tangent;
                if (hasTangent)
                {
                    var tangentReader = new GltfAccessorReader(document, tangentAcc);
                    tangent = tangentReader.ReadVector4(i);
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
                    if (hasWeights)
                    {
                        var weightsReader = new GltfAccessorReader(document, weightsAcc);
                        weight = weightsReader.ReadVector4(i);
                    }
                    else
                    {
                        weight = Vector4.Zero;
                    }
                    MemoryMarshal.Write(span[(offset + 48)..], in weight);

                    UInt4 jointData;
                    if (hasJoints)
                    {
                        var jointsReader = new GltfAccessorReader(document, jointsAcc);
                        var joints = jointsReader.ReadUInt4(i);
                        jointData = new UInt4 { X = joints.a, Y = joints.b, Z = joints.c, W = joints.d };
                    }
                    else
                    {
                        jointData = default;
                    }
                    MemoryMarshal.Write(span[(offset + 64)..], in jointData);
                }
            }

            allVertices.AddRange(vertexBytes);

            if (primitive.Indices.HasValue)
            {
                var indexReader = new GltfAccessorReader(document, primitive.Indices.Value);
                for (var i = 0; i < indexReader.Count; i += 3)
                {
                    if (convertToLeftHanded)
                    {
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i));
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i + 2));
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i + 1));
                    }
                    else
                    {
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i));
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i + 1));
                        allIndices.Add(baseVertex + indexReader.ReadIndex(i + 2));
                    }
                }
            }
            else
            {
                for (uint i = 0; i < vertexCount; i += 3)
                {
                    if (convertToLeftHanded)
                    {
                        allIndices.Add(baseVertex + i);
                        allIndices.Add(baseVertex + i + 2);
                        allIndices.Add(baseVertex + i + 1);
                    }
                    else
                    {
                        allIndices.Add(baseVertex + i);
                        allIndices.Add(baseVertex + i + 1);
                        allIndices.Add(baseVertex + i + 2);
                    }
                }
            }

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

    private static Vector4 ComputeTangent(Vector3 normal)
    {
        var up = MathF.Abs(normal.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        return new Vector4(tangent, 1.0f);
    }

    private static BoundingBox ComputeBounds(byte[] vertices, VertexFormat format)
    {
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
