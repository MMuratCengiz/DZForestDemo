using System.Numerics;
using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Components;

public abstract class NiziComponent
{
    public GameObject? Owner { get; set; }

    [HideInInspector] public GameObject GameObject => Owner!;
    [HideInInspector] public string Name { get => Owner!.Name; set => Owner!.Name = value; }
    [HideInInspector] public string? Tag { get => Owner?.Tag; set { if (Owner != null)
        {
            Owner.Tag = value;
        }
    } }
    [HideInInspector] public bool IsActive => Owner?.IsActive ?? false;

    [HideInInspector] public Vector3 Position { get => Owner!.WorldPosition; set => Owner!.WorldPosition = value; }
    [HideInInspector] public Vector3 LocalPosition { get => Owner!.LocalPosition; set => Owner!.LocalPosition = value; }
    [HideInInspector] public Quaternion Rotation { get => Owner!.WorldRotation; set => Owner!.WorldRotation = value; }
    [HideInInspector] public Quaternion LocalRotation { get => Owner!.LocalRotation; set => Owner!.LocalRotation = value; }
    [HideInInspector] public Vector3 LocalScale { get => Owner!.LocalScale; set => Owner!.LocalScale = value; }

    [HideInInspector] public Vector3 Forward => Owner!.Forward;
    [HideInInspector] public Vector3 Right => Owner!.Right;
    [HideInInspector] public Vector3 Up => Owner!.Up;

    public virtual void Initialize() { }
    public virtual void Begin() { }
    public virtual void Update() { }
    public virtual void PostUpdate() { }
    public virtual void PhysicsUpdate() { }
    public virtual void OnDestroy() { }

    public virtual void OnCollisionEnter(in Collision collision) { }
    public virtual void OnCollisionStay(in Collision collision) { }
    public virtual void OnCollisionExit(in Collision collision) { }

    public void SetActive(bool active) => Owner!.SetActive(active);

    public void Translate(Vector3 translation, Space relativeTo = Space.Self) => Owner!.Translate(translation, relativeTo);
    public void Rotate(Vector3 eulerAngles, Space relativeTo = Space.Self) => Owner!.Rotate(eulerAngles, relativeTo);
    public void Rotate(Quaternion rotation, Space relativeTo = Space.Self) => Owner!.Rotate(rotation, relativeTo);
    public void LookAt(Vector3 target, Vector3? worldUp = null) => Owner!.LookAt(target, worldUp);

    public T? GetComponent<T>() where T : NiziComponent => Owner?.GetComponent<T>();
    public List<T> GetComponents<T>() where T : NiziComponent => Owner!.GetComponents<T>();
    public T? GetComponentInChildren<T>() where T : NiziComponent => Owner!.GetComponentInChildren<T>();
    public List<T> GetComponentsInChildren<T>() where T : NiziComponent => Owner!.GetComponentsInChildren<T>();
    public T? GetComponentInParent<T>() where T : NiziComponent => Owner!.GetComponentInParent<T>();
    public T AddComponent<T>() where T : NiziComponent, new() => Owner!.AddComponent<T>();
    public void AddComponent(NiziComponent component) => Owner?.AddComponent(component);
    public bool RemoveComponent<T>() where T : NiziComponent => Owner!.RemoveComponent<T>();
    public bool RemoveComponent(NiziComponent component) => Owner!.RemoveComponent(component);
    public bool HasComponent<T>() where T : NiziComponent => Owner?.HasComponent<T>() ?? false;

    public GameObject? FindChild(string name) => Owner!.FindChild(name);
    public void SetParent(GameObject? parent) => Owner!.SetParent(parent);

    public void Destroy() => Owner!.Destroy();
    public static void Destroy(GameObject go) => go.Destroy();
    public static T? FindObjectOfType<T>() where T : NiziComponent => World.CurrentScene?.FindComponent<T>();
    public static IEnumerable<T> FindObjectsOfType<T>() where T : NiziComponent => World.CurrentScene?.FindComponents<T>() ?? [];
    public static GameObject? FindWithTag(string tag) => World.FindObjectWithTag(tag);
    public static List<GameObject> FindObjectsWithTag(string tag) => World.FindObjectsWithTag(tag);
}
