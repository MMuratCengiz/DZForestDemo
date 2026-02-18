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
        else if (_staticHandles.TryGetValue(id, out var staticHandle))
        {
            _simulation.Statics.GetDescription(staticHandle, out var desc);
            desc.Pose = new RigidPose(position, rotation);
            _simulation.Statics.ApplyDescription(staticHandle, desc);
        }
    }

    /// <summary>
    /// Syncs a physics body's pose from the GameObject's current transform.
    /// Called by the editor after gizmo drags, transform panel edits, or undo/redo.
    /// </summary>
    public void SyncEditorTransform(int id, Vector3 position, Quaternion rotation)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            bodyRef.Pose.Position = position;
            bodyRef.Pose.Orientation = rotation;
            bodyRef.Velocity.Linear = Vector3.Zero;
            bodyRef.Velocity.Angular = Vector3.Zero;
            _simulation.Awakener.AwakenBody(handle);
            _kinematicTargets.Remove(id);
        }
        else if (_staticHandles.TryGetValue(id, out var staticHandle))
        {
            _simulation.Statics.GetDescription(staticHandle, out var desc);
            desc.Pose = new RigidPose(position, rotation);
            _simulation.Statics.ApplyDescription(staticHandle, desc);
        }
    }

    public void ApplyImpulse(int id, Vector3 impulse)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
            _simulation.Bodies.GetBodyReference(handle).ApplyLinearImpulse(impulse);
        }
    }

    public void ApplyImpulse(int id, Vector3 impulse, Vector3 worldPoint)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var offset = worldPoint - bodyRef.Pose.Position;
            bodyRef.ApplyImpulse(impulse, offset);
        }
    }

    public void ApplyAngularImpulse(int id, Vector3 angularImpulse)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
            _simulation.Bodies.GetBodyReference(handle).ApplyAngularImpulse(angularImpulse);
        }
    }

    public Vector3 GetVelocity(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            return _simulation.Bodies.GetBodyReference(handle).Velocity.Linear;
        }
        return Vector3.Zero;
    }

    public void SetVelocity(int id, Vector3 velocity)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
            _simulation.Bodies.GetBodyReference(handle).Velocity.Linear = velocity;
        }
    }

    public Vector3 GetAngularVelocity(int id)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            return _simulation.Bodies.GetBodyReference(handle).Velocity.Angular;
        }
        return Vector3.Zero;
    }

    public void SetAngularVelocity(int id, Vector3 angularVelocity)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            _simulation.Awakener.AwakenBody(handle);
            _simulation.Bodies.GetBodyReference(handle).Velocity.Angular = angularVelocity;
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

    public void SetKinematicTarget(int id, Vector3 position, Quaternion rotation)
    {
        _kinematicTargets[id] = (position, rotation);
    }

    public void SetKinematicTargetPosition(int id, Vector3 position)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var currentRot = _simulation.Bodies.GetBodyReference(handle).Pose.Orientation;
            _kinematicTargets[id] = (position, currentRot);
        }
    }

    public void SetKinematicTargetRotation(int id, Quaternion rotation)
    {
        if (_bodyHandles.TryGetValue(id, out var handle))
        {
            var currentPos = _simulation.Bodies.GetBodyReference(handle).Pose.Position;
            _kinematicTargets[id] = (currentPos, rotation);
        }
    }

    public void ChangeBodyType(int id, PhysicsBodyType newType, float mass = 1f)
    {
        if (!_trackedObjects.TryGetValue(id, out var entry))
        {
            return;
        }

        var currentType = entry.Rigidbody.BodyType;
        if (currentType == newType)
        {
            return;
        }

        var isCurrentBody = currentType is PhysicsBodyType.Dynamic or PhysicsBodyType.Kinematic;
        var isNewBody = newType is PhysicsBodyType.Dynamic or PhysicsBodyType.Kinematic;

        if (isCurrentBody && isNewBody)
        {
            if (!_bodyHandles.TryGetValue(id, out var handle))
            {
                return;
            }

            var bodyRef = _simulation.Bodies.GetBodyReference(handle);

            if (newType == PhysicsBodyType.Kinematic)
            {
                bodyRef.LocalInertia = new BodyInertia();
            }
            else
            {
                var collider = entry.Go.GetComponent<Components.Collider>();
                if (collider != null)
                {
                    var shape = collider.CreateShape();
                    bodyRef.LocalInertia = shape.ComputeInertia(mass);
                }
            }

            _simulation.Awakener.AwakenBody(handle);
            entry.Rigidbody.BodyType = newType;
            entry.Rigidbody.Mass = mass;
        }
        else
        {
            var go = entry.Go;
            var collider = go.GetComponent<Components.Collider>();
            if (collider == null)
            {
                return;
            }

            Vector3 pos;
            Quaternion rot;
            Vector3 linearVel = Vector3.Zero;
            Vector3 angularVel = Vector3.Zero;

            if (_bodyHandles.TryGetValue(id, out var bodyHandle))
            {
                var bodyRef = _simulation.Bodies.GetBodyReference(bodyHandle);
                pos = bodyRef.Pose.Position;
                rot = bodyRef.Pose.Orientation;
                linearVel = bodyRef.Velocity.Linear;
                angularVel = bodyRef.Velocity.Angular;
            }
            else
            {
                pos = go.LocalPosition;
                rot = go.LocalRotation;
            }

            _trackedObjects.Remove(id);
            entry.Rigidbody.BodyHandle = null;
            entry.Rigidbody.StaticHandle = null;
            collider.BodyHandle = null;
            collider.StaticHandle = null;
            RemoveBody(id);

            entry.Rigidbody.BodyType = newType;
            entry.Rigidbody.Mass = mass;

            if (newType == PhysicsBodyType.Static)
            {
                var shape = collider.CreateShape();
                var staticHandle = CreateStaticBody(id, pos, rot, shape);
                entry.Rigidbody.StaticHandle = staticHandle;
                collider.StaticHandle = staticHandle;
            }
            else
            {
                var shape = collider.CreateShape();
                var desc = entry.Rigidbody.ToBodyDesc(shape);
                var newHandle = CreateBody(id, pos, rot, desc);
                entry.Rigidbody.BodyHandle = newHandle;
                collider.BodyHandle = newHandle;

                if (newType == PhysicsBodyType.Dynamic)
                {
                    var bodyRef = _simulation.Bodies.GetBodyReference(newHandle);
                    bodyRef.Velocity.Linear = linearVel;
                    bodyRef.Velocity.Angular = angularVel;
                    _simulation.Awakener.AwakenBody(newHandle);
                }
            }

            _trackedObjects[id] = (go, entry.Rigidbody);
        }
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
