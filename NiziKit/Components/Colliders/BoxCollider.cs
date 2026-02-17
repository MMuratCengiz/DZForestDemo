using System.Numerics;
using NiziKit.Physics;

namespace NiziKit.Components;

public partial class BoxCollider : Collider
{
    [JsonProperty("size")]
    public partial Vector3 Size { get; set; }

    [JsonProperty("isTrigger")]
    public partial bool IsTrigger { get; set; }

    [JsonProperty("center")]
    public partial Vector3 Center { get; set; }

    public override PhysicsShapeType ShapeType => PhysicsShapeType.Box;

    public BoxCollider()
    {
        Size = Vector3.One;
    }

    internal override PhysicsShape CreateShape()
    {
        return PhysicsShape.Box(Size);
    }

    public static BoxCollider Create(float width, float height, float depth)
    {
        return new BoxCollider { Size = new Vector3(width, height, depth) };
    }

    public static BoxCollider Create(Vector3 size)
    {
        return new BoxCollider { Size = size };
    }

    public static BoxCollider Cube(float size)
    {
        return new BoxCollider { Size = new Vector3(size, size, size) };
    }
}
