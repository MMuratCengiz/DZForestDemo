using System.Numerics;

namespace DZForestDemo;

public struct StandardMaterial
{
    public Vector4 BaseColor;
    public float Metallic;
    public float Roughness;
    public float AmbientOcclusion;
    private float _padding;

    public StandardMaterial()
    {
        BaseColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
        Metallic = 0.0f;
        Roughness = 0.5f;
        AmbientOcclusion = 1.0f;
        _padding = 0;
    }

    public static StandardMaterial FromColor(Vector3 color) => new()
    {
        BaseColor = new Vector4(color, 1.0f)
    };

    public static StandardMaterial FromColor(Vector4 color) => new()
    {
        BaseColor = color
    };

    public static StandardMaterial FromColor(float r, float g, float b, float a = 1.0f) => new()
    {
        BaseColor = new Vector4(r, g, b, a)
    };
}

public struct Unlit;

public struct Wireframe;

public struct NoShadow;

public static class Materials
{
    public static StandardMaterial Default => new();

    public static StandardMaterial Red => StandardMaterial.FromColor(0.9f, 0.2f, 0.2f);
    public static StandardMaterial Green => StandardMaterial.FromColor(0.2f, 0.8f, 0.3f);
    public static StandardMaterial Blue => StandardMaterial.FromColor(0.2f, 0.4f, 0.9f);
    public static StandardMaterial Yellow => StandardMaterial.FromColor(0.95f, 0.85f, 0.2f);
    public static StandardMaterial Orange => StandardMaterial.FromColor(0.95f, 0.5f, 0.15f);
    public static StandardMaterial Purple => StandardMaterial.FromColor(0.6f, 0.2f, 0.8f);
    public static StandardMaterial Cyan => StandardMaterial.FromColor(0.2f, 0.85f, 0.9f);
    public static StandardMaterial White => StandardMaterial.FromColor(0.95f, 0.95f, 0.95f);
    public static StandardMaterial Black => StandardMaterial.FromColor(0.05f, 0.05f, 0.05f);
    public static StandardMaterial Gray => StandardMaterial.FromColor(0.5f, 0.5f, 0.5f);

    public static StandardMaterial Wood => new()
    {
        BaseColor = new Vector4(0.55f, 0.35f, 0.2f, 1.0f),
        Roughness = 0.8f,
        Metallic = 0.0f
    };

    public static StandardMaterial Metal => new()
    {
        BaseColor = new Vector4(0.8f, 0.8f, 0.85f, 1.0f),
        Roughness = 0.3f,
        Metallic = 0.9f
    };

    public static StandardMaterial Plastic => new()
    {
        BaseColor = new Vector4(0.9f, 0.1f, 0.1f, 1.0f),
        Roughness = 0.4f,
        Metallic = 0.0f
    };

    public static StandardMaterial Concrete => new()
    {
        BaseColor = new Vector4(0.6f, 0.6f, 0.58f, 1.0f),
        Roughness = 0.95f,
        Metallic = 0.0f
    };

    public static StandardMaterial Rubber => new()
    {
        BaseColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f),
        Roughness = 0.9f,
        Metallic = 0.0f
    };

    public static StandardMaterial Gold => new()
    {
        BaseColor = new Vector4(1.0f, 0.84f, 0.0f, 1.0f),
        Roughness = 0.2f,
        Metallic = 1.0f
    };

    public static StandardMaterial Copper => new()
    {
        BaseColor = new Vector4(0.95f, 0.64f, 0.54f, 1.0f),
        Roughness = 0.25f,
        Metallic = 1.0f
    };

    public static StandardMaterial Grass => new()
    {
        BaseColor = new Vector4(0.3f, 0.55f, 0.2f, 1.0f),
        Roughness = 0.85f,
        Metallic = 0.0f
    };
}
