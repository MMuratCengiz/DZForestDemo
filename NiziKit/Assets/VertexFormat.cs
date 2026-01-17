using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Assets;

public enum VertexAttributeType : byte
{
    Float,
    Float2,
    Float3,
    Float4,
    UInt4,
    UByte4
}

public readonly struct VertexAttribute
{
    public string Semantic { get; init; }
    public VertexAttributeType Type { get; init; }
    public int Offset { get; init; }
    public int SemanticIndex { get; init; }

    public int SizeInBytes => Type switch
    {
        VertexAttributeType.Float => 4,
        VertexAttributeType.Float2 => 8,
        VertexAttributeType.Float3 => 12,
        VertexAttributeType.Float4 => 16,
        VertexAttributeType.UInt4 => 16,
        VertexAttributeType.UByte4 => 4,
        _ => throw new ArgumentOutOfRangeException()
    };
}

public sealed class VertexFormat
{
    public string Name { get; }
    public int Stride { get; }
    public IReadOnlyList<VertexAttribute> Attributes { get; }

    private VertexFormat(string name, int stride, VertexAttribute[] attributes)
    {
        Name = name;
        Stride = stride;
        Attributes = attributes;
    }

    public bool HasAttribute(string semantic) =>
        Attributes.Any(a => a.Semantic == semantic);

    public VertexAttribute? GetAttribute(string semantic)
    {
        foreach (var attr in Attributes)
        {
            if (attr.Semantic == semantic)
            {
                return attr;
            }
        }
        return null;
    }

    public static VertexFormat Static { get; } = CreateStatic();
    public static VertexFormat Skinned { get; } = CreateSkinned();

    private static VertexFormat CreateStatic()
    {
        return new VertexFormat("Static", 48,
        [
            new VertexAttribute { Semantic = "POSITION", Type = VertexAttributeType.Float3, Offset = 0 },
            new VertexAttribute { Semantic = "NORMAL", Type = VertexAttributeType.Float3, Offset = 12 },
            new VertexAttribute { Semantic = "TEXCOORD", Type = VertexAttributeType.Float2, Offset = 24, SemanticIndex = 0 },
            new VertexAttribute { Semantic = "TANGENT", Type = VertexAttributeType.Float4, Offset = 32 }
        ]);
    }

    private static VertexFormat CreateSkinned()
    {
        return new VertexFormat("Skinned", 80,
        [
            new VertexAttribute { Semantic = "POSITION", Type = VertexAttributeType.Float3, Offset = 0 },
            new VertexAttribute { Semantic = "NORMAL", Type = VertexAttributeType.Float3, Offset = 12 },
            new VertexAttribute { Semantic = "TEXCOORD", Type = VertexAttributeType.Float2, Offset = 24, SemanticIndex = 0 },
            new VertexAttribute { Semantic = "TANGENT", Type = VertexAttributeType.Float4, Offset = 32 },
            new VertexAttribute { Semantic = "BLENDWEIGHT", Type = VertexAttributeType.Float4, Offset = 48 },
            new VertexAttribute { Semantic = "BLENDINDICES", Type = VertexAttributeType.UInt4, Offset = 64 }
        ]);
    }

    public static VertexFormatBuilder Builder(string name) => new(name);
}

public sealed class VertexFormatBuilder
{
    private readonly string _name;
    private readonly List<VertexAttribute> _attributes = [];
    private int _currentOffset;

    internal VertexFormatBuilder(string name) => _name = name;

    public VertexFormatBuilder Add(string semantic, VertexAttributeType type, int semanticIndex = 0)
    {
        var attr = new VertexAttribute
        {
            Semantic = semantic,
            Type = type,
            Offset = _currentOffset,
            SemanticIndex = semanticIndex
        };
        _attributes.Add(attr);
        _currentOffset += attr.SizeInBytes;
        return this;
    }

    public VertexFormat Build() =>
        (VertexFormat)Activator.CreateInstance(
            typeof(VertexFormat),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [_name, _currentOffset, _attributes.ToArray()],
            null)!;
}
