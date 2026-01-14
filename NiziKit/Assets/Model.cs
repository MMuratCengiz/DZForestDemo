using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace NiziKit.Assets;

public class Model : IAsset
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public List<Mesh> Meshes { get; set; } = [];
    public Skeleton? Skeleton { get; set; }
    public List<Animation> Animations { get; set; } = [];
    public List<Material> Materials { get; set; } = [];

    public void Load(GraphicsContext context, string path)
    {
        var bytes = Content.ReadBytes($"Models/{path}");
        LoadFromBytes(bytes, path);
    }

    public async Task LoadAsync(GraphicsContext context, string path, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync($"Models/{path}", ct);
        LoadFromBytes(bytes, path);
    }

    public void LoadFromBytes(byte[] bytes, string name)
    {
        using var stream = new MemoryStream(bytes);
        var gltf = ModelRoot.ReadGLB(stream, new ReadSettings{ Validation = ValidationMode.Skip });
        Name = Path.GetFileNameWithoutExtension(name);
        SourcePath = name;
        ExtractMeshes(gltf);
    }

    private void ExtractMeshes(ModelRoot gltf)
    {
        var skinnedMeshIndices = GetSkinnedMeshIndices(gltf);

        foreach (var gltfMesh in gltf.LogicalMeshes)
        {
            var isSkinned = skinnedMeshIndices.Contains(gltfMesh.LogicalIndex);
            var mesh = ExtractMesh(gltfMesh, isSkinned);
            Meshes.Add(mesh);
        }
    }

    private static HashSet<int> GetSkinnedMeshIndices(ModelRoot gltf)
    {
        var result = new HashSet<int>();
        foreach (var skin in gltf.LogicalSkins)
        {
            foreach (var node in gltf.LogicalNodes)
            {
                if (node.Skin == skin && node.Mesh != null)
                {
                    result.Add(node.Mesh.LogicalIndex);
                }
            }
        }

        return result;
    }

    private static Mesh ExtractMesh(SharpGLTF.Schema2.Mesh gltfMesh, bool isSkinned)
    {
        var format = isSkinned ? VertexFormat.Skinned : VertexFormat.Static;
        var allVertices = new List<byte>();
        var allIndices = new List<uint>();
        var baseVertex = 0u;

        foreach (var primitive in gltfMesh.Primitives)
        {
            var posAccessor = primitive.GetVertexAccessor("POSITION");
            if (posAccessor == null) continue;

            var positions = posAccessor.AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var joints = primitive.GetVertexAccessor("JOINTS_0");
            var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            var vertexBytes = new byte[positions.Count * format.Stride];
            var span = vertexBytes.AsSpan();

            for (var i = 0; i < positions.Count; i++)
            {
                var offset = i * format.Stride;

                var pos = positions[i];
                pos.Z = -pos.Z;
                MemoryMarshal.Write(span[offset..], in pos);

                var normal = normals != null && i < normals.Count ? normals[i] : Vector3.UnitY;
                normal.Z = -normal.Z;
                MemoryMarshal.Write(span[(offset + 12)..], in normal);

                var uv = texCoords != null && i < texCoords.Count ? texCoords[i] : Vector2.Zero;
                MemoryMarshal.Write(span[(offset + 24)..], in uv);

                var tangent = tangents != null && i < tangents.Count ? tangents[i] : ComputeTangent(normal);
                tangent.Z = -tangent.Z;
                MemoryMarshal.Write(span[(offset + 32)..], in tangent);

                if (isSkinned)
                {
                    var weight = weights != null && i < weights.Count ? weights[i] : Vector4.Zero;
                    MemoryMarshal.Write(span[(offset + 48)..], in weight);

                    var jointData = ReadJointIndices(joints, i);
                    MemoryMarshal.Write(span[(offset + 64)..], in jointData);
                }
            }

            allVertices.AddRange(vertexBytes);

            var indexAccessor = primitive.IndexAccessor;
            if (indexAccessor != null)
            {
                var primitiveIndices = indexAccessor.AsIndicesArray();
                for (var i = 0; i < primitiveIndices.Count; i += 3)
                {
                    allIndices.Add(baseVertex + (uint)primitiveIndices[i]);
                    allIndices.Add(baseVertex + (uint)primitiveIndices[i + 2]);
                    allIndices.Add(baseVertex + (uint)primitiveIndices[i + 1]);
                }
            }
            else
            {
                for (uint i = 0; i < positions.Count; i += 3)
                {
                    allIndices.Add(baseVertex + i);
                    allIndices.Add(baseVertex + i + 2);
                    allIndices.Add(baseVertex + i + 1);
                }
            }

            baseVertex += (uint)positions.Count;
        }

        var vertices = allVertices.ToArray();
        var indices = allIndices.ToArray();

        return new Mesh
        {
            Name = gltfMesh.Name ?? $"Mesh_{gltfMesh.LogicalIndex}",
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

    private static UInt4 ReadJointIndices(Accessor? accessor, int index)
    {
        if (accessor == null) return default;

        var data = accessor.AsVector4Array();
        if (index >= data.Count) return default;

        var v = data[index];
        return new UInt4 { X = (uint)v.X, Y = (uint)v.Y, Z = (uint)v.Z, W = (uint)v.W };
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

    public void Dispose()
    {
        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }

        Skeleton?.Dispose();

        foreach (var animation in Animations)
        {
            animation.Dispose();
        }
    }
}
