using System.Numerics;

namespace DZForestDemo;

public struct DirectionalLight
{
    public Vector3 Direction;
    public float Intensity;
    public Vector3 Color;
    public bool CastShadows;

    public DirectionalLight(Vector3 direction, Vector3? color = null, float intensity = 1.0f)
    {
        Direction = Vector3.Normalize(direction);
        Color = color ?? Vector3.One;
        Intensity = intensity;
        CastShadows = true;
    }

    public static DirectionalLight Sun => new(
        new Vector3(0.5f, -1.0f, 0.3f),
        new Vector3(1.0f, 0.98f, 0.95f),
        1.0f
    );
}

public struct PointLight
{
    public Vector3 Color;
    public float Intensity;
    public float Radius;
    public float Falloff;

    public PointLight(Vector3? color = null, float intensity = 1.0f, float radius = 10.0f)
    {
        Color = color ?? Vector3.One;
        Intensity = intensity;
        Radius = radius;
        Falloff = 2.0f;
    }
}

public struct SpotLight
{
    public Vector3 Direction;
    public Vector3 Color;
    public float Intensity;
    public float Radius;
    public float InnerConeAngle;
    public float OuterConeAngle;

    public SpotLight(Vector3 direction, Vector3? color = null, float intensity = 1.0f,
        float radius = 15.0f, float innerAngleDegrees = 25f, float outerAngleDegrees = 35f)
    {
        Direction = Vector3.Normalize(direction);
        Color = color ?? Vector3.One;
        Intensity = intensity;
        Radius = radius;
        InnerConeAngle = innerAngleDegrees * MathF.PI / 180f;
        OuterConeAngle = outerAngleDegrees * MathF.PI / 180f;
    }
}

public struct AmbientLight
{
    public Vector3 SkyColor;
    public Vector3 GroundColor;
    public float Intensity;

    public AmbientLight(Vector3? skyColor = null, Vector3? groundColor = null, float intensity = 0.3f)
    {
        SkyColor = skyColor ?? new Vector3(0.4f, 0.5f, 0.6f);
        GroundColor = groundColor ?? new Vector3(0.2f, 0.18f, 0.15f);
        Intensity = intensity;
    }

    public static AmbientLight Default => new();
}
