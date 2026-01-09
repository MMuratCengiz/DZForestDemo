using System.Runtime.InteropServices;

namespace DenOfIz.World.Offline;

public static class MeshFormat
{
    public const string Magic = "DZMESH";
    public const ushort CurrentVersion = 1;
    public const int HeaderNumBytes = 64;
    public const int VertexNumBytes = 80;
}

[Flags]
public enum MeshFlags : uint
{
    None = 0,
    IsSkinned = 1 << 0,
    HasMaterial = 1 << 1
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MeshHeader
{
    public unsafe fixed byte Magic[6];
    public ushort Version;
    public MeshFlags Flags;
    public uint VertexCount;
    public uint IndexCount;
    public ulong VertexDataOffset;
    public ulong VertexDataNumBytes;
    public ulong IndexDataOffset;
    public ulong IndexDataNumBytes;
    public ulong MaterialOffset;
    public uint Reserved;

    public readonly bool IsSkinned => (Flags & MeshFlags.IsSkinned) != 0;
    public readonly bool HasMaterial => (Flags & MeshFlags.HasMaterial) != 0;
}

public sealed class MeshMaterial
{
    public required string Name { get; init; }
    public required float[] BaseColor { get; init; }
    public required float Metallic { get; init; }
    public required float Roughness { get; init; }
    public string? BaseColorTexture { get; init; }
    public string? NormalTexture { get; init; }
    public string? MetallicRoughnessTexture { get; init; }
}
