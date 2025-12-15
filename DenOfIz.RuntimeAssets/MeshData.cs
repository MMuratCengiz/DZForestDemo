using System.Numerics;
using System.Runtime.InteropServices;

namespace RuntimeAssets;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;
    public Vector4 Tangent;
    public Vector4 BoneWeights;
    public UInt4 BoneIndices;
}

[StructLayout(LayoutKind.Sequential)]
public struct UInt4
{
    public uint X;
    public uint Y;
    public uint Z;
    public uint W;
}

public sealed class MeshPrimitive
{
    public required Vertex[] Vertices { get; init; }
    public required uint[] Indices { get; init; }
    public int MaterialIndex { get; init; } = -1;
}

public sealed class MeshData
{
    public required string Name { get; init; }
    public required IReadOnlyList<MeshPrimitive> Primitives { get; init; }
}
