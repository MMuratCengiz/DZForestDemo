using DenOfIz;

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

    public bool HasAttribute(string semantic, int semanticIndex) =>
        Attributes.Any(a => a.Semantic == semantic && a.SemanticIndex == semanticIndex);

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

    public VertexAttribute? GetAttribute(string semantic, int semanticIndex)
    {
        foreach (var attr in Attributes)
        {
            if (attr.Semantic == semantic && attr.SemanticIndex == semanticIndex)
            {
                return attr;
            }
        }
        return null;
    }

    public bool IsCompatibleWith(VertexFormat other)
    {
        foreach (var attr in other.Attributes)
        {
            if (!HasAttribute(attr.Semantic, attr.SemanticIndex))
            {
                return false;
            }
        }
        return true;
    }

    public static VertexFormat Static { get; } = CreateStatic();
    public static VertexFormat Skinned { get; } = CreateSkinned();

    private static readonly Dictionary<string, (VertexAttributeType Type, string Semantic, int Index)> GltfAttributeMapping = new()
    {
        ["POSITION"] = (VertexAttributeType.Float3, "POSITION", 0),
        ["NORMAL"] = (VertexAttributeType.Float3, "NORMAL", 0),
        ["TANGENT"] = (VertexAttributeType.Float4, "TANGENT", 0),
        ["TEXCOORD_0"] = (VertexAttributeType.Float2, "TEXCOORD", 0),
        ["TEXCOORD_1"] = (VertexAttributeType.Float2, "TEXCOORD", 1),
        ["TEXCOORD_2"] = (VertexAttributeType.Float2, "TEXCOORD", 2),
        ["TEXCOORD_3"] = (VertexAttributeType.Float2, "TEXCOORD", 3),
        ["COLOR_0"] = (VertexAttributeType.Float4, "COLOR", 0),
        ["JOINTS_0"] = (VertexAttributeType.UInt4, "BLENDINDICES", 0),
        ["WEIGHTS_0"] = (VertexAttributeType.Float4, "BLENDWEIGHT", 0),
    };

    private static readonly string[] AttributeOrder =
    [
        "POSITION", "NORMAL", "TEXCOORD_0", "TANGENT",
        "TEXCOORD_1", "TEXCOORD_2", "TEXCOORD_3",
        "COLOR_0", "WEIGHTS_0", "JOINTS_0"
    ];

    public static VertexFormat FromGltfAttributes(IEnumerable<string> gltfAttributes, string name = "Dynamic")
    {
        var builder = Builder(name);
        var attrSet = new HashSet<string>(gltfAttributes);

        foreach (var gltfAttr in AttributeOrder)
        {
            if (attrSet.Contains(gltfAttr) && GltfAttributeMapping.TryGetValue(gltfAttr, out var mapping))
            {
                builder.Add(mapping.Semantic, mapping.Type, mapping.Index);
            }
        }

        return builder.Build();
    }

    public static VertexFormat FromInputLayout(InputLayoutDesc inputLayoutDesc, string name = "Shader")
    {
        var builder = Builder(name);

        var inputGroups = inputLayoutDesc.InputGroups.ToArray();
        foreach (var group in inputGroups)
        {
            var elements = group.Elements.ToArray();
            foreach (var element in elements)
            {
                var semantic = element.Semantic.ToString();
                var semanticIndex = (int)element.SemanticIndex;
                var attrType = MapFormatToAttributeType(element.Format);
                builder.Add(semantic, attrType, semanticIndex);
            }
        }

        return builder.Build();
    }

    private static VertexAttributeType MapFormatToAttributeType(Format format)
    {
        return format switch
        {
            Format.R32Float => VertexAttributeType.Float,
            Format.R32G32Float => VertexAttributeType.Float2,
            Format.R32G32B32Float => VertexAttributeType.Float3,
            Format.R32G32B32A32Float => VertexAttributeType.Float4,
            Format.R32G32B32A32Uint => VertexAttributeType.UInt4,
            Format.R8G8B8A8Uint => VertexAttributeType.UByte4,
            _ => VertexAttributeType.Float4
        };
    }

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
