using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;

namespace Physics.Components;

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