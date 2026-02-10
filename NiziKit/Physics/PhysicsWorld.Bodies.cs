using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
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

    public bool IsSleeping(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            return !_simulation.Bodies.GetBodyReference(handle).Awake;
        }
        return false;
    }

    public BodyHandle CreateBodyDirect(BodyDescription description)
    {
        return _simulation.Bodies.Add(description);
    }

    public void RemoveBodyDirect(BodyHandle handle)
    {
        if (_simulation.Bodies.BodyExists(handle))
        {
            _simulation.Bodies.Remove(handle);
        }
    }

    public BodyReference GetBodyReference(BodyHandle handle)
    {
        return _simulation.Bodies.GetBodyReference(handle);
    }

    public TypedIndex AddShape<TShape>(TShape shape) where TShape : unmanaged, IShape
    {
        return _simulation.Shapes.Add(shape);
    }

    public void WakeBody(BodyHandle handle)
    {
        _simulation.Awakener.AwakenBody(handle);
    }

    internal void TrackBodyToId(BodyHandle handle, int id)
    {
        _bodyToId[handle] = id;
        _bodyHandles[id] = handle;
    }

    internal void UntrackBody(BodyHandle handle, int id)
    {
        _bodyToId.Remove(handle);
        _bodyHandles.Remove(id);
    }

    public void IgnoreCollision(BodyHandle a, BodyHandle b, bool ignore = true)
    {
        var pair = a.Value < b.Value ? (a, b) : (b, a);
        if (ignore)
        {
            _ignoredCollisionPairs.Add(pair);
        }
        else
        {
            _ignoredCollisionPairs.Remove(pair);
        }
    }

    internal bool ShouldIgnoreCollision(BodyHandle a, BodyHandle b)
    {
        var pair = a.Value < b.Value ? (a, b) : (b, a);
        return _ignoredCollisionPairs.Contains(pair);
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

    internal int GetIdFromCollidablePublic(CollidableReference collidable)
    {
        return GetIdFromCollidable(collidable);
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
}
