using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using NiziKit.Physics;

namespace NiziKit.Components;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct RigidBody(BodyHandle handle, bool isStatic = false)
{
    public BodyHandle Handle = handle;
    public readonly bool IsStatic = isStatic;
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct StaticBody(StaticHandle handle)
{
    public StaticHandle Handle = handle;
}

[NiziComponent]
public partial class RigidbodyComponent
{
    public partial PhysicsShape Shape { get; set; }
    public partial PhysicsBodyType BodyType { get; set; }
    public partial float Mass { get; set; }
    public partial float SpeculativeMargin { get; set; }
    public partial float SleepThreshold { get; set; }

    internal BodyHandle? BodyHandle { get; set; }
    internal StaticHandle? StaticHandle { get; set; }

    public bool IsRegistered => BodyHandle.HasValue || StaticHandle.HasValue;

    public static RigidbodyComponent Dynamic(PhysicsShape shape, float mass = 1f)
    {
        return new RigidbodyComponent
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Dynamic,
            Mass = mass
        };
    }

    public static RigidbodyComponent Static(PhysicsShape shape)
    {
        return new RigidbodyComponent
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Static,
            Mass = 0f,
            SleepThreshold = 0f
        };
    }

    public static RigidbodyComponent Kinematic(PhysicsShape shape)
    {
        return new RigidbodyComponent
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Kinematic,
            Mass = 0f,
            SleepThreshold = 0f
        };
    }

    internal PhysicsBodyDesc ToBodyDesc()
    {
        return new PhysicsBodyDesc
        {
            Shape = Shape,
            BodyType = BodyType,
            Mass = Mass,
            SpeculativeMargin = SpeculativeMargin,
            SleepThreshold = SleepThreshold
        };
    }
}

public enum ColliderShape
{
    Box,
    Sphere,
    Capsule
}

public struct ColliderDesc
{
    public ColliderShape Shape;
    public Vector3 Size;
    public float Mass;
    public bool IsStatic;

    public static ColliderDesc Box(Vector3 size, float mass = 1f)
    {
        return new ColliderDesc
        {
            Shape = ColliderShape.Box,
            Size = size,
            Mass = mass,
            IsStatic = false
        };
    }

    public static ColliderDesc StaticBox(Vector3 size)
    {
        return new ColliderDesc
        {
            Shape = ColliderShape.Box,
            Size = size,
            Mass = 0f,
            IsStatic = true
        };
    }

    public static ColliderDesc Sphere(float radius, float mass = 1f)
    {
        return new ColliderDesc
        {
            Shape = ColliderShape.Sphere,
            Size = new Vector3(radius, radius, radius),
            Mass = mass,
            IsStatic = false
        };
    }
}