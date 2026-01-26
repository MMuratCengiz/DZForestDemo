using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;

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
    public Matrix4x4[]? InverseBindMatrices { get; set; }
    public Matrix4x4 NodeTransform { get; set; } = Matrix4x4.Identity;

    public byte[]? CpuVertices { get; internal set; }
    public uint[]? CpuIndices { get; internal set; }

    public VertexBufferView VertexBuffer { get; internal set; }
    public IndexBufferView IndexBuffer { get; internal set; }

    public MeshAttributeSet? SourceAttributes { get; set; }

    private readonly Dictionary<string, VertexBufferView> _formatVariants = new();

    public int NumVertices => SourceAttributes?.VertexCount ??
                              (CpuVertices != null ? CpuVertices.Length / Format.Stride : (int)VertexBuffer.Count);
    public int NumIndices => SourceAttributes?.Indices.Length ?? CpuIndices?.Length ?? (int)IndexBuffer.Count;

    public bool IsUploaded => VertexBuffer.IsValid;
    public bool HasSourceData => SourceAttributes != null;

    internal uint Index { get; set; }

    public VertexBufferView GetVertexBuffer(VertexFormat format)
    {
        if (_formatVariants.TryGetValue(format.Name, out var cached))
        {
            return cached;
        }

        if (format.Name == Format.Name && VertexBuffer.IsValid)
        {
            return VertexBuffer;
        }

        if (SourceAttributes == null)
        {
            if (VertexBuffer.IsValid)
            {
                return VertexBuffer;
            }
            throw new InvalidOperationException($"Cannot create vertex buffer variant for format '{format.Name}': source data has been discarded");
        }

        var packed = VertexPacker.Pack(SourceAttributes, format);
        var view = Assets.UploadVertices(packed, format);
        _formatVariants[format.Name] = view;
        return view;
    }

    public void DiscardSourceData()
    {
        SourceAttributes = null;
    }

    public void Dispose()
    {
    }
}
