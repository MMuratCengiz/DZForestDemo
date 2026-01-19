using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Components;

public interface IComponent
{
    GameObject? Owner { get; set; }

    void Initialize() { }
    void Begin() { }
    void Update() { }
    void PostUpdate() { }
    void PhysicsUpdate() { }
    void OnDestroy() { }

    void OnCollisionEnter(in Collision collision) { }
    void OnCollisionStay(in Collision collision) { }
    void OnCollisionExit(in Collision collision) { }

    T? GetComponent<T>() where T : class, IComponent => Owner?.GetComponent<T>();
    T AddComponent<T>() where T : IComponent, new() => Owner!.AddComponent<T>();
    void AddComponent(IComponent component) => Owner?.AddComponent(component);
    bool HasComponent<T>() where T : class, IComponent => Owner?.HasComponent<T>() ?? false;
}
