using BepuPhysics;
using BepuPhysics.Collidables;
using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Components;

public abstract class Collider : IComponent
{
    public virtual GameObject? Owner { get; set; }

    internal BodyHandle? BodyHandle { get; set; }
    internal StaticHandle? StaticHandle { get; set; }
    internal TypedIndex? ShapeIndex { get; set; }

    public bool IsRegistered => BodyHandle.HasValue || StaticHandle.HasValue;

    internal abstract PhysicsShape CreateShape();

    public abstract PhysicsShapeType ShapeType { get; }
}
