using System.Numerics;

namespace DZForestDemo;

public struct DirectionalLight(Vector3 direction, Vector3? color = null, float intensity = 1.0f)
{
    public Vector3 Direction = Vector3.Normalize(direction);
    public readonly float Intensity = intensity;
    public Vector3 Color = color ?? Vector3.One;
    public readonly bool CastShadows = true;

    public static DirectionalLight Sun => new(
        new Vector3(0.5f, -1.0f, 0.3f),
        new Vector3(1.0f, 0.98f, 0.95f)
    );
}

public struct PointLight(Vector3? color = null, float intensity = 1.0f, float radius = 10.0f)
{
    public Vector3 Color = color ?? Vector3.One;
    public readonly float Intensity = intensity;
    public readonly float Radius = radius;
    public float Falloff = 2.0f;
}

public struct SpotLight(
    Vector3 direction,
    Vector3? color = null,
    float intensity = 1.0f,
    float radius = 15.0f,
    float innerAngleDegrees = 25f,
    float outerAngleDegrees = 35f)
{
    public Vector3 Direction = Vector3.Normalize(direction);
    public Vector3 Color = color ?? Vector3.One;
    public readonly float Intensity = intensity;
    public readonly float Radius = radius;
    public readonly float InnerConeAngle = innerAngleDegrees * MathF.PI / 180f;
    public readonly float OuterConeAngle = outerAngleDegrees * MathF.PI / 180f;
}

public struct AmbientLight(Vector3? skyColor = null, Vector3? groundColor = null, float intensity = 0.3f)
{
    public Vector3 SkyColor = skyColor ?? new Vector3(0.4f, 0.5f, 0.6f);
    public Vector3 GroundColor = groundColor ?? new Vector3(0.2f, 0.18f, 0.15f);
    public readonly float Intensity = intensity;

    public static AmbientLight Default => new();
}