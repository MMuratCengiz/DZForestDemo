using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using Flecs.NET.Core;
using Physics.Components;

namespace Physics;

public struct RaycastHit
{
    public Entity Entity;
    public Vector3 Point;
    public Vector3 Normal;
    public float Distance;
    public bool IsStatic;
}

public struct CollisionEvent
{
    public Entity EntityA;
    public Entity EntityB;
    public Vector3 ContactPoint;
    public Vector3 Normal;
    public float Depth;
}

public class PhysicsResource : IDisposable
{
    private readonly List<CollisionEvent> _collisionEvents = [];
    private readonly Dictionary<BodyHandle, Entity> _dynamicToEntity = new();
    private readonly Dictionary<Entity, BodyHandle> _entityToDynamic = new();
    private readonly Dictionary<Entity, StaticHandle> _entityToStatic = new();
    private readonly HashSet<Entity> _pendingRemoval = [];
    private readonly Dictionary<StaticHandle, Entity> _staticToEntity = new();
    private readonly ThreadDispatcher _threadDispatcher;

    private bool _disposed;

    public PhysicsResource(int targetThreadCount = -1)
    {
        BufferPool = new BufferPool();

        var threadCount = targetThreadCount > 0
            ? targetThreadCount
            : Math.Max(1, Environment.ProcessorCount - 2);

        _threadDispatcher = new ThreadDispatcher(threadCount);

        var narrowPhaseCallbacks = new NarrowPhaseCallbacks(this);
        var poseIntegratorCallbacks = new PoseIntegratorCallbacks(Gravity);
        var solveDescription = new SolveDescription(8, 1);

        Simulation = Simulation.Create(
            BufferPool,
            narrowPhaseCallbacks,
            poseIntegratorCallbacks,
            solveDescription);
    }

    public Simulation Simulation { get; }

    public BufferPool BufferPool { get; }

    public Vector3 Gravity { get; set; } = new(0, -9.81f, 0);
    public float FixedTimeStep { get; set; } = 1f / 60f;
    public int AccumulatedSteps { get; set; }
    public IReadOnlyList<CollisionEvent> CollisionEvents => _collisionEvents;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Simulation.Dispose();
        _threadDispatcher.Dispose();
        BufferPool.Clear();

        GC.SuppressFinalize(this);
    }

    public BodyHandle CreateBody(Entity entity, Vector3 position, Quaternion rotation, PhysicsBodyDesc desc)
    {
        var shapeIndex = desc.Shape.AddToSimulation(Simulation);
        var inertia = desc.Shape.ComputeInertia(desc.Mass);

        var bodyDescription = desc.BodyType == PhysicsBodyType.Kinematic
            ? BodyDescription.CreateKinematic(
                new RigidPose(position, rotation),
                new CollidableDescription(shapeIndex, desc.SpeculativeMargin),
                new BodyActivityDescription(desc.SleepThreshold))
            : BodyDescription.CreateDynamic(
                new RigidPose(position, rotation),
                inertia,
                new CollidableDescription(shapeIndex, desc.SpeculativeMargin),
                new BodyActivityDescription(desc.SleepThreshold));

        var handle = Simulation.Bodies.Add(bodyDescription);
        _entityToDynamic[entity] = handle;
        _dynamicToEntity[handle] = entity;
        return handle;
    }

    public StaticHandle CreateStaticBody(Entity entity, Vector3 position, Quaternion rotation, PhysicsShape shape)
    {
        var shapeIndex = shape.AddToSimulation(Simulation);
        var staticDescription = new StaticDescription(new RigidPose(position, rotation), shapeIndex);
        var handle = Simulation.Statics.Add(staticDescription);
        _entityToStatic[entity] = handle;
        _staticToEntity[handle] = entity;
        return handle;
    }

    [Obsolete("Use CreateBody with PhysicsBodyDesc instead")]
    public BodyHandle CreateDynamicBody(Entity entity, Vector3 position, Quaternion rotation, ColliderDesc desc)
    {
        var shape = desc.Shape switch
        {
            ColliderShape.Box => PhysicsShape.Box(desc.Size),
            ColliderShape.Sphere => PhysicsShape.Sphere(desc.Size.X),
            ColliderShape.Capsule => PhysicsShape.Capsule(desc.Size.X, desc.Size.Y),
            _ => throw new ArgumentOutOfRangeException()
        };
        return CreateBody(entity, position, rotation, PhysicsBodyDesc.Dynamic(shape, desc.Mass));
    }

    [Obsolete("Use CreateStaticBody with PhysicsShape instead")]
    public StaticHandle CreateStaticBody(Entity entity, Vector3 position, Quaternion rotation, ColliderDesc desc)
    {
        var shape = desc.Shape switch
        {
            ColliderShape.Box => PhysicsShape.Box(desc.Size),
            ColliderShape.Sphere => PhysicsShape.Sphere(desc.Size.X),
            ColliderShape.Capsule => PhysicsShape.Capsule(desc.Size.X, desc.Size.Y),
            _ => throw new ArgumentOutOfRangeException()
        };
        return CreateStaticBody(entity, position, rotation, shape);
    }

    public void MarkForRemoval(Entity entity)
    {
        _pendingRemoval.Add(entity);
    }

    public void ProcessRemovals()
    {
        foreach (var entity in _pendingRemoval)
        {
            RemoveBody(entity);
        }

        _pendingRemoval.Clear();
    }

    public void RemoveBody(Entity entity)
    {
        if (_entityToDynamic.TryGetValue(entity, out var dynamicHandle))
        {
            Simulation.Bodies.Remove(dynamicHandle);
            _entityToDynamic.Remove(entity);
            _dynamicToEntity.Remove(dynamicHandle);
        }

        if (_entityToStatic.TryGetValue(entity, out var staticHandle))
        {
            Simulation.Statics.Remove(staticHandle);
            _entityToStatic.Remove(entity);
            _staticToEntity.Remove(staticHandle);
        }
    }

    public bool HasBody(Entity entity)
    {
        return _entityToDynamic.ContainsKey(entity) || _entityToStatic.ContainsKey(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 Position, Quaternion Rotation) GetBodyPose(BodyHandle handle)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        return (bodyRef.Pose.Position, bodyRef.Pose.Orientation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (Vector3 Linear, Vector3 Angular) GetBodyVelocity(BodyHandle handle)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        return (bodyRef.Velocity.Linear, bodyRef.Velocity.Angular);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBodyVelocity(BodyHandle handle, Vector3 linearVelocity, Vector3 angularVelocity = default)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        bodyRef.Velocity.Linear = linearVelocity;
        bodyRef.Velocity.Angular = angularVelocity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBodyPose(BodyHandle handle, Vector3 position, Quaternion rotation)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        bodyRef.Pose.Position = position;
        bodyRef.Pose.Orientation = rotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyImpulse(BodyHandle handle, Vector3 impulse)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        bodyRef.ApplyLinearImpulse(impulse);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyImpulseAt(BodyHandle handle, Vector3 impulse, Vector3 worldPoint)
    {
        var bodyRef = Simulation.Bodies.GetBodyReference(handle);
        bodyRef.ApplyImpulse(impulse, worldPoint - bodyRef.Pose.Position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WakeBody(BodyHandle handle)
    {
        Simulation.Awakener.AwakenBody(handle);
    }

    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        var handler = new RayHitHandler(this);
        Simulation.RayCast(origin, direction, maxDistance, ref handler);

        if (handler.Hit)
        {
            hit = handler.Result;
            return true;
        }

        hit = default;
        return false;
    }

    public bool RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, List<RaycastHit> hits)
    {
        var handler = new RayHitAllHandler(this, hits);
        Simulation.RayCast(origin, direction, maxDistance, ref handler);
        return hits.Count > 0;
    }

    internal void AddCollisionEvent(CollisionEvent evt)
    {
        _collisionEvents.Add(evt);
    }

    internal Entity? GetEntityFromCollidable(CollidableReference collidable)
    {
        if (collidable.Mobility == CollidableMobility.Dynamic)
        {
            return _dynamicToEntity.TryGetValue(collidable.BodyHandle, out var entity) ? entity : null;
        }

        return _staticToEntity.TryGetValue(collidable.StaticHandle, out var staticEntity) ? staticEntity : null;
    }

    public void ClearCollisionEvents()
    {
        _collisionEvents.Clear();
    }

    public void Step(float dt)
    {
        ClearCollisionEvents();
        Simulation.Timestep(dt, _threadDispatcher);
    }
}

internal struct RayHitHandler(PhysicsResource resource) : IRayHitHandler
{
    public bool Hit = false;
    public RaycastHit Result = default;

    public bool AllowTest(CollidableReference collidable)
    {
        return true;
    }

    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true;
    }

    public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable,
        int childIndex)
    {
        if (t < maximumT)
        {
            maximumT = t;
            Hit = true;

            var entity = resource.GetEntityFromCollidable(collidable);
            Result = new RaycastHit
            {
                Entity = entity ?? default,
                Point = ray.Origin + ray.Direction * t,
                Normal = normal,
                Distance = t,
                IsStatic = collidable.Mobility == CollidableMobility.Static
            };
        }
    }
}

internal struct RayHitAllHandler(PhysicsResource resource, List<RaycastHit> hits) : IRayHitHandler
{
    public bool AllowTest(CollidableReference collidable)
    {
        return true;
    }

    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true;
    }

    public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable,
        int childIndex)
    {
        var entity = resource.GetEntityFromCollidable(collidable);
        hits.Add(new RaycastHit
        {
            Entity = entity ?? default,
            Point = ray.Origin + ray.Direction * t,
            Normal = normal,
            Distance = t,
            IsStatic = collidable.Mobility == CollidableMobility.Static
        });
    }
}

public struct NarrowPhaseCallbacks(PhysicsResource resource) : INarrowPhaseCallbacks
{
    private PhysicsResource _resource = resource;

    public void Initialize(Simulation simulation)
    {
    }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
        ref float speculativeMargin)
    {
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = 0.5f;
        pairMaterial.MaximumRecoveryVelocity = 2f;
        pairMaterial.SpringSettings = new SpringSettings(30, 1);
        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }
}

public struct PoseIntegratorCallbacks(Vector3 gravity, float linearDamping = 0.03f, float angularDamping = 0.03f)
    : IPoseIntegratorCallbacks
{
    private Vector3Wide _gravityWideDt = default;
    private Vector<float> _linearDampingDt = new(1f - linearDamping);
    private Vector<float> _angularDampingDt = new(1f - angularDamping);

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
    }

    public void PrepareForIntegration(float dt)
    {
        _gravityWideDt = Vector3Wide.Broadcast(gravity * dt);
        _linearDampingDt = new Vector<float>(MathF.Pow(0.97f, dt));
        _angularDampingDt = new Vector<float>(MathF.Pow(0.97f, dt));
    }

    public void IntegrateVelocity(
        Vector<int> bodyIndices,
        Vector3Wide position,
        QuaternionWide orientation,
        BodyInertiaWide localInertia,
        Vector<int> integrationMask,
        int workerIndex,
        Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        velocity.Linear += _gravityWideDt;
        velocity.Linear *= _linearDampingDt;
        velocity.Angular *= _angularDampingDt;
    }
}