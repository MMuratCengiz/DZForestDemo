using System.Numerics;
using System.Runtime.InteropServices;
using SharpGLTF.Schema2;

namespace DenOfIz.World.Offline;

public sealed class MeshExportSettings
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
    public MeshExportResult ExportMesh(string gltfPath, int meshIndex, MeshExportSettings settings)
    {
        if (!File.Exists(gltfPath))
        {
            return MeshExportResult.Failed($"File not found: {gltfPath}");
        }

        try
        {
            var model = ModelRoot.Load(gltfPath);

            if (meshIndex < 0 || meshIndex >= model.LogicalMeshes.Count)
            {
                return MeshExportResult.Failed($"Invalid mesh index: {meshIndex}");
            }

            var mesh = model.LogicalMeshes[meshIndex];
            var isSkinned = IsMeshSkinned(model, meshIndex);

            var (vertices, indices) = ExtractMeshData(mesh, isSkinned, settings.ConvertToLeftHanded);

            MeshMaterial? material = null;
            if (settings.IncludeMaterial)
            {
                material = ExtractMaterial(mesh, model, gltfPath);
            }

            WriteMesh(settings.OutputPath, vertices, indices, isSkinned, material);

            return MeshExportResult.Succeeded(settings.OutputPath, vertices.Length, indices.Length);
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
            var model = ModelRoot.Load(gltfPath);
            Directory.CreateDirectory(outputDirectory);

            foreach (var meshIndex in meshIndices)
            {
                if (meshIndex < 0 || meshIndex >= model.LogicalMeshes.Count)
                {
                    results.Add(MeshExportResult.Failed($"Invalid mesh index: {meshIndex}"));
                    continue;
                }

                var mesh = model.LogicalMeshes[meshIndex];
                var meshName = mesh.Name ?? $"Mesh_{meshIndex}";
                var outputPath = Path.Combine(outputDirectory, $"{meshName}.dzmesh");

                var result = ExportMesh(gltfPath, meshIndex, new MeshExportSettings
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

    public IReadOnlyList<MeshExportResult> ExportAllMeshes(
        string gltfPath,
        string outputDirectory,
        bool includeMaterials = false)
    {
        if (!File.Exists(gltfPath))
        {
            return [MeshExportResult.Failed($"File not found: {gltfPath}")];
        }

        try
        {
            var model = ModelRoot.Load(gltfPath);
            var indices = Enumerable.Range(0, model.LogicalMeshes.Count);
            return ExportMeshes(gltfPath, indices, outputDirectory, includeMaterials);
        }
        catch (Exception ex)
        {
            return [MeshExportResult.Failed($"Failed to load glTF: {ex.Message}")];
        }
    }

    private static bool IsMeshSkinned(ModelRoot model, int meshIndex)
    {
        foreach (var skin in model.LogicalSkins)
        {
            foreach (var node in model.LogicalNodes)
            {
                if (node.Skin == skin && node.Mesh?.LogicalIndex == meshIndex)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static (ExportVertex[] vertices, uint[] indices) ExtractMeshData(
        Mesh mesh,
        bool isSkinned,
        bool convertToLeftHanded)
    {
        var allVertices = new List<ExportVertex>();
        var allIndices = new List<uint>();

        foreach (var primitive in mesh.Primitives)
        {
            var baseVertex = (uint)allVertices.Count;

            var posAccessor = primitive.GetVertexAccessor("POSITION");
            if (posAccessor == null)
            {
                continue;
            }

            var positions = posAccessor.AsVector3Array();
            var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
            var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
            var tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
            var joints = primitive.GetVertexAccessor("JOINTS_0");
            var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

            for (var i = 0; i < positions.Count; i++)
            {
                var pos = positions[i];
                var normal = normals != null && i < normals.Count ? normals[i] : Vector3.UnitY;
                var texCoord = texCoords != null && i < texCoords.Count ? texCoords[i] : Vector2.Zero;
                var tangent = tangents != null && i < tangents.Count ? tangents[i] : new Vector4(1, 0, 0, 1);
                var weight = weights != null && i < weights.Count ? weights[i] : Vector4.Zero;

                uint j0 = 0, j1 = 0, j2 = 0, j3 = 0;
                if (joints != null && i < joints.Count)
                {
                    var jointData = ReadJointIndices(joints, i);
                    j0 = jointData.X;
                    j1 = jointData.Y;
                    j2 = jointData.Z;
                    j3 = jointData.W;
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
                    BoneIndex0 = isSkinned ? j0 : 0,
                    BoneIndex1 = isSkinned ? j1 : 0,
                    BoneIndex2 = isSkinned ? j2 : 0,
                    BoneIndex3 = isSkinned ? j3 : 0
                });
            }

            var indexAccessor = primitive.IndexAccessor;
            if (indexAccessor != null)
            {
                var indices = indexAccessor.AsIndicesArray();
                for (var i = 0; i < indices.Count; i += 3)
                {
                    if (convertToLeftHanded)
                    {
                        allIndices.Add(baseVertex + (uint)indices[i]);
                        allIndices.Add(baseVertex + (uint)indices[i + 2]);
                        allIndices.Add(baseVertex + (uint)indices[i + 1]);
                    }
                    else
                    {
                        allIndices.Add(baseVertex + (uint)indices[i]);
                        allIndices.Add(baseVertex + (uint)indices[i + 1]);
                        allIndices.Add(baseVertex + (uint)indices[i + 2]);
                    }
                }
            }
            else
            {
                for (uint i = 0; i < positions.Count; i += 3)
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

    private static (uint X, uint Y, uint Z, uint W) ReadJointIndices(Accessor accessor, int index)
    {
        var encoding = accessor.Encoding;
        if (encoding == EncodingType.UNSIGNED_BYTE)
        {
            var data = accessor.AsVector4Array();
            var v = data[index];
            return ((uint)v.X, (uint)v.Y, (uint)v.Z, (uint)v.W);
        }
        else if (encoding == EncodingType.UNSIGNED_SHORT)
        {
            var data = accessor.AsVector4Array();
            var v = data[index];
            return ((uint)v.X, (uint)v.Y, (uint)v.Z, (uint)v.W);
        }
        else
        {
            var data = accessor.AsVector4Array();
            var v = data[index];
            return ((uint)v.X, (uint)v.Y, (uint)v.Z, (uint)v.W);
        }
    }

    private static MeshMaterial? ExtractMaterial(Mesh mesh, ModelRoot model, string gltfPath)
    {
        Material? gltfMaterial = null;
        foreach (var primitive in mesh.Primitives)
        {
            if (primitive.Material != null)
            {
                gltfMaterial = primitive.Material;
                break;
            }
        }

        if (gltfMaterial == null)
        {
            return null;
        }

        var gltfDirectory = Path.GetDirectoryName(gltfPath) ?? "";
        var baseColorChannel = gltfMaterial.FindChannel("BaseColor");
        var metallicRoughnessChannel = gltfMaterial.FindChannel("MetallicRoughness");
        var normalChannel = gltfMaterial.FindChannel("Normal");

        var baseColor = baseColorChannel?.Color ?? Vector4.One;
        var metallic = 1.0f;
        var roughness = 1.0f;

        if (metallicRoughnessChannel != null)
        {
            foreach (var param in metallicRoughnessChannel.Value.Parameters)
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
        }

        return new MeshMaterial
        {
            Name = gltfMaterial.Name ?? "Material",
            BaseColor = [baseColor.X, baseColor.Y, baseColor.Z, baseColor.W],
            Metallic = metallic,
            Roughness = roughness,
            BaseColorTexture = GetTexturePath(baseColorChannel?.Texture, gltfDirectory),
            NormalTexture = GetTexturePath(normalChannel?.Texture, gltfDirectory),
            MetallicRoughnessTexture = GetTexturePath(metallicRoughnessChannel?.Texture, gltfDirectory)
        };
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
        using var writer = new System.IO.BinaryWriter(stream);

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

    private static void WriteMaterial(System.IO.BinaryWriter writer, MeshMaterial material)
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

    private static void WriteLengthPrefixedString(System.IO.BinaryWriter writer, string? value)
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
