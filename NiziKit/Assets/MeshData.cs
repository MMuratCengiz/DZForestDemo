using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.Assets;

/// <summary>
/// Mesh type determines which pipeline variant to use for rendering.
/// </summary>
public enum MeshType : byte
{
    /// <summary>
    /// Static mesh with full vertex data but no animation.
    /// Uses Vertex struct (Position, Normal, TexCoord, Tangent).
    /// Also used for built-in geometry primitives (box, sphere, quad).
    /// </summary>
    Static = 0,

    /// <summary>
    /// Skinned mesh with bone weights and indices for skeletal animation.
    /// Uses full Vertex struct including BoneWeights and BoneIndices.
    /// </summary>
    Skinned = 1
}

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