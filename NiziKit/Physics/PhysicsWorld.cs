using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using NiziKit.SceneManagement;

namespace NiziKit.Physics;

public sealed class PhysicsWorld : IWorldEventListener, IDisposable
{
    private readonly Simulation _simulation;
    private readonly BufferPool _bufferPool;
    private readonly ThreadDispatcher _threadDispatcher;
    private readonly Dictionary<int, BodyHandle> _bodyHandles = new();
    private readonly Dictionary<int, StaticHandle> _staticHandles = new();

    public Vector3 Gravity { get; set; }

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
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(Gravity),
            new SolveDescription(8, 1));
    }

    public void Step(float dt)
    {
        _simulation.Timestep(dt, _threadDispatcher);
    }

    public BodyHandle CreateBody(int id, Vector3 position, Quaternion rotation, PhysicsBodyDesc desc)
    {
        var handle = _simulation.Bodies.Add(CreateBodyDescription(position, rotation, desc));
        _bodyHandles[id] = handle;
        return handle;
    }

    public StaticHandle CreateStaticBody(int id, Vector3 position, Quaternion rotation, PhysicsShape shape)
    {
        var shapeIndex = shape.AddToSimulation(_simulation);
        var handle = _simulation.Statics.Add(new StaticDescription(
            new RigidPose(position, rotation), shapeIndex));
        _staticHandles[id] = handle;
        return handle;
    }

    public void RemoveBody(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var bodyHandle))
        {
            _simulation.Bodies.Remove(bodyHandle);
            _bodyHandles.Remove(id);
        }

        if (_staticHandles.TryGetValue(id, out var staticHandle))
        {
            _simulation.Statics.Remove(staticHandle);
            _staticHandles.Remove(id);
        }
    }

    public (Vector3 Position, Quaternion Rotation)? GetPose(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            return (bodyRef.Pose.Position, bodyRef.Pose.Orientation);
        }
        return null;
    }

    public void SetPose(int id, Vector3 position, Quaternion rotation)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.Pose.Position = position;
            bodyRef.Pose.Orientation = rotation;
        }
    }

    public void ApplyImpulse(int id, Vector3 impulse)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
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

    public void Dispose()
    {
        _simulation.Dispose();
        _threadDispatcher.Dispose();
        _bufferPool.Clear();
    }

    public void SceneReset()
    {
    }

    public void GameObjectCreated(GameObject go)
    {
    }

    public void GameObjectDestroyed(GameObject go)
    {
    }
}

internal struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation) { }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        => true;

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = 1f;
        pairMaterial.MaximumRecoveryVelocity = 2f;
        pairMaterial.SpringSettings = new SpringSettings(30, 1);
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
