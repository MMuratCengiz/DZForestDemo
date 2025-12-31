using System.Numerics;

namespace DenOfIz.World;

public class SceneObject(string name = "SceneObject")
{
    private static int _nextId = 1;

    public int Id { get; } = _nextId++;
    public string Name { get; set; } = name;
    public bool IsActive { get; set; } = true;

    public Scene? Scene { get; internal set; }
    public SceneObject? Parent { get; private set; }

    private readonly List<SceneObject> _children = new();
    public IReadOnlyList<SceneObject> Children => _children;

    private Vector3 _localPosition;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    private bool _transformDirty = true;

    public Vector3 LocalPosition
    {
        get => _localPosition;
        set { _localPosition = value; MarkTransformDirty(); }
    }

    public Quaternion LocalRotation
    {
        get => _localRotation;
        set { _localRotation = value; MarkTransformDirty(); }
    }

    public Vector3 LocalScale
    {
        get => _localScale;
        set { _localScale = value; MarkTransformDirty(); }
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

    public SceneObject CreateChild(string name = "SceneObject")
    {
        var child = new SceneObject(name);
        AddChild(child);
        return child;
    }

    public void AddChild(SceneObject child)
    {
        child.Parent?._children.Remove(child);
        child.Parent = this;
        child.Scene = Scene;
        _children.Add(child);
        child.MarkTransformDirty();
    }

    public void RemoveChild(SceneObject child)
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
}
