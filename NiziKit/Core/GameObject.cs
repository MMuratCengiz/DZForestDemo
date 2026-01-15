using System.Numerics;
using NiziKit.Components;

namespace NiziKit.Core;

public class GameObject(string name = "GameObject")
{
    private static int _nextId = 1;

    public int Id { get; } = _nextId++;
    public string Name { get; set; } = name;
    public bool IsActive { get; set; } = true;

    public GameObject? Parent { get; private set; }

    internal bool IsInWorld { get; set; }

    private readonly List<GameObject> _children = [];
    public IReadOnlyList<GameObject> Children => _children;

    private readonly List<IComponent> _components = [];
    public IReadOnlyList<IComponent> Components => _components;

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

    public Vector3 WorldPosition => WorldMatrix.Translation;

    public virtual void Load()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void Update()
    {
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
    }

    public void RemoveChild(GameObject child)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
        }
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

    public T? GetComponent<T>() where T : class, IComponent
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

    public T AddComponent<T>() where T : IComponent, new()
    {
        var component = new T { };
        _components.Add(component);
        if (IsInWorld) World.OnComponentAdded(this, component);
        return component;
    }

    public void AddComponent(IComponent component)
    {
        _components.Add(component);
        if (IsInWorld) World.OnComponentAdded(this, component);
    }

    public bool RemoveComponent<T>() where T : class, IComponent
    {
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            if (_components[i] is T component)
            {
                _components.RemoveAt(i);
                if (IsInWorld) World.OnComponentRemoved(this, component);
                return true;
            }
        }

        return false;
    }

    public bool HasComponent<T>() where T : class, IComponent
    {
        return GetComponent<T>() != null;
    }
}