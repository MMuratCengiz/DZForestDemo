using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public static class PhysicsConstants
{
    public const int MaxContactPoints = 4;
}

[StructLayout(LayoutKind.Sequential)]
public struct ContactPoint
{
    public Vector3 Point;
    public Vector3 Normal;
    public float Separation;
}

[InlineArray(PhysicsConstants.MaxContactPoints)]
public struct ContactPointBuffer
{
    private ContactPoint _element0;
}

public ref struct Collision
{
    public GameObject Other;
    public Rigidbody? Rigidbody;
    public Vector3 RelativeVelocity;
    public int ContactCount;
    public ContactPointBuffer Contacts;

    public readonly ReadOnlySpan<ContactPoint> GetContacts()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Contacts[0]), ContactCount);
    }
}

public struct CollisionData
{
    public int OtherId;
    public int RigidbodyOwnerId;
    public Vector3 RelativeVelocity;
    public int ContactCount;
    public ContactPointBuffer Contacts;
}
