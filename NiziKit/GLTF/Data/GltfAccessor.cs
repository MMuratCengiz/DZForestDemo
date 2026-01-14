using System.Text.Json.Serialization;

namespace NiziKit.GLTF.Data;

public sealed class GltfAccessor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bufferView")]
    public int? BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("componentType")]
    public int ComponentType { get; set; }

    [JsonPropertyName("normalized")]
    public bool Normalized { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "SCALAR";

    [JsonPropertyName("max")]
    public float[]? Max { get; set; }

    [JsonPropertyName("min")]
    public float[]? Min { get; set; }

    [JsonPropertyName("sparse")]
    public GltfSparse? Sparse { get; set; }
}

public sealed class GltfSparse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("indices")]
    public GltfSparseIndices Indices { get; set; } = new();

    [JsonPropertyName("values")]
    public GltfSparseValues Values { get; set; } = new();
}

public sealed class GltfSparseIndices
{
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }

    [JsonPropertyName("componentType")]
    public int ComponentType { get; set; }
}

public sealed class GltfSparseValues
{
    [JsonPropertyName("bufferView")]
    public int BufferView { get; set; }

    [JsonPropertyName("byteOffset")]
    public int ByteOffset { get; set; }
}

public static class GltfComponentType
{
    public const int Byte = 5120;
    public const int UnsignedByte = 5121;
    public const int Short = 5122;
    public const int UnsignedShort = 5123;
    public const int UnsignedInt = 5125;
    public const int Float = 5126;

    public static int GetSize(int componentType) => componentType switch
    {
        Byte or UnsignedByte => 1,
        Short or UnsignedShort => 2,
        UnsignedInt or Float => 4,
        _ => throw new NotSupportedException($"Unknown component type: {componentType}")
    };
}

public static class GltfAccessorType
{
    public const string Scalar = "SCALAR";
    public const string Vec2 = "VEC2";
    public const string Vec3 = "VEC3";
    public const string Vec4 = "VEC4";
    public const string Mat2 = "MAT2";
    public const string Mat3 = "MAT3";
    public const string Mat4 = "MAT4";

    public static int GetComponentCount(string type) => type switch
    {
        Scalar => 1,
        Vec2 => 2,
        Vec3 => 3,
        Vec4 => 4,
        Mat2 => 4,
        Mat3 => 9,
        Mat4 => 16,
        _ => throw new NotSupportedException($"Unknown accessor type: {type}")
    };
}
