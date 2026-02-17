using System.Numerics;
using BepuPhysics;
using NiziKit.Application.Timing;
using NiziKit.Physics;

namespace NiziKit.Components;

public partial class Rigidbody : NiziComponent
{
    [JsonProperty("bodyType")]
    public partial PhysicsBodyType BodyType { get; set; }

    [JsonProperty("mass")]
    public partial float Mass { get; set; }

    [JsonProperty("speculativeMargin")]
    public partial float SpeculativeMargin { get; set; }

    [JsonProperty("sleepThreshold")]
    public partial float SleepThreshold { get; set; }

    internal BodyHandle? BodyHandle { get; set; }
    internal StaticHandle? StaticHandle { get; set; }

    [HideInInspector]
    public bool IsRegistered => BodyHandle.HasValue || StaticHandle.HasValue;
    [HideInInspector]
    public bool IsDynamic => BodyType == PhysicsBodyType.Dynamic;
    [HideInInspector]
    public bool IsStatic => BodyType == PhysicsBodyType.Static;
    [HideInInspector]
    public bool IsKinematic => BodyType == PhysicsBodyType.Kinematic;

    [HideInInspector]
    public Vector3 Velocity
    {
        get => IsRegistered ? Core.World.PhysicsWorld.GetVelocity(Owner!.Id) : Vector3.Zero;
        set
        {
            if (IsRegistered)
            {
                Core.World.PhysicsWorld.SetVelocity(Owner!.Id, value);
            }
        }
    }

    [HideInInspector]
    public Vector3 AngularVelocity
    {
        get => IsRegistered ? Core.World.PhysicsWorld.GetAngularVelocity(Owner!.Id) : Vector3.Zero;
        set
        {
            if (IsRegistered)
            {
                Core.World.PhysicsWorld.SetAngularVelocity(Owner!.Id, value);
            }
        }
    }

    [HideInInspector]
    public Vector3 Position
    {
        get
        {
            if (!IsRegistered || Owner == null)
            {
                return Vector3.Zero;
            }

            var pose = Core.World.PhysicsWorld.GetPose(Owner.Id);
            return pose?.Position ?? Owner.LocalPosition;
        }
    }

    [HideInInspector]
    public Quaternion Rotation
    {
        get
        {
            if (!IsRegistered || Owner == null)
            {
                return Quaternion.Identity;
            }

            var pose = Core.World.PhysicsWorld.GetPose(Owner.Id);
            return pose?.Rotation ?? Owner.LocalRotation;
        }
    }

    [HideInInspector]
    public bool IsSleeping => IsRegistered && Core.World.PhysicsWorld.IsSleeping(Owner!.Id);

    public Rigidbody()
    {
        Mass = 1f;
        SpeculativeMargin = 0.1f;
        SleepThreshold = 0.01f;
    }

    public void AddForce(Vector3 force)
    {
        if (!IsRegistered)
        {
            return;
        }

        Core.World.PhysicsWorld.ApplyImpulse(Owner!.Id, force * Time.DeltaTime);
    }

    public void AddForce(Vector3 force, ForceMode mode)
    {
        if (!IsRegistered)
        {
            return;
        }

        var id = Owner!.Id;
        var world = Core.World.PhysicsWorld;
        switch (mode)
        {
            case ForceMode.Force:
                world.ApplyImpulse(id, force * Time.DeltaTime);
                break;
            case ForceMode.Impulse:
                world.ApplyImpulse(id, force);
                break;
            case ForceMode.Acceleration:
                world.ApplyImpulse(id, force * Mass * Time.DeltaTime);
                break;
            case ForceMode.VelocityChange:
                world.SetVelocity(id, world.GetVelocity(id) + force);
                break;
        }
    }

    public void AddForceAtPosition(Vector3 force, Vector3 worldPoint)
    {
        if (!IsRegistered)
        {
            return;
        }

        Core.World.PhysicsWorld.ApplyImpulse(Owner!.Id, force * Time.DeltaTime, worldPoint);
    }

    public void AddTorque(Vector3 torque)
    {
        if (!IsRegistered)
        {
            return;
        }

        Core.World.PhysicsWorld.ApplyAngularImpulse(Owner!.Id, torque * Time.DeltaTime);
    }

    public void AddTorque(Vector3 torque, ForceMode mode)
    {
        if (!IsRegistered)
        {
            return;
        }

        var id = Owner!.Id;
        var world = Core.World.PhysicsWorld;
        switch (mode)
        {
            case ForceMode.Force:
                world.ApplyAngularImpulse(id, torque * Time.DeltaTime);
                break;
            case ForceMode.Impulse:
                world.ApplyAngularImpulse(id, torque);
                break;
            case ForceMode.Acceleration:
                world.ApplyAngularImpulse(id, torque * Mass * Time.DeltaTime);
                break;
            case ForceMode.VelocityChange:
                world.SetAngularVelocity(id, world.GetAngularVelocity(id) + torque);
                break;
        }
    }

    public void MovePosition(Vector3 position)
    {
        if (Owner != null)
        {
            Owner.LocalPosition = position;
        }
    }

    public void MoveRotation(Quaternion rotation)
    {
        if (Owner != null)
        {
            Owner.LocalRotation = rotation;
        }
    }

    public void WakeUp()
    {
        if (IsRegistered)
        {
            Core.World.PhysicsWorld.Awake(Owner!.Id);
        }
    }

    public static Rigidbody Dynamic(float mass = 1f)
    {
        return new Rigidbody
        {
            BodyType = PhysicsBodyType.Dynamic,
            Mass = mass
        };
    }

    public static Rigidbody Static()
    {
        return new Rigidbody
        {
            BodyType = PhysicsBodyType.Static,
            Mass = 0f,
            SleepThreshold = 0f
        };
    }

    public static Rigidbody Kinematic()
    {
        return new Rigidbody
        {
            BodyType = PhysicsBodyType.Kinematic,
            Mass = 0f,
            SleepThreshold = 0f
        };
    }

    internal PhysicsBodyDesc ToBodyDesc(PhysicsShape shape)
    {
        return new PhysicsBodyDesc
        {
            Shape = shape,
            BodyType = BodyType,
            Mass = Mass,
            SpeculativeMargin = SpeculativeMargin,
            SleepThreshold = SleepThreshold
        };
    }
}
