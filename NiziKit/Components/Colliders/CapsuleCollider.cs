using System.Numerics;
using NiziKit.Physics;

namespace NiziKit.Components;

public enum ColliderDirection
{
    X = 0,
    Y = 1,
    Z = 2
}

[NiziComponent]
public partial class CapsuleCollider : Collider
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

    public override PhysicsShapeType ShapeType => PhysicsShapeType.Capsule;

    public CapsuleCollider()
    {
        Radius = 0.5f;
        Height = 2f;
        Direction = ColliderDirection.Y;
    }

    internal override PhysicsShape CreateShape()
    {
        float cylinderLength = Math.Max(0, Height - Radius * 2f);
        return PhysicsShape.Capsule(Radius, cylinderLength);
    }

    public static CapsuleCollider Create(float radius, float height, ColliderDirection direction = ColliderDirection.Y)
    {
        return new CapsuleCollider
        {
            Radius = radius,
            Height = height,
            Direction = direction
        };
    }
}
