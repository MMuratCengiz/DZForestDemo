using System.Numerics;
using SharpGLTF.Schema2;

namespace RuntimeAssets;

public sealed class GltfLoadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<MeshData> Meshes { get; init; } = [];
    public IReadOnlyList<MaterialData> Materials { get; init; } = [];
    public IReadOnlyList<Matrix4x4> InverseBindMatrices { get; init; } = [];

    public static GltfLoadResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}

public sealed class GltfLoader
{
    public async Task<GltfLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Load(path), cancellationToken);
    }

    public GltfLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return GltfLoadResult.Failed($"File not found: {path}");
        }

        try
        {
            var model = ModelRoot.Load(path);
            var materials = LoadMaterials(model);
            var meshes = LoadMeshes(model);
            var inverseBindMatrices = LoadInverseBindMatrices(model);

            return new GltfLoadResult
            {
                Success = true,
                Meshes = meshes,
                Materials = materials,
                InverseBindMatrices = inverseBindMatrices
            };
        }
        catch (Exception ex)
        {
            return GltfLoadResult.Failed(ex.Message);
        }
    }

    private static List<MaterialData> LoadMaterials(ModelRoot model)
    {
        var materials = new List<MaterialData>();

        foreach (var mat in model.LogicalMaterials)
        {
            var pbr = mat.FindChannel("BaseColor");
            var baseColor = Vector4.One;
            string? baseColorTexture = null;

            if (pbr != null)
            {
                baseColor = pbr.Value.Color;
                baseColorTexture = pbr.Value.Texture?.PrimaryImage?.Content.SourcePath;
            }

            var mrChannel = mat.FindChannel("MetallicRoughness");
            var metallic = 0f;
            var roughness = 1f;
            string? mrTexture = null;

            if (mrChannel != null)
            {
                metallic = mrChannel.Value.GetFactor("MetallicFactor");
                roughness = mrChannel.Value.GetFactor("RoughnessFactor");
                mrTexture = mrChannel.Value.Texture?.PrimaryImage?.Content.SourcePath;
            }

            var normalChannel = mat.FindChannel("Normal");
            string? normalTexture = null;

            if (normalChannel != null)
            {
                normalTexture = normalChannel.Value.Texture?.PrimaryImage?.Content.SourcePath;
            }

            materials.Add(new MaterialData
            {
                Name = mat.Name ?? $"Material_{materials.Count}",
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness,
                BaseColorTexturePath = baseColorTexture,
                NormalTexturePath = normalTexture,
                MetallicRoughnessTexturePath = mrTexture
            });
        }

        return materials;
    }

    private static List<MeshData> LoadMeshes(ModelRoot model)
    {
        var meshes = new List<MeshData>();

        foreach (var mesh in model.LogicalMeshes)
        {
            var primitives = new List<MeshPrimitive>();

            foreach (var primitive in mesh.Primitives)
            {
                var vertices = LoadVertices(primitive);
                var indices = LoadIndices(primitive);

                primitives.Add(new MeshPrimitive
                {
                    Vertices = vertices,
                    Indices = indices,
                    MaterialIndex = primitive.Material?.LogicalIndex ?? -1
                });
            }

            meshes.Add(new MeshData
            {
                Name = mesh.Name ?? $"Mesh_{meshes.Count}",
                Primitives = primitives
            });
        }

        return meshes;
    }

    private static Vertex[] LoadVertices(SharpGLTF.Schema2.MeshPrimitive primitive)
    {
        var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();
        if (positions == null)
        {
            return [];
        }

        var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
        var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
        var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
        var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
        var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

        var vertices = new Vertex[positions.Count];

        for (var i = 0; i < positions.Count; i++)
        {
            vertices[i] = new Vertex
            {
                Position = positions[i],
                Normal = normals?[i] ?? Vector3.UnitY,
                TexCoord = texCoords?[i] ?? Vector2.Zero,
                Tangent = tangents?[i] ?? new Vector4(1, 0, 0, 1),
                BoneWeights = weights?[i] ?? Vector4.Zero,
                BoneIndices = joints != null
                    ? new UInt4
                    {
                        X = (uint)joints[i].X,
                        Y = (uint)joints[i].Y,
                        Z = (uint)joints[i].Z,
                        W = (uint)joints[i].W
                    }
                    : default
            };
        }

        return vertices;
    }

    private static uint[] LoadIndices(SharpGLTF.Schema2.MeshPrimitive primitive)
    {
        var accessor = primitive.IndexAccessor;
        if (accessor == null)
        {
            return [];
        }

        var indices = accessor.AsIndicesArray();
        var result = new uint[indices.Count];

        for (var i = 0; i < indices.Count; i++)
        {
            result[i] = indices[i];
        }

        return result;
    }

    private static List<Matrix4x4> LoadInverseBindMatrices(ModelRoot model)
    {
        var matrices = new List<Matrix4x4>();

        foreach (var skin in model.LogicalSkins)
        {
            var ibm = skin.GetInverseBindMatricesAccessor()?.AsMatrix4x4Array();
            if (ibm != null)
            {
                matrices.AddRange(ibm);
            }
        }

        return matrices;
    }
}
