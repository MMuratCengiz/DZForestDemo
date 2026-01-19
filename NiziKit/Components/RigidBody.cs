using BepuPhysics;
using NiziKit.Physics;

namespace NiziKit.Components;

[NiziComponent(GenerateFactory = false)]
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