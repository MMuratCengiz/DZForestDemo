using System.Numerics;
using NiziKit.Physics;

namespace NiziKit.Components;

[NiziComponent]
public partial class SphereCollider : Collider
{
    [JsonProperty("radius")]
    public partial float Radius { get; set; }

    [JsonProperty("isTrigger")]
    public partial bool IsTrigger { get; set; }

    [JsonProperty("center")]
    public partial Vector3 Center { get; set; }

    public override PhysicsShapeType ShapeType => PhysicsShapeType.Sphere;

    public SphereCollider()
    {
        Radius = 0.5f;
    }

    internal override PhysicsShape CreateShape()
    {
        return PhysicsShape.Sphere(Radius * 2f);
    }

    public static SphereCollider Create(float radius)
    {
        return new SphereCollider { Radius = radius };
    }
}
