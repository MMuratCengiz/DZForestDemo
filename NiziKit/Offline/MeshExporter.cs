using System.Numerics;
using System.Runtime.InteropServices;
using NiziKit.GLTF;
using NiziKit.GLTF.Data;

namespace NiziKit.Offline;

public sealed class MeshExportDesc
{
    public required string OutputPath { get; set; }
    public bool IncludeMaterial { get; set; } = false;
    public bool ConvertToLeftHanded { get; set; } = true;
}

public sealed class MeshExportResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? OutputPath { get; init; }
    public int VertexCount { get; init; }
    public int IndexCount { get; init; }

    public static MeshExportResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static MeshExportResult Succeeded(string path, int vertices, int indices) => new()
    {
        Success = true,
        OutputPath = path,
        VertexCount = vertices,
        IndexCount = indices
    };
}

[StructLayout(LayoutKind.Sequential)]
public struct ExportVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;
    public Vector4 Tangent;
    public Vector4 BoneWeights;
    public uint BoneIndex0;
    public uint BoneIndex1;
    public uint BoneIndex2;
    public uint BoneIndex3;
}

public sealed class MeshExporter
{
    public MeshExportResult ExportMesh(string gltfPath, int meshIndex, MeshExportDesc desc)
    {
        if (!File.Exists(gltfPath))
        {
            return MeshExportResult.Failed($"File not found: {gltfPath}");
        }

        try
        {
            var document = LoadDocument(gltfPath);
            var root = document.Root;

            if (root.Meshes == null || meshIndex < 0 || meshIndex >= root.Meshes.Count)
            {
                return MeshExportResult.Failed($"Invalid mesh index: {meshIndex}");
            }

            var mesh = root.Meshes[meshIndex];
            var skinMeshIndices = GltfMeshExtractor.GetSkinnedMeshIndices(document);
            var isSkinned = skinMeshIndices.Contains(meshIndex);

            var (vertices, indices) = ExtractMeshData(document, mesh, isSkinned, desc.ConvertToLeftHanded);

            MeshMaterial? material = null;
            if (desc.IncludeMaterial)
            {
                material = ExtractMaterial(document, mesh, gltfPath);
            }

            WriteMesh(desc.OutputPath, vertices, indices, isSkinned, material);

            return MeshExportResult.Succeeded(desc.OutputPath, vertices.Length, indices.Length);
        }
        catch (Exception ex)
        {
            return MeshExportResult.Failed($"Failed to export mesh: {ex.Message}");
        }
    }

    public IReadOnlyList<MeshExportResult> ExportMeshes(
        string gltfPath,
        IEnumerable<int> meshIndices,
        string outputDirectory,
        bool includeMaterials = false)
    {
        var results = new List<MeshExportResult>();

        if (!File.Exists(gltfPath))
        {
            results.Add(MeshExportResult.Failed($"File not found: {gltfPath}"));
            return results;
        }

        try
        {
            var document = LoadDocument(gltfPath);
            var root = document.Root;
            Directory.CreateDirectory(outputDirectory);

            foreach (var meshIndex in meshIndices)
            {
                if (root.Meshes == null || meshIndex < 0 || meshIndex >= root.Meshes.Count)
                {
                    results.Add(MeshExportResult.Failed($"Invalid mesh index: {meshIndex}"));
                    continue;
                }

                var mesh = root.Meshes[meshIndex];
                var meshName = mesh.Name ?? $"Mesh_{meshIndex}";
                var outputPath = Path.Combine(outputDirectory, $"{meshName}.dzmesh");

                var result = ExportMesh(gltfPath, meshIndex, new MeshExportDesc
                {
                    OutputPath = outputPath,
                    IncludeMaterial = includeMaterials
                });
                results.Add(result);
            }
        }
        catch (Exception ex)
        {
            results.Add(MeshExportResult.Failed($"Failed to load glTF: {ex.Message}"));
        }

        return results;
    }

    public IReadOnlyList<MeshExportResult> ExportAllMeshes(string gltfPath, string outputDirectory,
        bool includeMaterials = false)
    {
        if (!File.Exists(gltfPath))
        {
            return [MeshExportResult.Failed($"File not found: {gltfPath}")];
        }

        try
        {
            var document = LoadDocument(gltfPath);
            var meshCount = document.Root.Meshes?.Count ?? 0;
            var indices = Enumerable.Range(0, meshCount);
            return ExportMeshes(gltfPath, indices, outputDirectory, includeMaterials);
        }
        catch (Exception ex)
        {
            return [MeshExportResult.Failed($"Failed to load glTF: {ex.Message}")];
        }
    }

    private static GltfDocument LoadDocument(string gltfPath)
    {
        var bytes = File.ReadAllBytes(gltfPath);
        var basePath = Path.GetDirectoryName(gltfPath);

        Func<string, byte[]> loadBuffer = uri =>
        {
            var fullPath = Path.Combine(basePath ?? "", uri);
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : [];
        };

        return GltfReader.Read(bytes, loadBuffer, basePath);
    }

    private static (ExportVertex[] vertices, uint[] indices) ExtractMeshData(
        GltfDocument document,
        GltfMesh mesh,
        bool isSkinned,
        bool convertToLeftHanded)
    {
        var allVertices = new List<ExportVertex>();
        var allIndices = new List<uint>();

        foreach (var primitive in mesh.Primitives)
        {
            if (primitive.Mode != GltfPrimitiveMode.Triangles)
            {
                continue;
            }

            if (!primitive.Attributes.TryGetValue("POSITION", out var positionAccessor))
            {
                continue;
            }

            var baseVertex = (uint)allVertices.Count;
            var posReader = new GltfAccessorReader(document, positionAccessor);
            var vertexCount = posReader.Count;

            primitive.Attributes.TryGetValue("NORMAL", out var normalAcc);
            primitive.Attributes.TryGetValue("TEXCOORD_0", out var texCoordAcc);
            primitive.Attributes.TryGetValue("TANGENT", out var tangentAcc);
            primitive.Attributes.TryGetValue("JOINTS_0", out var jointsAcc);
            primitive.Attributes.TryGetValue("WEIGHTS_0", out var weightsAcc);

            var hasNormal = normalAcc > 0 || primitive.Attributes.ContainsKey("NORMAL");
            var hasTexCoord = texCoordAcc > 0 || primitive.Attributes.ContainsKey("TEXCOORD_0");
            var hasTangent = tangentAcc > 0 || primitive.Attributes.ContainsKey("TANGENT");
            var hasJoints = jointsAcc > 0 || primitive.Attributes.ContainsKey("JOINTS_0");
            var hasWeights = weightsAcc > 0 || primitive.Attributes.ContainsKey("WEIGHTS_0");

            for (var i = 0; i < vertexCount; i++)
            {
                var pos = posReader.ReadVector3(i);

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

                Vector2 texCoord;
                if (hasTexCoord)
                {
                    var texCoordReader = new GltfAccessorReader(document, texCoordAcc);
                    texCoord = texCoordReader.ReadVector2(i);
                }
                else
                {
                    texCoord = Vector2.Zero;
                }

                Vector4 tangent;
                if (hasTangent)
                {
                    var tangentReader = new GltfAccessorReader(document, tangentAcc);
                    tangent = tangentReader.ReadVector4(i);
                }
                else
                {
                    tangent = new Vector4(1, 0, 0, 1);
                }

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

                (uint a, uint b, uint c, uint d) joints;
                if (hasJoints)
                {
                    var jointsReader = new GltfAccessorReader(document, jointsAcc);
                    joints = jointsReader.ReadUInt4(i);
                }
                else
                {
                    joints = (0u, 0u, 0u, 0u);
                }

                if (convertToLeftHanded)
                {
                    pos.Z = -pos.Z;
                    normal.Z = -normal.Z;
                    tangent.Z = -tangent.Z;
                }

                allVertices.Add(new ExportVertex
                {
                    Position = pos,
                    Normal = normal,
                    TexCoord = texCoord,
                    Tangent = tangent,
                    BoneWeights = isSkinned ? weight : Vector4.Zero,
                    BoneIndex0 = isSkinned ? joints.a : 0,
                    BoneIndex1 = isSkinned ? joints.b : 0,
                    BoneIndex2 = isSkinned ? joints.c : 0,
                    BoneIndex3 = isSkinned ? joints.d : 0
                });
            }

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
        }

        return (allVertices.ToArray(), allIndices.ToArray());
    }

    private static MeshMaterial? ExtractMaterial(GltfDocument document, GltfMesh mesh, string gltfPath)
    {
        var materialIndex = -1;
        foreach (var primitive in mesh.Primitives)
        {
            if (primitive.Material.HasValue)
            {
                materialIndex = primitive.Material.Value;
                break;
            }
        }

        if (materialIndex < 0)
        {
            return null;
        }

        var materials = GltfMaterialExtractor.ExtractMaterials(document);
        if (materialIndex >= materials.Count)
        {
            return null;
        }

        var material = materials[materialIndex];
        var gltfDirectory = Path.GetDirectoryName(gltfPath) ?? "";

        return new MeshMaterial
        {
            Name = material.Name,
            BaseColor = [material.BaseColorFactor.X, material.BaseColorFactor.Y, material.BaseColorFactor.Z, material.BaseColorFactor.W],
            Metallic = material.MetallicFactor,
            Roughness = material.RoughnessFactor,
            BaseColorTexture = GetTexturePath(document, material.BaseColorTexture, gltfDirectory),
            NormalTexture = GetTexturePath(document, material.NormalTexture, gltfDirectory),
            MetallicRoughnessTexture = GetTexturePath(document, material.MetallicRoughnessTexture, gltfDirectory)
        };
    }

    private static string? GetTexturePath(GltfDocument document, GltfTextureReference? textureRef, string gltfDirectory)
    {
        if (textureRef == null)
        {
            return null;
        }

        var imageIndex = GltfMaterialExtractor.GetImageIndex(document, textureRef.TextureIndex);
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

    private static void WriteMesh(
        string outputPath,
        ExportVertex[] vertices,
        uint[] indices,
        bool isSkinned,
        MeshMaterial? material)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        var flags = MeshFlags.None;
        if (isSkinned)
        {
            flags |= MeshFlags.IsSkinned;
        }

        if (material != null)
        {
            flags |= MeshFlags.HasMaterial;
        }

        var vertexDataOffset = (ulong)MeshFormat.HeaderNumBytes;
        var vertexDataNumBytes = (ulong)(vertices.Length * MeshFormat.VertexNumBytes);
        var indexDataOffset = vertexDataOffset + vertexDataNumBytes;
        var indexDataNumBytes = (ulong)(indices.Length * sizeof(uint));
        var materialOffset = material != null ? indexDataOffset + indexDataNumBytes : 0;

        writer.Write(System.Text.Encoding.ASCII.GetBytes(MeshFormat.Magic));
        writer.Write(MeshFormat.CurrentVersion);
        writer.Write((uint)flags);
        writer.Write((uint)vertices.Length);
        writer.Write((uint)indices.Length);
        writer.Write(vertexDataOffset);
        writer.Write(vertexDataNumBytes);
        writer.Write(indexDataOffset);
        writer.Write(indexDataNumBytes);
        writer.Write(materialOffset);
        writer.Write(0u);

        foreach (var vertex in vertices)
        {
            writer.Write(vertex.Position.X);
            writer.Write(vertex.Position.Y);
            writer.Write(vertex.Position.Z);
            writer.Write(vertex.Normal.X);
            writer.Write(vertex.Normal.Y);
            writer.Write(vertex.Normal.Z);
            writer.Write(vertex.TexCoord.X);
            writer.Write(vertex.TexCoord.Y);
            writer.Write(vertex.Tangent.X);
            writer.Write(vertex.Tangent.Y);
            writer.Write(vertex.Tangent.Z);
            writer.Write(vertex.Tangent.W);
            writer.Write(vertex.BoneWeights.X);
            writer.Write(vertex.BoneWeights.Y);
            writer.Write(vertex.BoneWeights.Z);
            writer.Write(vertex.BoneWeights.W);
            writer.Write(vertex.BoneIndex0);
            writer.Write(vertex.BoneIndex1);
            writer.Write(vertex.BoneIndex2);
            writer.Write(vertex.BoneIndex3);
        }

        foreach (var index in indices)
        {
            writer.Write(index);
        }

        if (material != null)
        {
            WriteMaterial(writer, material);
        }
    }

    private static void WriteMaterial(BinaryWriter writer, MeshMaterial material)
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(material.Name);
        writer.Write((ushort)nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write(material.BaseColor[0]);
        writer.Write(material.BaseColor[1]);
        writer.Write(material.BaseColor[2]);
        writer.Write(material.BaseColor[3]);
        writer.Write(material.Metallic);
        writer.Write(material.Roughness);

        WriteLengthPrefixedString(writer, material.BaseColorTexture);
        WriteLengthPrefixedString(writer, material.NormalTexture);
        WriteLengthPrefixedString(writer, material.MetallicRoughnessTexture);
    }

    private static void WriteLengthPrefixedString(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write((ushort)0);
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }
}
