using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
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
        else if (component is WheelColliderComponent wheelCollider)
        {
            TryRegisterWheelCollider(go, wheelCollider);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is RigidbodyComponent)
        {
            Unregister(go);
        }
        else if (component is WheelColliderComponent wheelCollider)
        {
            UnregisterWheelCollider(go, wheelCollider);
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
        if (component is RigidbodyComponent)
        {
            Unregister(go);
            TryRegister(go);
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
}
