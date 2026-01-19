using System.Numerics;
using BepuPhysics;

namespace NiziKit.Components;

public struct SuspensionSpring
{
    public float Frequency;
    public float Damping;
    public float RestLength;

    public static SuspensionSpring Default => new()
    {
        Frequency = 5f,
        Damping = 0.7f,
        RestLength = 0.25f
    };
}

[NiziComponent]
public partial class WheelColliderComponent
{
    public partial float Radius { get; set; }
    public partial float Width { get; set; }
    public partial float Mass { get; set; }
    public partial SuspensionSpring Suspension { get; set; }
    public partial Vector3 Center { get; set; }

    public float MotorTorque { get; set; }
    public float BrakeTorque { get; set; }
    public float SteerAngle { get; set; }

    internal BodyHandle? WheelBodyHandle { get; set; }
    internal BodyHandle? ConnectedBodyHandle { get; set; }
    internal ConstraintHandle? SuspensionSpringConstraint { get; set; }
    internal ConstraintHandle? SuspensionTrackConstraint { get; set; }
    internal ConstraintHandle? HingeConstraint { get; set; }
    internal ConstraintHandle? MotorConstraint { get; set; }

    public bool IsGrounded => WheelBodyHandle.HasValue;
    public float Rpm { get; internal set; }

    public (Vector3 Position, Quaternion Rotation) GetWorldPose()
    {
        return (WorldPosition, WorldRotation);
    }

    internal Vector3 WorldPosition { get; set; }
    internal Quaternion WorldRotation { get; set; }

    public static WheelColliderComponent Create(float radius = 0.4f, float width = 0.25f, float mass = 20f)
    {
        return new WheelColliderComponent
        {
            Radius = radius,
            Width = width,
            Mass = mass,
            Suspension = SuspensionSpring.Default,
            Center = Vector3.Zero
        };
    }
}
