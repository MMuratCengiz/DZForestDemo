using System.Numerics;

namespace DenOfIz.World.Assets;

public class Material
{
    public string Name { get; set; } = string.Empty;
    public Vector4 BaseColor { get; set; } = Vector4.One;
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 1.0f;
    public Texture? AlbedoTexture { get; set; }
    public Texture? NormalTexture { get; set; }
    public Texture? MetallicRoughnessTexture { get; set; }
}
