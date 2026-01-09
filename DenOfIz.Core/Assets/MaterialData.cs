using System.Numerics;

namespace DenOfIz.World.Assets;

public sealed class MaterialData
{
    public required string Name { get; init; }
    public Vector4 BaseColor { get; init; } = Vector4.One;
    public float Metallic { get; init; }
    public float Roughness { get; init; } = 1.0f;
    public string? BaseColorTexturePath { get; init; }
    public string? NormalTexturePath { get; init; }
    public string? MetallicRoughnessTexturePath { get; init; }
}