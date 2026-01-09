using System.Numerics;
using SharpGLTF.Schema2;

namespace NiziKit.Offline;

public sealed class GltfMeshInfo
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required int PrimitiveCount { get; init; }
    public required int VertexCount { get; init; }
    public required int IndexCount { get; init; }
    public required bool HasSkinning { get; init; }
    public required int MaterialIndex { get; init; }
}

public sealed class GltfMaterialInfo
{
    public required int Index { get; init; }
    public required string Name { get; init; }
    public required Vector4 BaseColor { get; init; }
    public required float Metallic { get; init; }
    public required float Roughness { get; init; }
    public string? BaseColorTexturePath { get; init; }
    public string? NormalTexturePath { get; init; }
    public string? MetallicRoughnessTexturePath { get; init; }
}

public sealed class GltfInspectionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<GltfMeshInfo> Meshes { get; init; } = [];
    public IReadOnlyList<GltfMaterialInfo> Materials { get; init; } = [];
    public bool HasAnimations { get; init; }
    public bool HasSkins { get; init; }

    public static GltfInspectionResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static GltfInspectionResult Succeeded(
        IReadOnlyList<GltfMeshInfo> meshes,
        IReadOnlyList<GltfMaterialInfo> materials,
        bool hasAnimations,
        bool hasSkins) => new()
    {
        Success = true,
        Meshes = meshes,
        Materials = materials,
        HasAnimations = hasAnimations,
        HasSkins = hasSkins
    };
}

public sealed class GltfInspector
{
    public GltfInspectionResult Inspect(string gltfPath)
    {
        if (!File.Exists(gltfPath))
        {
            return GltfInspectionResult.Failed($"File not found: {gltfPath}");
        }

        try
        {
            var model = ModelRoot.Load(gltfPath);
            var meshes = InspectMeshes(model);
            var materials = InspectMaterials(model, gltfPath);

            return GltfInspectionResult.Succeeded(
                meshes,
                materials,
                model.LogicalAnimations.Count > 0,
                model.LogicalSkins.Count > 0
            );
        }
        catch (Exception ex)
        {
            return GltfInspectionResult.Failed($"Failed to load glTF: {ex.Message}");
        }
    }

    private static List<GltfMeshInfo> InspectMeshes(ModelRoot model)
    {
        var meshes = new List<GltfMeshInfo>();
        var skinMeshIndices = new HashSet<int>();

        foreach (var skin in model.LogicalSkins)
        {
            foreach (var node in model.LogicalNodes)
            {
                if (node.Skin == skin && node.Mesh != null)
                {
                    skinMeshIndices.Add(node.Mesh.LogicalIndex);
                }
            }
        }

        foreach (var mesh in model.LogicalMeshes)
        {
            var vertexCount = 0;
            var indexCount = 0;
            var materialIndex = -1;

            foreach (var primitive in mesh.Primitives)
            {
                var positionAccessor = primitive.GetVertexAccessor("POSITION");
                if (positionAccessor != null)
                {
                    vertexCount += positionAccessor.Count;
                }

                var indexAccessor = primitive.IndexAccessor;
                if (indexAccessor != null)
                {
                    indexCount += indexAccessor.Count;
                }

                if (materialIndex < 0 && primitive.Material != null)
                {
                    materialIndex = primitive.Material.LogicalIndex;
                }
            }

            meshes.Add(new GltfMeshInfo
            {
                Index = mesh.LogicalIndex,
                Name = mesh.Name ?? $"Mesh_{mesh.LogicalIndex}",
                PrimitiveCount = mesh.Primitives.Count,
                VertexCount = vertexCount,
                IndexCount = indexCount,
                HasSkinning = skinMeshIndices.Contains(mesh.LogicalIndex),
                MaterialIndex = materialIndex
            });
        }

        return meshes;
    }

    private static List<GltfMaterialInfo> InspectMaterials(ModelRoot model, string gltfPath)
    {
        var materials = new List<GltfMaterialInfo>();
        var gltfDirectory = Path.GetDirectoryName(gltfPath) ?? "";

        foreach (var material in model.LogicalMaterials)
        {
            var pbr = material.FindChannel("BaseColor");
            var baseColor = Vector4.One;
            string? baseColorTexture = null;

            if (pbr != null)
            {
                baseColor = pbr.Value.Color;
                baseColorTexture = GetTexturePath(pbr.Value.Texture, gltfDirectory);
            }

            var metallicRoughness = material.FindChannel("MetallicRoughness");
            var metallic = 1.0f;
            var roughness = 1.0f;
            string? metallicRoughnessTexture = null;

            if (metallicRoughness != null)
            {
                var parameters = metallicRoughness.Value.Parameters;
                foreach (var param in parameters)
                {
                    if (param.Name == "MetallicFactor")
                    {
                        metallic = (float)param.Value;
                    }
                    else if (param.Name == "RoughnessFactor")
                    {
                        roughness = (float)param.Value;
                    }
                }
                metallicRoughnessTexture = GetTexturePath(metallicRoughness.Value.Texture, gltfDirectory);
            }

            var normalChannel = material.FindChannel("Normal");
            string? normalTexture = null;
            if (normalChannel != null)
            {
                normalTexture = GetTexturePath(normalChannel.Value.Texture, gltfDirectory);
            }

            materials.Add(new GltfMaterialInfo
            {
                Index = material.LogicalIndex,
                Name = material.Name ?? $"Material_{material.LogicalIndex}",
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness,
                BaseColorTexturePath = baseColorTexture,
                NormalTexturePath = normalTexture,
                MetallicRoughnessTexturePath = metallicRoughnessTexture
            });
        }

        return materials;
    }

    private static string? GetTexturePath(SharpGLTF.Schema2.Texture? texture, string gltfDirectory)
    {
        if (texture?.PrimaryImage?.Content == null)
        {
            return null;
        }

        var sourceUri = texture.PrimaryImage.Content.SourcePath;
        if (string.IsNullOrEmpty(sourceUri))
        {
            return null;
        }

        return Path.IsPathRooted(sourceUri) ? sourceUri : Path.Combine(gltfDirectory, sourceUri);
    }
}
