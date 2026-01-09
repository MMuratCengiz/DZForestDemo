using System.Numerics;
using NiziKit.SceneManagement;

namespace NiziKit.Light;

public class SpotLight : GameObject
{
    public Vector3 Color { get; set; } = new(1.0f, 1.0f, 1.0f);
    public float Intensity { get; set; } = 1.0f;
    public float Range { get; set; } = 10.0f;
    public float InnerConeAngle { get; set; } = 0.3f;
    public float OuterConeAngle { get; set; } = 0.5f;
    public bool CastsShadows { get; set; } = false;

    /// <summary>
    /// Direction is derived from the object's forward vector (rotation).
    /// </summary>
    public Vector3 Direction
    {
        get
        {
            var forward = Vector3.Transform(Vector3.UnitZ, (Quaternion)LocalRotation);
            return Vector3.Normalize(forward);
        }
    }

    public SpotLight() : base("SpotLight")
    {
    }

    public SpotLight(string name) : base(name)
    {
    }

    /// <summary>
    /// Sets the light direction by calculating the required rotation.
    /// </summary>
    public void LookAt(Vector3 direction)
    {
        direction = Vector3.Normalize(direction);

        var dot = Vector3.Dot(Vector3.UnitZ, direction);

        if (dot > 0.9999f)
        {
            LocalRotation = Quaternion.Identity;
        }
        else if (dot < -0.9999f)
        {
            LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        }
        else
        {
            var axis = Vector3.Cross(Vector3.UnitZ, direction);
            var angle = MathF.Acos(dot);
            LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
        }
    }
}
