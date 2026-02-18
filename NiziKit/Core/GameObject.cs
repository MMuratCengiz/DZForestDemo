using System.Numerics;
using NiziKit.Components;

namespace NiziKit.Core;

public class GameObject(string name = "GameObject")
{
    private static int _nextId = 1;

    public int Id { get; } = _nextId++;
    public string Name { get; set; } = name;
    public string? Tag { get; set; }
    public bool IsActive { get; set; } = true;

    public GameObject? Parent { get; private set; }
    public Scene? Scene { get; internal set; }

    internal bool IsInWorld { get; set; }
    internal bool HasInitialized { get; set; }
    internal bool HasBegun { get; set; }
    internal bool IsDestroying { get; set; }

    private readonly List<GameObject> _children = [];
    public IReadOnlyList<GameObject> Children => _children;

    private readonly List<NiziComponent> _components = [];
    public IReadOnlyList<NiziComponent> Components => _components;

    private Vector3 _localPosition;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    private bool _transformDirty = true;

    public Vector3 LocalPosition
    {
        get => _localPosition;
        set
        {
            _localPosition = value;
            MarkTransformDirty();
        }
    }

    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            _localRotation = value;
            MarkTransformDirty();
        }
    }

    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            _localScale = value;
            MarkTransformDirty();
        }
    }

    public Matrix4x4 WorldMatrix
    {
        get
        {
            if (_transformDirty)
            {
                UpdateWorldMatrix();
            }

            return _worldMatrix;
        }
    }

    public Vector3 WorldPosition
    {
        get => WorldMatrix.Translation;
        set
        {
            if (Parent != null)
            {
                Matrix4x4.Invert(Parent.WorldMatrix, out var invParent);
                LocalPosition = Vector3.Transform(value, invParent);
            }
            else
            {
                LocalPosition = value;
            }
        }
    }

    public Quaternion WorldRotation
    {
        get
        {
            Matrix4x4.Decompose(WorldMatrix, out _, out var rotation, out _);
            return rotation;
        }
        set
        {
            if (Parent != null)
            {
                LocalRotation = Quaternion.Inverse(Parent.WorldRotation) * value;
            }
            else
            {
                LocalRotation = value;
            }
        }
    }

    public Vector3 Forward => Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, LocalRotation));
    public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, LocalRotation));
    public Vector3 Up => Vector3.Normalize(Vector3.Transform(Vector3.UnitY, LocalRotation));

    public void Translate(Vector3 translation, Space relativeTo = Space.Self)
    {
        if (relativeTo == Space.Self)
        {
            LocalPosition += Vector3.Transform(translation, LocalRotation);
        }
        else
        {
            WorldPosition += translation;
        }
    }

    public void Rotate(Vector3 eulerAngles, Space relativeTo = Space.Self)
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(
            eulerAngles.Y * (MathF.PI / 180f),
            eulerAngles.X * (MathF.PI / 180f),
            eulerAngles.Z * (MathF.PI / 180f));
        Rotate(rotation, relativeTo);
    }

    public void Rotate(Quaternion rotation, Space relativeTo = Space.Self)
    {
        if (relativeTo == Space.Self)
        {
            LocalRotation = Quaternion.Normalize(LocalRotation * rotation);
        }
        else
        {
            LocalRotation = Quaternion.Normalize(rotation * LocalRotation);
        }
    }

    public void LookAt(Vector3 target, Vector3? worldUp = null)
    {
        var up = worldUp ?? Vector3.UnitY;
        var direction = target - WorldPosition;
        if (direction.LengthSquared() < 0.000001f)
            return;

        var forward = Vector3.Normalize(direction);
        var right = Vector3.Cross(up, forward);

        if (right.LengthSquared() < 0.000001f)
        {
            right = Vector3.Cross(forward.Y > 0 ? -Vector3.UnitZ : Vector3.UnitZ, forward);
        }

        right = Vector3.Normalize(right);
        var correctedUp = Vector3.Cross(forward, right);

        var matrix = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            correctedUp.X, correctedUp.Y, correctedUp.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);

        var worldRot = Quaternion.CreateFromRotationMatrix(matrix);

        if (Parent != null)
        {
            LocalRotation = Quaternion.Inverse(Parent.WorldRotation) * worldRot;
        }
        else
        {
            LocalRotation = worldRot;
        }
    }

    public void SetParent(GameObject? newParent)
    {
        if (newParent == this || newParent == Parent)
            return;

        if (newParent == null)
        {
            Parent?.RemoveChild(this);
        }
        else
        {
            newParent.AddChild(this);
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
    }

    public void Destroy()
    {
        Scene?.Destroy(this);
    }

    public GameObject CreateChild(string name = "SceneObject")
    {
        var child = new GameObject(name);
        AddChild(child);
        return child;
    }

    public void AddChild(GameObject child)
    {
        child.Parent?._children.Remove(child);
        child.Parent = this;
        _children.Add(child);
        child.MarkTransformDirty();
        if (IsInWorld && !child.IsInWorld)
        {
            World.OnGameObjectCreated(child);
        }
    }

    public void RemoveChild(GameObject child)
    {
        if (_children.Remove(child))
        {
            if (child.IsInWorld)
            {
                World.OnGameObjectDestroyed(child);
            }
            child.Parent = null;
        }
    }

    public GameObject? FindChild(string name)
    {
        foreach (var child in _children)
        {
            if (child.Name == name) return child;
        }

        foreach (var child in _children)
        {
            var found = child.FindChild(name);
            if (found != null) return found;
        }

        return null;
    }

    private void MarkTransformDirty()
    {
        _transformDirty = true;
        foreach (var child in _children)
        {
            child.MarkTransformDirty();
        }
    }

    private void UpdateWorldMatrix()
    {
        var local = Matrix4x4.CreateScale(_localScale) *
                    Matrix4x4.CreateFromQuaternion(_localRotation) *
                    Matrix4x4.CreateTranslation(_localPosition);

        _worldMatrix = Parent != null ? local * Parent.WorldMatrix : local;
        _transformDirty = false;
    }

    public T? GetComponent<T>() where T : NiziComponent
    {
        foreach (var component in _components)
        {
            if (component is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    public List<T> GetComponents<T>() where T : NiziComponent
    {
        var results = new List<T>();
        foreach (var component in _components)
        {
            if (component is T typed) results.Add(typed);
        }
        return results;
    }

    public T? GetComponentInChildren<T>() where T : NiziComponent
    {
        var result = GetComponent<T>();
        if (result != null) return result;

        foreach (var child in _children)
        {
            result = child.GetComponentInChildren<T>();
            if (result != null) return result;
        }

        return null;
    }

    public List<T> GetComponentsInChildren<T>() where T : NiziComponent
    {
        var results = new List<T>();
        GetComponentsInChildrenRecursive(results);
        return results;
    }

    private void GetComponentsInChildrenRecursive<T>(List<T> results) where T : NiziComponent
    {
        foreach (var component in _components)
        {
            if (component is T typed) results.Add(typed);
        }

        foreach (var child in _children)
        {
            child.GetComponentsInChildrenRecursive(results);
        }
    }

    public T? GetComponentInParent<T>() where T : NiziComponent
    {
        var result = GetComponent<T>();
        if (result != null) return result;
        return Parent?.GetComponentInParent<T>();
    }

    public T AddComponent<T>() where T : NiziComponent, new()
    {
        var component = new T
        {
            Owner = this
        };
        _components.Add(component);
        if (IsInWorld)
        {
            World.OnComponentAdded(this, component);
        }

        return component;
    }

    public void AddComponent(NiziComponent component)
    {
        component.Owner = this;
        _components.Add(component);
        if (IsInWorld)
        {
            World.OnComponentAdded(this, component);
        }
    }

    public bool RemoveComponent<T>() where T : NiziComponent
    {
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            if (_components[i] is T component)
            {
                _components.RemoveAt(i);
                component.Owner = null;
                if (IsInWorld)
                {
                    World.OnComponentRemoved(this, component);
                }

                return true;
            }
        }

        return false;
    }

    public bool RemoveComponent(NiziComponent component)
    {
        var index = _components.IndexOf(component);
        if (index < 0)
        {
            return false;
        }

        _components.RemoveAt(index);
        component.Owner = null;
        if (IsInWorld)
        {
            World.OnComponentRemoved(this, component);
        }

        return true;
    }

    public void NotifyComponentChanged(NiziComponent component)
    {
        if (IsInWorld)
        {
            World.OnComponentChanged(this, component);
        }
    }

    public bool HasComponent<T>() where T : NiziComponent
    {
        return GetComponent<T>() != null;
    }
}
