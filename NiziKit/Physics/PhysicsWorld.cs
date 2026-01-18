using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed class PhysicsWorld : IWorldEventListener, IDisposable
{
    private const int MaxCollisionEvents = 1024;
    private const int MaxTrackedContacts = 2048;

    private readonly Simulation _simulation;
    private readonly BufferPool _bufferPool;
    private readonly ThreadDispatcher _threadDispatcher;
    private readonly Dictionary<int, BodyHandle> _bodyHandles = new();
    private readonly Dictionary<int, StaticHandle> _staticHandles = new();
    private readonly Dictionary<int, GameObject> _trackedObjects = new();
    private readonly Dictionary<BodyHandle, int> _bodyToId = new();
    private readonly Dictionary<StaticHandle, int> _staticToId = new();

    private readonly CollisionPair[] _activeContactsBuffer = new CollisionPair[MaxTrackedContacts];
    private readonly CollisionPair[] _previousContactsBuffer = new CollisionPair[MaxTrackedContacts];
    private int _activeContactCount;
    private int _previousContactCount;

    private readonly CollisionEventData[] _pendingCollisionEvents = new CollisionEventData[MaxCollisionEvents];
    private int _pendingEventCount;
    private readonly Lock _collisionLock = new();

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
            new NarrowPhaseCallbacks(this),
            new PoseIntegratorCallbacks(Gravity),
            new SolveDescription(8, 1));
    }

    internal int GetIdFromCollidablePublic(CollidableReference collidable)
    {
        return GetIdFromCollidable(collidable);
    }

    public void Step(float dt)
    {
        SwapContactBuffers();
        _simulation.Timestep(dt, _threadDispatcher);
        SyncToGameObjects();
        ProcessCollisionCallbacks();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SwapContactBuffers()
    {
        Array.Copy(_activeContactsBuffer, _previousContactsBuffer, _activeContactCount);
        _previousContactCount = _activeContactCount;
        _activeContactCount = 0;
    }

    private void ProcessCollisionCallbacks()
    {
        int eventCount;
        Span<CollisionEventData> events = stackalloc CollisionEventData[MaxCollisionEvents];

        lock (_collisionLock)
        {
            eventCount = _pendingEventCount;
            for (var i = 0; i < eventCount; i++)
            {
                events[i] = _pendingCollisionEvents[i];
            }
            _pendingEventCount = 0;
        }

        for (var i = 0; i < eventCount; i++)
        {
            ref var evt = ref events[i];
            var idA = evt.IdA;
            var idB = evt.IdB;
            var key = idA < idB ? new CollisionPair(idA, idB) : new CollisionPair(idB, idA);

            if (!ContainsContact(_activeContactsBuffer, _activeContactCount, key))
            {
                if (_activeContactCount < MaxTrackedContacts)
                {
                    _activeContactsBuffer[_activeContactCount++] = key;
                }
            }
        }

        for (var i = 0; i < _activeContactCount; i++)
        {
            var key = _activeContactsBuffer[i];

            if (!_trackedObjects.TryGetValue(key.IdA, out var goA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var goB))
            {
                continue;
            }

            var rbA = goA.GetComponent<RigidbodyComponent>();
            var rbB = goB.GetComponent<RigidbodyComponent>();

            var velA = rbA != null ? GetVelocity(key.IdA) : Vector3.Zero;
            var velB = rbB != null ? GetVelocity(key.IdB) : Vector3.Zero;
            var relativeVelocity = velA - velB;

            var wasActive = ContainsContact(_previousContactsBuffer, _previousContactCount, key);

            var collisionA = new Collision
            {
                Other = goB,
                Rigidbody = rbB,
                RelativeVelocity = relativeVelocity,
                ContactCount = 0
            };

            var collisionB = new Collision
            {
                Other = goA,
                Rigidbody = rbA,
                RelativeVelocity = -relativeVelocity,
                ContactCount = 0
            };

            if (wasActive)
            {
                goA.OnCollisionStay(in collisionA);
                goB.OnCollisionStay(in collisionB);
            }
            else
            {
                goA.OnCollisionEnter(in collisionA);
                goB.OnCollisionEnter(in collisionB);
            }
        }

        for (var i = 0; i < _previousContactCount; i++)
        {
            var key = _previousContactsBuffer[i];

            if (ContainsContact(_activeContactsBuffer, _activeContactCount, key))
            {
                continue;
            }

            if (!_trackedObjects.TryGetValue(key.IdA, out var goA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var goB))
            {
                continue;
            }

            var rbA = goA.GetComponent<RigidbodyComponent>();
            var rbB = goB.GetComponent<RigidbodyComponent>();

            var collisionA = new Collision
            {
                Other = goB,
                Rigidbody = rbB,
                RelativeVelocity = Vector3.Zero,
                ContactCount = 0
            };

            var collisionB = new Collision
            {
                Other = goA,
                Rigidbody = rbA,
                RelativeVelocity = Vector3.Zero,
                ContactCount = 0
            };

            goA.OnCollisionExit(in collisionA);
            goB.OnCollisionExit(in collisionB);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsContact(CollisionPair[] buffer, int count, CollisionPair key)
    {
        for (var i = 0; i < count; i++)
        {
            if (buffer[i].IdA == key.IdA && buffer[i].IdB == key.IdB)
            {
                return true;
            }
        }
        return false;
    }

    internal void RecordContact(int idA, int idB, Vector3 contactPoint, Vector3 normal, float depth)
    {
        lock (_collisionLock)
        {
            if (_pendingEventCount < MaxCollisionEvents)
            {
                _pendingCollisionEvents[_pendingEventCount++] = new CollisionEventData
                {
                    IdA = idA,
                    IdB = idB,
                    ContactPoint = contactPoint,
                    Normal = normal,
                    Depth = depth
                };
            }
        }
    }

    public BodyHandle CreateBody(int id, Vector3 position, Quaternion rotation, PhysicsBodyDesc desc)
    {
        var handle = _simulation.Bodies.Add(CreateBodyDescription(position, rotation, desc));
        _bodyHandles[id] = handle;
        _bodyToId[handle] = id;
        return handle;
    }

    public StaticHandle CreateStaticBody(int id, Vector3 position, Quaternion rotation, PhysicsShape shape)
    {
        var shapeIndex = shape.AddToSimulation(_simulation);
        var handle = _simulation.Statics.Add(new StaticDescription(
            new RigidPose(position, rotation), shapeIndex));
        _staticHandles[id] = handle;
        _staticToId[handle] = id;
        return handle;
    }

    public void RemoveBody(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var bodyHandle))
        {
            _simulation.Bodies.Remove(bodyHandle);
            _bodyHandles.Remove(id);
            _bodyToId.Remove(bodyHandle);
        }

        if (_staticHandles.TryGetValue(id, out var staticHandle))
        {
            _simulation.Statics.Remove(staticHandle);
            _staticHandles.Remove(id);
            _staticToId.Remove(staticHandle);
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

    public void ApplyImpulse(int id, Vector3 impulse, Vector3 worldPoint)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var offset = worldPoint - bodyRef.Pose.Position;
            bodyRef.ApplyImpulse(impulse, offset);
        }
    }

    public void ApplyAngularImpulse(int id, Vector3 angularImpulse)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.ApplyAngularImpulse(angularImpulse);
        }
    }

    public Vector3 GetVelocity(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            return bodyRef.Velocity.Linear;
        }
        return Vector3.Zero;
    }

    public void SetVelocity(int id, Vector3 velocity)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.Velocity.Linear = velocity;
        }
    }

    public Vector3 GetAngularVelocity(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            return bodyRef.Velocity.Angular;
        }
        return Vector3.Zero;
    }

    public void SetAngularVelocity(int id, Vector3 angularVelocity)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.Velocity.Angular = angularVelocity;
        }
    }

    public void Awake(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
        }
    }

    public bool Raycast(Ray ray, float maxDistance, out RaycastHit hit)
    {
        hit = default;
        var handler = new RayHitHandler(this);
        _simulation.RayCast(ray.Origin, ray.Direction, maxDistance, ref handler);

        if (handler.HasHit)
        {
            hit = handler.Hit;
            return true;
        }
        return false;
    }

    public void AddAttractorForce(Vector3 position, float force, float radius, float falloffPower = 1f)
    {
        var radiusSq = radius * radius;

        foreach (var (id, handle) in _bodyHandles)
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var bodyPos = bodyRef.Pose.Position;
            var diff = position - bodyPos;
            var distSq = diff.LengthSquared();

            if (distSq > radiusSq || distSq < 0.0001f)
            {
                continue;
            }

            var dist = MathF.Sqrt(distSq);
            var falloff = MathF.Pow(1f - (dist / radius), falloffPower);
            var direction = diff / dist;

            var impulse = direction * force * falloff;
            _simulation.Awakener.AwakenBody(handle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
    }

    public void AddExplosionForce(Vector3 position, float force, float radius, float upwardsModifier = 0f)
    {
        var radiusSq = radius * radius;

        foreach (var (id, handle) in _bodyHandles)
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var bodyPos = bodyRef.Pose.Position;
            var diff = bodyPos - position;
            var distSq = diff.LengthSquared();

            if (distSq > radiusSq || distSq < 0.0001f)
            {
                continue;
            }

            var dist = MathF.Sqrt(distSq);
            var falloff = 1f - (dist / radius);
            var direction = diff / dist;

            if (upwardsModifier != 0f)
            {
                direction.Y += upwardsModifier;
                direction = Vector3.Normalize(direction);
            }

            var impulse = direction * force * falloff;
            _simulation.Awakener.AwakenBody(handle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
    }

    public IEnumerable<int> OverlapSphere(Vector3 position, float radius)
    {
        var radiusSq = radius * radius;
        foreach (var (id, _) in _trackedObjects)
        {
            var pose = GetPose(id);
            if (!pose.HasValue)
            {
                continue;
            }

            var distSq = (pose.Value.Position - position).LengthSquared();
            if (distSq <= radiusSq)
            {
                yield return id;
            }
        }
    }

    private int GetIdFromCollidable(CollidableReference collidable)
    {
        if (collidable.Mobility == CollidableMobility.Static)
        {
            var staticHandle = collidable.StaticHandle;
            return _staticToId.TryGetValue(staticHandle, out var id) ? id : -1;
        }
        else
        {
            var bodyHandle = collidable.BodyHandle;
            return _bodyToId.TryGetValue(bodyHandle, out var id) ? id : -1;
        }
    }

    private struct RayHitHandler(PhysicsWorld world) : IRayHitHandler
    {
        public bool HasHit;
        public RaycastHit Hit;
        private float _closestT = float.MaxValue;

        public bool AllowTest(CollidableReference collidable) => true;

        public bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t < _closestT)
            {
                _closestT = t;
                maximumT = t;
                HasHit = true;
                Hit = new RaycastHit
                {
                    Point = ray.Origin + ray.Direction * t,
                    Normal = normal,
                    Distance = t,
                    GameObjectId = world.GetIdFromCollidable(collidable),
                    IsStatic = collidable.Mobility == CollidableMobility.Static
                };
            }
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

    public void GameObjectCreated(GameObject go)
    {
        TryRegister(go);
    }

    public void GameObjectDestroyed(GameObject go)
    {
        Unregister(go);
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is RigidbodyComponent)
        {
            TryRegister(go);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is RigidbodyComponent)
        {
            Unregister(go);
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
        if (component is RigidbodyComponent)
        {
            Unregister(go);
            TryRegister(go);
        }
    }

    private void TryRegister(GameObject go)
    {
        if (_trackedObjects.ContainsKey(go.Id))
        {
            return;
        }

        var rigidbody = go.GetComponent<RigidbodyComponent>();
        if (rigidbody == null)
        {
            return;
        }

        if (rigidbody.BodyType == PhysicsBodyType.Static)
        {
            var handle = CreateStaticBody(go.Id, go.LocalPosition, go.LocalRotation, rigidbody.Shape);
            rigidbody.StaticHandle = handle;
        }
        else
        {
            var handle = CreateBody(go.Id, go.LocalPosition, go.LocalRotation, rigidbody.ToBodyDesc());
            rigidbody.BodyHandle = handle;
        }

        _trackedObjects[go.Id] = go;
    }

    private void Unregister(GameObject go)
    {
        if (!_trackedObjects.Remove(go.Id))
        {
            return;
        }

        var rigidbody = go.GetComponent<RigidbodyComponent>();
        if (rigidbody != null)
        {
            rigidbody.BodyHandle = null;
            rigidbody.StaticHandle = null;
        }

        RemoveBody(go.Id);
    }

    private void SyncToGameObjects()
    {
        foreach (var (id, go) in _trackedObjects)
        {
            var rigidbody = go.GetComponent<RigidbodyComponent>();
            if (rigidbody == null)
            {
                continue;
            }
            if (rigidbody.BodyType == PhysicsBodyType.Kinematic)
            {
                SetPose(id, go.LocalPosition, go.LocalRotation);
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
        => a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;

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
