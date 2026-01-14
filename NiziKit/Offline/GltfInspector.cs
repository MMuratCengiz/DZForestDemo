using System.Numerics;
using NiziKit.GLTF;

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
            var bytes = File.ReadAllBytes(gltfPath);
            var basePath = Path.GetDirectoryName(gltfPath);

            Func<string, byte[]> loadBuffer = uri =>
            {
                var fullPath = Path.Combine(basePath ?? "", uri);
                return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : [];
            };

            var document = GltfReader.Read(bytes, loadBuffer, basePath);
            var root = document.Root;

            var meshes = InspectMeshes(document);
            var materials = InspectMaterials(document, gltfPath);

            return GltfInspectionResult.Succeeded(
                meshes,
                materials,
                root.Animations?.Count > 0,
                root.Skins?.Count > 0
            );
        }
        catch (Exception ex)
        {
            return GltfInspectionResult.Failed($"Failed to load glTF: {ex.Message}");
        }
    }

    private static List<GltfMeshInfo> InspectMeshes(GltfDocument document)
    {
        var meshes = new List<GltfMeshInfo>();
        var root = document.Root;

        if (root.Meshes == null)
        {
            return meshes;
        }

        var skinMeshIndices = GltfMeshExtractor.GetSkinnedMeshIndices(document);

        for (var meshIndex = 0; meshIndex < root.Meshes.Count; meshIndex++)
        {
            var mesh = root.Meshes[meshIndex];
            var vertexCount = 0;
            var indexCount = 0;
            var materialIndex = -1;

            foreach (var primitive in mesh.Primitives)
            {
                if (primitive.Attributes.TryGetValue("POSITION", out var positionAccessorIndex))
                {
                    if (root.Accessors != null && positionAccessorIndex < root.Accessors.Count)
                    {
                        vertexCount += root.Accessors[positionAccessorIndex].Count;
                    }
                }

                if (primitive.Indices.HasValue && root.Accessors != null &&
                    primitive.Indices.Value < root.Accessors.Count)
                {
                    indexCount += root.Accessors[primitive.Indices.Value].Count;
                }

                if (materialIndex < 0 && primitive.Material.HasValue)
                {
                    materialIndex = primitive.Material.Value;
                }
            }

            meshes.Add(new GltfMeshInfo
            {
                Index = meshIndex,
                Name = mesh.Name ?? $"Mesh_{meshIndex}",
                PrimitiveCount = mesh.Primitives.Count,
                VertexCount = vertexCount,
                IndexCount = indexCount,
                HasSkinning = skinMeshIndices.Contains(meshIndex),
                MaterialIndex = materialIndex
            });
        }

        return meshes;
    }

    private static List<GltfMaterialInfo> InspectMaterials(GltfDocument document, string gltfPath)
    {
        var result = new List<GltfMaterialInfo>();
        var gltfDirectory = Path.GetDirectoryName(gltfPath) ?? "";

        var materials = GltfMaterialExtractor.ExtractMaterials(document);

        for (var i = 0; i < materials.Count; i++)
        {
            var material = materials[i];

            string? baseColorTexture = null;
            string? normalTexture = null;
            string? metallicRoughnessTexture = null;

            if (material.BaseColorTexture != null)
            {
                baseColorTexture = GetTexturePath(document, material.BaseColorTexture.TextureIndex, gltfDirectory);
            }

            if (material.NormalTexture != null)
            {
                normalTexture = GetTexturePath(document, material.NormalTexture.TextureIndex, gltfDirectory);
            }

            if (material.MetallicRoughnessTexture != null)
            {
                metallicRoughnessTexture = GetTexturePath(document, material.MetallicRoughnessTexture.TextureIndex, gltfDirectory);
            }

            result.Add(new GltfMaterialInfo
            {
                Index = i,
                Name = material.Name,
                BaseColor = material.BaseColorFactor,
                Metallic = material.MetallicFactor,
                Roughness = material.RoughnessFactor,
                BaseColorTexturePath = baseColorTexture,
                NormalTexturePath = normalTexture,
                MetallicRoughnessTexturePath = metallicRoughnessTexture
            });
        }

        return result;
    }

    private static string? GetTexturePath(GltfDocument document, int textureIndex, string gltfDirectory)
    {
        var imageIndex = GltfMaterialExtractor.GetImageIndex(document, textureIndex);
        if (!imageIndex.HasValue)
        {
            return null;
        }

        var images = GltfMaterialExtractor.ExtractImages(document);
        if (imageIndex.Value >= images.Count)
        {
            return null;
        }

        var image = images[imageIndex.Value];
        if (string.IsNullOrEmpty(image.Uri) || image.Uri.StartsWith("data:"))
        {
            return null;
        }

        return Path.IsPathRooted(image.Uri) ? image.Uri : Path.Combine(gltfDirectory, image.Uri);
    }
}
