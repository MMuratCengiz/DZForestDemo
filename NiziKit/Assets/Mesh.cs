using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public enum MeshType : byte
{
    Static = 0,
    Skinned = 1
}

[StructLayout(LayoutKind.Sequential)]
public struct StaticVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;
    public Vector4 Tangent;
}

[StructLayout(LayoutKind.Sequential)]
public struct SkinnedVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;
    public Vector4 Tangent;
    public Vector4 BoneWeights;
    public UInt4 BoneIndices;
}

public class Mesh : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public VertexFormat Format { get; set; } = VertexFormat.Static;
    public BoundingBox Bounds { get; set; }
    public MeshType MeshType { get; set; }
    public int MaterialIndex { get; set; } = -1;

    public byte[]? CpuVertices { get; internal set; }
    public uint[]? CpuIndices { get; internal set; }

    public VertexBufferView VertexBuffer { get; internal set; }
    public IndexBufferView IndexBuffer { get; internal set; }

    public int NumVertices => CpuVertices != null ? CpuVertices.Length / Format.Stride : (int)VertexBuffer.Count;
    public int NumIndices => CpuIndices?.Length ?? (int)IndexBuffer.Count;

    public bool IsUploaded => VertexBuffer.IsValid;

    internal uint Index { get; set; }

    public void Dispose()
    {
    }
}
