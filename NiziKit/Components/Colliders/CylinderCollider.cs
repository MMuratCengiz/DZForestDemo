using System.Numerics;
using NiziKit.Physics;

namespace NiziKit.Components;

[NiziComponent]
public partial class CylinderCollider : Collider
{
    [JsonProperty("radius")]
    public partial float Radius { get; set; }

    [JsonProperty("height")]
    public partial float Height { get; set; }

    [JsonProperty("direction")]
    public partial ColliderDirection Direction { get; set; }

    [JsonProperty("isTrigger")]
    public partial bool IsTrigger { get; set; }

    [JsonProperty("center")]
    public partial Vector3 Center { get; set; }

    public override PhysicsShapeType ShapeType => PhysicsShapeType.Cylinder;

    public CylinderCollider()
    {
        Radius = 0.5f;
        Height = 2f;
        Direction = ColliderDirection.Y;
    }

    internal override PhysicsShape CreateShape()
    {
        return PhysicsShape.Cylinder(Radius * 2f, Height);
    }

    public static CylinderCollider Create(float radius, float height, ColliderDirection direction = ColliderDirection.Y)
    {
        return new CylinderCollider
        {
            Radius = radius,
            Height = height,
            Direction = direction
        };
    }
}
