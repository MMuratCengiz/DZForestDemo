using System.Text.Json.Serialization;

namespace RuntimeAssets.GltfModels;

[JsonConverter(typeof(JsonNumberEnumConverter<GltfPrimitiveMode>))]
public enum GltfPrimitiveMode
{
    Points = 0,
    Lines = 1,
    LineLoop = 2,
    LineStrip = 3,
    Triangles = 4,
    TriangleStrip = 5,
    TriangleFan = 6
}