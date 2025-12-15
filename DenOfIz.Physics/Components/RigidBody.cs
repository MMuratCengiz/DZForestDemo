using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;

namespace Physics.Components;

public struct RigidBody
{
    public BodyHandle Handle;
    public bool IsStatic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RigidBody(BodyHandle handle, bool isStatic = false)
    {
        Handle = handle;
        IsStatic = isStatic;
    }
}

public struct StaticBody
{
    public StaticHandle Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StaticBody(StaticHandle handle)
    {
        Handle = handle;
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

    public static ColliderDesc Box(Vector3 size, float mass = 1f) => new()
    {
        Shape = ColliderShape.Box,
        Size = size,
        Mass = mass,
        IsStatic = false
    };

    public static ColliderDesc StaticBox(Vector3 size) => new()
    {
        Shape = ColliderShape.Box,
        Size = size,
        Mass = 0f,
        IsStatic = true
    };

    public static ColliderDesc Sphere(float radius, float mass = 1f) => new()
    {
        Shape = ColliderShape.Sphere,
        Size = new Vector3(radius, radius, radius),
        Mass = mass,
        IsStatic = false
    };
}
