using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Components;

/// <summary>
/// Base class for all components. Extend this to create custom components.
/// Public properties are automatically serialized and visible in the editor.
/// </summary>
public abstract class NiziComponent
{
    /// <summary>
    /// The GameObject this component is attached to.
    /// </summary>
    public GameObject? Owner { get; set; }

    public virtual void Initialize() { }
    public virtual void Begin() { }
    public virtual void Update() { }
    public virtual void PostUpdate() { }
    public virtual void PhysicsUpdate() { }
    public virtual void OnDestroy() { }

    public virtual void OnCollisionEnter(in Collision collision) { }
    public virtual void OnCollisionStay(in Collision collision) { }
    public virtual void OnCollisionExit(in Collision collision) { }

    public T? GetComponent<T>() where T : NiziComponent => Owner?.GetComponent<T>();
    public T AddComponent<T>() where T : NiziComponent, new() => Owner!.AddComponent<T>();
    public void AddComponent(NiziComponent component) => Owner?.AddComponent(component);
    public bool HasComponent<T>() where T : NiziComponent => Owner?.HasComponent<T>() ?? false;
}
