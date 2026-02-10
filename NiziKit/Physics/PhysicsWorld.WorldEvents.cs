using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
    private readonly Dictionary<int, (GameObject Go, Collider Collider)> _trackedColliders = new();

    public void GameObjectCreated(GameObject go)
    {
        TryRegister(go);
        TryRegisterColliders(go);
    }

    public void GameObjectDestroyed(GameObject go)
    {
        Unregister(go);
        UnregisterColliders(go);
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is Rigidbody)
        {
            UnregisterColliders(go);
            TryRegister(go);
        }
        else if (component is Collider collider)
        {
            TryRegisterCollider(go, collider);
        }
        else if (component is WheelColliderComponent wheelCollider)
        {
            TryRegisterWheelCollider(go, wheelCollider);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is Rigidbody)
        {
            Unregister(go);
            TryRegisterColliders(go);
        }
        else if (component is Collider collider)
        {
            UnregisterCollider(go, collider);
        }
        else if (component is WheelColliderComponent wheelCollider)
        {
            UnregisterWheelCollider(go, wheelCollider);
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
        if (component is Rigidbody)
        {
            Unregister(go);
            TryRegister(go);
        }
        else if (component is Collider collider)
        {
            UnregisterCollider(go, collider);
            TryRegisterCollider(go, collider);
        }
        else if (component is WheelColliderComponent wheelCollider)
        {
            UnregisterWheelCollider(go, wheelCollider);
            TryRegisterWheelCollider(go, wheelCollider);
        }
    }

    private void TryRegister(GameObject go)
    {
        if (_trackedObjects.ContainsKey(go.Id))
        {
            return;
        }

        var rigidbody = go.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            return;
        }

        var collider = go.GetComponent<Collider>();
        var shape = collider?.CreateShape() ?? rigidbody.Shape;

        if (shape.Size == System.Numerics.Vector3.Zero)
        {
            return;
        }

        if (rigidbody.BodyType == PhysicsBodyType.Static)
        {
            var handle = CreateStaticBody(go.Id, go.LocalPosition, go.LocalRotation, shape);
            rigidbody.StaticHandle = handle;
            if (collider != null)
            {
                collider.StaticHandle = handle;
            }
        }
        else
        {
            var desc = rigidbody.ToBodyDesc();
            if (collider != null)
            {
                desc = new PhysicsBodyDesc
                {
                    Shape = shape,
                    BodyType = desc.BodyType,
                    Mass = desc.Mass,
                    SpeculativeMargin = desc.SpeculativeMargin,
                    SleepThreshold = desc.SleepThreshold
                };
            }
            var handle = CreateBody(go.Id, go.LocalPosition, go.LocalRotation, desc);
            rigidbody.BodyHandle = handle;
            if (collider != null)
            {
                collider.BodyHandle = handle;
            }
        }

        _trackedObjects[go.Id] = (go, rigidbody);
    }

    private void Unregister(GameObject go)
    {
        if (!_trackedObjects.TryGetValue(go.Id, out var entry))
        {
            return;
        }

        _trackedObjects.Remove(go.Id);
        entry.Rigidbody.BodyHandle = null;
        entry.Rigidbody.StaticHandle = null;
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.BodyHandle = null;
            collider.StaticHandle = null;
        }

        RemoveBody(go.Id);
    }

    private void TryRegisterColliders(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            TryRegisterCollider(go, collider);
        }
    }

    private void TryRegisterCollider(GameObject go, Collider collider)
    {
        if (go.HasComponent<Rigidbody>())
        {
            return;
        }
        if (_trackedColliders.ContainsKey(go.Id))
        {
            return;
        }
        var shape = collider.CreateShape();
        var handle = CreateStaticBody(go.Id, go.LocalPosition, go.LocalRotation, shape);
        collider.StaticHandle = handle;

        _trackedColliders[go.Id] = (go, collider);
    }

    private void UnregisterColliders(GameObject go)
    {
        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            UnregisterCollider(go, collider);
        }
    }

    private void UnregisterCollider(GameObject go, Collider collider)
    {
        if (!_trackedColliders.TryGetValue(go.Id, out _))
        {
            return;
        }

        _trackedColliders.Remove(go.Id);
        collider.BodyHandle = null;
        collider.StaticHandle = null;

        RemoveBody(go.Id);
    }
}
