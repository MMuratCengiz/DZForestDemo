using System.Numerics;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Light;

public class DirectionalLight(string name) : GameObject(name)
{
    [Color] public Vector3 Color { get; set; } = new(1.0f, 1.0f, 1.0f);
    public float Intensity { get; set; } = 1.0f;
    public bool CastsShadows { get; set; } = true;

    [HideInInspector] public Vector3 Direction
    {
        get
        {
            var forward = Vector3.Transform(Vector3.UnitZ, LocalRotation);
            return Vector3.Normalize(forward);
        }
    }

    public DirectionalLight() : this("DirectionalLight")
    {
    }

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
