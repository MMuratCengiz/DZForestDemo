using System.Numerics;
using System.Runtime.InteropServices;

namespace DenOfIz.World.Assets;

[Flags]
internal enum MeshFileFlags : uint
{
    None = 0,
    IsSkinned = 1 << 0,
    HasMaterial = 1 << 1
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MeshFileHeader
{
    public byte Magic0, Magic1, Magic2, Magic3, Magic4, Magic5;
    public ushort Version;
    public MeshFileFlags Flags;
    public uint VertexCount;
    public uint IndexCount;
    public ulong VertexDataOffset;
    public ulong VertexDataNumBytes;
    public ulong IndexDataOffset;
    public ulong IndexDataNumBytes;
    public ulong MaterialOffset;
    public uint Reserved;

    public readonly bool IsSkinned => (Flags & MeshFileFlags.IsSkinned) != 0;
    public readonly bool HasMaterial => (Flags & MeshFileFlags.HasMaterial) != 0;

    public readonly string GetMagic() => new(new[] { (char)Magic0, (char)Magic1, (char)Magic2, (char)Magic3, (char)Magic4, (char)Magic5 });
}

public sealed class MeshLoadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Vertex[]? Vertices { get; init; }
    public uint[]? Indices { get; init; }
    public MeshType MeshType { get; init; }
    public MaterialData? Material { get; init; }

    public static MeshLoadResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static MeshLoadResult Succeeded(Vertex[] vertices, uint[] indices, MeshType meshType, MaterialData? material = null) => new()
    {
        Success = true,
        Vertices = vertices,
        Indices = indices,
        MeshType = meshType,
        Material = material
    };
}

public sealed class MeshLoader
{
    private const string ExpectedMagic = "DZMESH";
    private const ushort SupportedVersion = 1;
    private const int HeaderNumBytes = 64;

    public MeshLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return MeshLoadResult.Failed($"File not found: {path}");
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new System.IO.BinaryReader(stream);

            var header = ReadHeader(reader);
            if (header == null)
            {
                return MeshLoadResult.Failed("Invalid header");
            }

            if (!ValidateHeader(header.Value, out var error))
            {
                return MeshLoadResult.Failed(error);
            }

            var vertices = ReadVertices(reader, stream, header.Value);
            var indices = ReadIndices(reader, stream, header.Value);

            MaterialData? material = null;
            if (header.Value.HasMaterial && header.Value.MaterialOffset > 0)
            {
                material = ReadMaterial(reader, stream, header.Value.MaterialOffset);
            }

            var meshType = header.Value.IsSkinned ? MeshType.Skinned : MeshType.Static;

            return MeshLoadResult.Succeeded(vertices, indices, meshType, material);
        }
        catch (Exception ex)
        {
            return MeshLoadResult.Failed($"Failed to load mesh: {ex.Message}");
        }
    }

    private static unsafe MeshFileHeader? ReadHeader(System.IO.BinaryReader reader)
    {
        var headerBytes = reader.ReadBytes(HeaderNumBytes);
        if (headerBytes.Length < HeaderNumBytes)
        {
            return null;
        }

        fixed (byte* ptr = headerBytes)
        {
            return Marshal.PtrToStructure<MeshFileHeader>((IntPtr)ptr);
        }
    }

    private static bool ValidateHeader(MeshFileHeader header, out string error)
    {
        error = string.Empty;

        var magic = header.GetMagic();
        if (magic != ExpectedMagic)
        {
            error = $"Invalid magic: expected '{ExpectedMagic}', got '{magic}'";
            return false;
        }

        if (header.Version != SupportedVersion)
        {
            error = $"Unsupported version: {header.Version}";
            return false;
        }

        return true;
    }

    private static Vertex[] ReadVertices(System.IO.BinaryReader reader, Stream stream, MeshFileHeader header)
    {
        stream.Seek((long)header.VertexDataOffset, SeekOrigin.Begin);

        var vertices = new Vertex[header.VertexCount];
        var vertexBytes = reader.ReadBytes((int)header.VertexDataNumBytes);

        var handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        try
        {
            Marshal.Copy(vertexBytes, 0, handle.AddrOfPinnedObject(), vertexBytes.Length);
        }
        finally
        {
            handle.Free();
        }

        return vertices;
    }

    private static uint[] ReadIndices(System.IO.BinaryReader reader, Stream stream, MeshFileHeader header)
    {
        stream.Seek((long)header.IndexDataOffset, SeekOrigin.Begin);

        var indices = new uint[header.IndexCount];
        var indexBytes = reader.ReadBytes((int)header.IndexDataNumBytes);

        System.Buffer.BlockCopy(indexBytes, 0, indices, 0, indexBytes.Length);

        return indices;
    }

    private static MaterialData? ReadMaterial(System.IO.BinaryReader reader, Stream stream, ulong offset)
    {
        stream.Seek((long)offset, SeekOrigin.Begin);

        var nameLength = reader.ReadUInt16();
        var name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

        var baseColorR = reader.ReadSingle();
        var baseColorG = reader.ReadSingle();
        var baseColorB = reader.ReadSingle();
        var baseColorA = reader.ReadSingle();
        var metallic = reader.ReadSingle();
        var roughness = reader.ReadSingle();

        var baseColorTexture = ReadLengthPrefixedString(reader);
        var normalTexture = ReadLengthPrefixedString(reader);
        var metallicRoughnessTexture = ReadLengthPrefixedString(reader);

        return new MaterialData
        {
            Name = name,
            BaseColor = new Vector4(baseColorR, baseColorG, baseColorB, baseColorA),
            Metallic = metallic,
            Roughness = roughness,
            BaseColorTexturePath = baseColorTexture,
            NormalTexturePath = normalTexture,
            MetallicRoughnessTexturePath = metallicRoughnessTexture
        };
    }

    private static string? ReadLengthPrefixedString(System.IO.BinaryReader reader)
    {
        var length = reader.ReadUInt16();
        if (length == 0)
        {
            return null;
        }

        return System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
    }
}
