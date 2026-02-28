using System.Numerics;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Light;

public class PointLight(string name) : GameObject(name)
{
    [Color] public Vector3 Color { get; set; } = new(1.0f, 1.0f, 1.0f);
    public float Intensity { get; set; } = 1.0f;
    public float Range { get; set; } = 10.0f;
    public bool CastsShadows { get; set; } = false;

    public PointLight() : this("PointLight")
    {
    }
}
