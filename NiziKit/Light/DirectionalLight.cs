using System.Numerics;
using NiziKit.SceneManagement;

namespace NiziKit.Light;

public class DirectionalLight : GameObject
{
    public Vector3 Color { get; set; } = new(1.0f, 1.0f, 1.0f);
    public float Intensity { get; set; } = 1.0f;
    public bool CastsShadows { get; set; } = true;

    /// <summary>
    /// Direction is derived from the object's forward vector (rotation).
    /// Returns normalized direction the light is pointing.
    /// </summary>
    public Vector3 Direction
    {
        get
        {
            // Forward direction transformed by rotation
            var forward = Vector3.Transform(Vector3.UnitZ, LocalRotation);
            return Vector3.Normalize(forward);
        }
    }

    public DirectionalLight() : base("DirectionalLight")
    {
    }

    public DirectionalLight(string name) : base(name)
    {
    }

    /// <summary>
    /// Sets the light direction by calculating the required rotation.
    /// </summary>
    public void LookAt(Vector3 direction)
    {
        direction = Vector3.Normalize(direction);

        // Calculate rotation from forward (0,0,1) to target direction
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
