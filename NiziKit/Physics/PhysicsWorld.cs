using System.Numerics;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld : IWorldEventListener, IDisposable
{
    private const int MaxCollisionEvents = 1024;
    private const int MaxTrackedContacts = 2048;

    private readonly Simulation _simulation;
    private readonly BufferPool _bufferPool;
    private readonly ThreadDispatcher _threadDispatcher;
    private readonly Dictionary<int, BodyHandle> _bodyHandles = new();
    private readonly Dictionary<int, StaticHandle> _staticHandles = new();
    private readonly Dictionary<int, (GameObject Go, Rigidbody Rigidbody)> _trackedObjects = new();
    private readonly Dictionary<int, (Vector3 Position, Quaternion Rotation)> _kinematicTargets = new();
    private readonly Dictionary<int, (Vector3 Position, Quaternion Rotation)> _kinematicFinalPoses = new();
    private readonly Dictionary<BodyHandle, int> _bodyToId = new();
    private readonly Dictionary<StaticHandle, int> _staticToId = new();
    private readonly Dictionary<int, List<ConstraintHandle>> _constraintsByOwner = new();
    private readonly List<ConstraintHandle> _allConstraints = [];
    private readonly HashSet<(BodyHandle, BodyHandle)> _ignoredCollisionPairs = [];
    private readonly Dictionary<int, (GameObject Go, WheelColliderComponent Wheel)> _wheelColliders = new();

    private readonly CollisionPair[] _activeContactsBuffer = new CollisionPair[MaxTrackedContacts];
    private readonly CollisionPair[] _previousContactsBuffer = new CollisionPair[MaxTrackedContacts];
    private int _activeContactCount;
    private int _previousContactCount;

    private readonly CollisionEventData[] _pendingCollisionEvents = new CollisionEventData[MaxCollisionEvents];
    private int _pendingEventCount;
    private readonly Lock _collisionLock = new();

    public Vector3 Gravity { get; set; }

    internal Simulation Simulation => _simulation;
    internal BufferPool BufferPool => _bufferPool;

    public PhysicsWorld(Vector3? gravity = null, int threadCount = -1)
    {
        Gravity = gravity ?? new Vector3(0, -9.81f, 0);

        _bufferPool = new BufferPool();

        var targetThreadCount = threadCount > 0
            ? threadCount
            : Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        _threadDispatcher = new ThreadDispatcher(targetThreadCount);

        _simulation = Simulation.Create(
            _bufferPool,
            new NarrowPhaseCallbacks(this),
            new PoseIntegratorCallbacks(Gravity),
            new SolveDescription(8, 1));
    }

    public void Step(float dt)
    {
        UpdateWheelColliders(dt);
        PrepareKinematics(dt);
        SwapContactBuffers();
        _simulation.Timestep(dt, _threadDispatcher);
        SyncToGameObjects();
        ProcessCollisionCallbacks();
    }

    private const float KinematicTeleportThreshold = 100f;

    private void PrepareKinematics(float dt)
    {
        if (dt <= 0f)
        {
            return;
        }

        var inverseDt = 1f / dt;

        foreach (var (id, entry) in _trackedObjects)
        {
            if (entry.Rigidbody.BodyType != PhysicsBodyType.Kinematic)
            {
                continue;
            }

            if (!_bodyHandles.TryGetValue(id, out var handle))
            {
                continue;
            }

            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var currentPos = bodyRef.Pose.Position;
            var currentRot = bodyRef.Pose.Orientation;

            Vector3 targetPos;
            Quaternion targetRot;

            if (_kinematicTargets.TryGetValue(id, out var target))
            {
                targetPos = target.Position;
                targetRot = target.Rotation;
                _kinematicTargets.Remove(id);
            }
            else
            {
                targetPos = entry.Go.LocalPosition;
                targetRot = entry.Go.LocalRotation;
            }

            var displacement = targetPos - currentPos;

            if (displacement.LengthSquared() > KinematicTeleportThreshold * KinematicTeleportThreshold)
            {
                bodyRef.Pose.Position = targetPos;
                bodyRef.Pose.Orientation = targetRot;
                bodyRef.Velocity.Linear = Vector3.Zero;
                bodyRef.Velocity.Angular = Vector3.Zero;
            }
            else
            {
                bodyRef.Velocity.Linear = displacement * inverseDt;
                bodyRef.Velocity.Angular = ComputeAngularVelocity(currentRot, targetRot, inverseDt);
            }

            _simulation.Awakener.AwakenBody(handle);
            _kinematicFinalPoses[id] = (targetPos, targetRot);
        }
    }

    private static Vector3 ComputeAngularVelocity(Quaternion from, Quaternion to, float inverseDt)
    {
        var delta = to * Quaternion.Conjugate(from);

        if (delta.W < 0f)
        {
            delta = new Quaternion(-delta.X, -delta.Y, -delta.Z, -delta.W);
        }

        var axis = new Vector3(delta.X, delta.Y, delta.Z);
        var sinHalfAngle = axis.Length();

        if (sinHalfAngle < 1e-6f)
        {
            return Vector3.Zero;
        }

        var halfAngle = MathF.Atan2(sinHalfAngle, delta.W);
        var angle = 2f * halfAngle;

        return axis / sinHalfAngle * angle * inverseDt;
    }

    private void SyncToGameObjects()
    {
        foreach (var (id, entry) in _trackedObjects)
        {
            var rigidbody = entry.Rigidbody;
            var go = entry.Go;

            if (rigidbody.BodyType == PhysicsBodyType.Kinematic)
            {
                if (_kinematicFinalPoses.TryGetValue(id, out var finalPose))
                {
                    SetPose(id, finalPose.Position, finalPose.Rotation);
                    go.LocalPosition = finalPose.Position;
                    go.LocalRotation = finalPose.Rotation;
                }
            }
            else if (rigidbody.BodyType == PhysicsBodyType.Dynamic)
            {
                var pose = GetPose(id);
                if (pose.HasValue)
                {
                    go.LocalPosition = pose.Value.Position;
                    go.LocalRotation = pose.Value.Rotation;
                }
            }
        }

        _kinematicFinalPoses.Clear();
    }

    public void Dispose()
    {
        _simulation.Dispose();
        _threadDispatcher.Dispose();
        _bufferPool.Clear();
    }

    public void SceneReset()
    {
        foreach (var (id, entry) in _wheelColliders.ToArray())
        {
            UnregisterWheelCollider(entry.Go, entry.Wheel);
        }
        _wheelColliders.Clear();
        _ignoredCollisionPairs.Clear();
        _kinematicTargets.Clear();
        _kinematicFinalPoses.Clear();

        foreach (var handle in _allConstraints.ToArray())
        {
            if (_simulation.Solver.ConstraintExists(handle))
            {
                _simulation.Solver.Remove(handle);
            }
        }
        _allConstraints.Clear();
        _constraintsByOwner.Clear();

        foreach (var id in _bodyHandles.Keys.ToArray())
        {
            RemoveBody(id);
        }
        foreach (var id in _staticHandles.Keys.ToArray())
        {
            RemoveBody(id);
        }
        _trackedObjects.Clear();
        _activeContactCount = 0;
        _previousContactCount = 0;
        _pendingEventCount = 0;
    }

    private BodyDescription CreateBodyDescription(Vector3 position, Quaternion rotation, PhysicsBodyDesc desc)
    {
        var shapeIndex = desc.Shape.AddToSimulation(_simulation);
        var inertia = desc.Shape.ComputeInertia(desc.Mass);

        return desc.BodyType == PhysicsBodyType.Kinematic
            ? BodyDescription.CreateKinematic(
                new RigidPose(position, rotation),
                new CollidableDescription(shapeIndex, desc.SpeculativeMargin),
                new BodyActivityDescription(desc.SleepThreshold))
            : BodyDescription.CreateDynamic(
                new RigidPose(position, rotation),
                inertia,
                new CollidableDescription(shapeIndex, desc.SpeculativeMargin),
                new BodyActivityDescription(desc.SleepThreshold));
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct CollisionPair(int idA, int idB)
{
    public int IdA = idA;
    public int IdB = idB;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CollisionEventData
{
    public int IdA;
    public int IdB;
    public Vector3 ContactPoint;
    public Vector3 Normal;
    public float Depth;
}

internal readonly struct NarrowPhaseCallbacks(PhysicsWorld physicsWorld) : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        if (a.Mobility != CollidableMobility.Dynamic && b.Mobility != CollidableMobility.Dynamic)
        {
            return false;
        }

        if (a.Mobility != CollidableMobility.Static && b.Mobility != CollidableMobility.Static)
        {
            if (physicsWorld.ShouldIgnoreCollision(a.BodyHandle, b.BodyHandle))
            {
                return false;
            }
        }

        return true;
    }

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        => true;

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = 1f;
        pairMaterial.MaximumRecoveryVelocity = 2f;
        pairMaterial.SpringSettings = new SpringSettings(30, 1);

        if (manifold.Count > 0)
        {
            var idA = physicsWorld.GetIdFromCollidablePublic(pair.A);
            var idB = physicsWorld.GetIdFromCollidablePublic(pair.B);
            if (idA >= 0 && idB >= 0)
            {
                manifold.GetContact(0, out var offset, out var normal, out var depth, out _);
                physicsWorld.RecordContact(idA, idB, offset, normal, depth);
            }
        }
        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold)
        => true;

    public void Dispose() { }
}

internal struct PoseIntegratorCallbacks(Vector3 gravity) : IPoseIntegratorCallbacks
{
    private Vector3Wide _gravityDt;

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation) { }

    public void PrepareForIntegration(float dt)
    {
        _gravityDt = Vector3Wide.Broadcast(gravity * dt);
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation,
        BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {
        velocity.Linear += _gravityDt;
    }
}
