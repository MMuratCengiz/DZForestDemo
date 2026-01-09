using System.Runtime.CompilerServices;

namespace DenOfIz.World.Components;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct ContactMaterial(float friction = 0.5f, float restitution = 0.3f)
{
    public float Friction = friction;
    public float Restitution = restitution;
    public float SpringFrequency = 30f;
    public float SpringDamping = 1f;

    public static ContactMaterial Default => new()
    {
        Friction = 0.5f,
        Restitution = 0.3f,
        SpringFrequency = 30f,
        SpringDamping = 1f
    };

    public static ContactMaterial Bouncy => new()
    {
        Friction = 0.3f,
        Restitution = 0.9f,
        SpringFrequency = 30f,
        SpringDamping = 1f
    };

    public static ContactMaterial Ice => new()
    {
        Friction = 0.05f,
        Restitution = 0.1f,
        SpringFrequency = 30f,
        SpringDamping = 1f
    };

    public static ContactMaterial Rubber => new()
    {
        Friction = 0.9f,
        Restitution = 0.8f,
        SpringFrequency = 30f,
        SpringDamping = 1f
    };
}