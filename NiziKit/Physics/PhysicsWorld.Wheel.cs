using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
    private void UpdateWheelColliders(float dt)
    {
        foreach (var (id, entry) in _wheelColliders)
        {
            UpdateWheelCollider(entry.Wheel, dt);
        }
    }
    
    private void UpdateWheelCollider(WheelColliderComponent wheel, float dt)
    {
        if (!wheel.WheelBodyHandle.HasValue || !wheel.MotorConstraint.HasValue || !wheel.HingeConstraint.HasValue)
        {
            return;
        }

        var motorHandle = wheel.MotorConstraint.Value;
        var hingeHandle = wheel.HingeConstraint.Value;

        var targetSpeed = 0f;
        var targetForce = 0f;

        if (wheel.BrakeTorque > 0.01f)
        {
            targetSpeed = 0f;
            targetForce = wheel.BrakeTorque;
        }
        else if (MathF.Abs(wheel.MotorTorque) > 0.01f)
        {
            targetForce = MathF.Abs(wheel.MotorTorque);
            targetSpeed = wheel.MotorTorque > 0 ? 100f : -100f;
        }

        _simulation.Solver.ApplyDescription(motorHandle, new AngularAxisMotor
        {
            LocalAxisA = new Vector3(0, -1, 0),
            Settings = new MotorSettings(targetForce, 1e-6f),
            TargetVelocity = targetSpeed
        });

        if (MathF.Abs(wheel.SteerAngle) > 0.001f || wheel.SteerAngle == 0f)
        {
            var localWheelOrientation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f);
            QuaternionEx.TransformUnitY(localWheelOrientation, out var wheelAxis);

            var suspensionDirection = new Vector3(0, -1, 0);
            Matrix3x3.CreateFromAxisAngle(suspensionDirection, -wheel.SteerAngle, out var rotation);
            Matrix3x3.Transform(wheelAxis, rotation, out var steeredAxis);

            _simulation.Solver.ApplyDescription(hingeHandle, new AngularHinge
            {
                LocalHingeAxisA = steeredAxis,
                LocalHingeAxisB = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
        }

        var wheelRef = _simulation.Bodies.GetBodyReference(wheel.WheelBodyHandle.Value);
        wheel.WorldPosition = wheelRef.Pose.Position;
        wheel.WorldRotation = wheelRef.Pose.Orientation;
        wheel.Rpm = wheelRef.Velocity.Angular.Y * 60f / (2f * MathF.PI);
    }

    private void TryRegisterWheelCollider(GameObject wheelGo, WheelColliderComponent wheel)
    {
        if (_wheelColliders.ContainsKey(wheelGo.Id))
        {
            return;
        }

        var parent = wheelGo.Parent;
        if (parent == null)
        {
            return;
        }

        var parentRb = parent.GetComponent<RigidbodyComponent>();
        if (parentRb?.BodyHandle == null)
        {
            return;
        }

        var bodyHandle = parentRb.BodyHandle.Value;
        wheel.ConnectedBodyHandle = bodyHandle;

        var localWheelOrientation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f);
        QuaternionEx.TransformUnitY(localWheelOrientation, out var wheelAxis);

        var suspensionDirection = new Vector3(0, -1, 0);
        var bodyPosition = parent.LocalPosition;
        var bodyRotation = parent.LocalRotation;
        var localOffset = wheel.Center + wheelGo.LocalPosition;

        RigidPose wheelPose;
        RigidPose.Transform(localOffset + suspensionDirection * wheel.Suspension.RestLength, new RigidPose(bodyPosition, bodyRotation), out wheelPose.Position);
        QuaternionEx.ConcatenateWithoutOverlap(localWheelOrientation, bodyRotation, out wheelPose.Orientation);

        var wheelShape = new Cylinder(wheel.Radius, wheel.Width);
        var wheelShapeIndex = _simulation.Shapes.Add(wheelShape);
        var wheelInertia = wheelShape.ComputeInertia(wheel.Mass);

        var wheelDescription = BodyDescription.CreateDynamic(
            wheelPose,
            wheelInertia,
            new CollidableDescription(wheelShapeIndex, 0.1f),
            0.01f);

        wheel.WheelBodyHandle = _simulation.Bodies.Add(wheelDescription);

        IgnoreCollision(bodyHandle, wheel.WheelBodyHandle.Value);

        var suspensionSettings = new SpringSettings(wheel.Suspension.Frequency, wheel.Suspension.Damping);

        wheel.SuspensionSpringConstraint = _simulation.Solver.Add(bodyHandle, wheel.WheelBodyHandle.Value, new LinearAxisServo
        {
            LocalPlaneNormal = suspensionDirection,
            TargetOffset = wheel.Suspension.RestLength,
            LocalOffsetA = localOffset,
            LocalOffsetB = default,
            ServoSettings = ServoSettings.Default,
            SpringSettings = suspensionSettings
        });
        _allConstraints.Add(wheel.SuspensionSpringConstraint.Value);

        wheel.SuspensionTrackConstraint = _simulation.Solver.Add(bodyHandle, wheel.WheelBodyHandle.Value, new PointOnLineServo
        {
            LocalDirection = suspensionDirection,
            LocalOffsetA = localOffset,
            LocalOffsetB = default,
            ServoSettings = ServoSettings.Default,
            SpringSettings = new SpringSettings(30, 1)
        });
        _allConstraints.Add(wheel.SuspensionTrackConstraint.Value);

        wheel.MotorConstraint = _simulation.Solver.Add(wheel.WheelBodyHandle.Value, bodyHandle, new AngularAxisMotor
        {
            LocalAxisA = new Vector3(0, 1, 0),
            Settings = default,
            TargetVelocity = default
        });
        _allConstraints.Add(wheel.MotorConstraint.Value);

        wheel.HingeConstraint = _simulation.Solver.Add(bodyHandle, wheel.WheelBodyHandle.Value, new AngularHinge
        {
            LocalHingeAxisA = wheelAxis,
            LocalHingeAxisB = new Vector3(0, 1, 0),
            SpringSettings = new SpringSettings(30, 1)
        });
        _allConstraints.Add(wheel.HingeConstraint.Value);

        _wheelColliders[wheelGo.Id] = (wheelGo, wheel);
    }

    private void UnregisterWheelCollider(GameObject wheelGo, WheelColliderComponent wheel)
    {
        if (!_wheelColliders.Remove(wheelGo.Id))
        {
            return;
        }

        if (wheel.ConnectedBodyHandle.HasValue && wheel.WheelBodyHandle.HasValue)
        {
            IgnoreCollision(wheel.ConnectedBodyHandle.Value, wheel.WheelBodyHandle.Value, false);
        }

        if (wheel.SuspensionSpringConstraint.HasValue)
        {
            if (_simulation.Solver.ConstraintExists(wheel.SuspensionSpringConstraint.Value))
            {
                _simulation.Solver.Remove(wheel.SuspensionSpringConstraint.Value);
            }

            _allConstraints.Remove(wheel.SuspensionSpringConstraint.Value);
        }
        if (wheel.SuspensionTrackConstraint.HasValue)
        {
            if (_simulation.Solver.ConstraintExists(wheel.SuspensionTrackConstraint.Value))
            {
                _simulation.Solver.Remove(wheel.SuspensionTrackConstraint.Value);
            }

            _allConstraints.Remove(wheel.SuspensionTrackConstraint.Value);
        }
        if (wheel.MotorConstraint.HasValue)
        {
            if (_simulation.Solver.ConstraintExists(wheel.MotorConstraint.Value))
            {
                _simulation.Solver.Remove(wheel.MotorConstraint.Value);
            }

            _allConstraints.Remove(wheel.MotorConstraint.Value);
        }
        if (wheel.HingeConstraint.HasValue)
        {
            if (_simulation.Solver.ConstraintExists(wheel.HingeConstraint.Value))
            {
                _simulation.Solver.Remove(wheel.HingeConstraint.Value);
            }

            _allConstraints.Remove(wheel.HingeConstraint.Value);
        }

        if (wheel.WheelBodyHandle.HasValue && _simulation.Bodies.BodyExists(wheel.WheelBodyHandle.Value))
        {
            _simulation.Bodies.Remove(wheel.WheelBodyHandle.Value);
        }

        wheel.WheelBodyHandle = null;
        wheel.ConnectedBodyHandle = null;
        wheel.SuspensionSpringConstraint = null;
        wheel.SuspensionTrackConstraint = null;
        wheel.MotorConstraint = null;
        wheel.HingeConstraint = null;
    }
}
