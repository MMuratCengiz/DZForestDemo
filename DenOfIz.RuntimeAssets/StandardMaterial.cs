using System.Numerics;

namespace RuntimeAssets;

public struct StandardMaterial()
{
    public Vector4 BaseColor = new(0.8f, 0.8f, 0.8f, 1.0f);
    public float Metallic = 0.0f;
    public float Roughness = 0.5f;
    public float AmbientOcclusion = 1.0f;
    public RuntimeTextureHandle AlbedoTexture = RuntimeTextureHandle.Invalid;

    public static StandardMaterial FromColor(Vector3 color)
    {
        return new StandardMaterial
        {
            BaseColor = new Vector4(color, 1.0f)
        };
    }

    public static StandardMaterial FromColor(Vector4 color)
    {
        return new StandardMaterial
        {
            BaseColor = color
        };
    }

    public static StandardMaterial FromColor(float r, float g, float b, float a = 1.0f)
    {
        return new StandardMaterial
        {
            BaseColor = new Vector4(r, g, b, a)
        };
    }
}
