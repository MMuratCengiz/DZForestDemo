using System.Numerics;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;
using NiziKit.UI;

namespace NiziKit.Editor.ViewModels;

public class GameObjectViewModel
{
    private readonly GameObject _gameObject;
    private readonly EditorViewModel _editor;

    public GameObjectViewModel(GameObject gameObject, EditorViewModel editor)
    {
        _gameObject = gameObject;
        _editor = editor;

        foreach (var child in gameObject.Children)
        {
            Children.Add(new GameObjectViewModel(child, editor));
        }

        foreach (var component in gameObject.Components)
        {
            Components.Add(new ComponentViewModel(component, this));
        }
    }

    public string TypeIcon
    {
        get
        {
            var objectTypeName = _gameObject.GetType().Name;
            if (objectTypeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
            {
                return FontAwesome.Lightbulb;
            }

            foreach (var component in _gameObject.Components)
            {
                var typeName = component.GetType().Name;
                if (typeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
                {
                    return FontAwesome.Lightbulb;
                }

                if (typeName.Contains("Camera", StringComparison.OrdinalIgnoreCase))
                {
                    return FontAwesome.Camera;
                }

                if (typeName.Contains("Mesh", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Renderer", StringComparison.OrdinalIgnoreCase))
                {
                    return FontAwesome.Cube;
                }
            }

            return _gameObject.Children.Count > 0 ? FontAwesome.Cube : FontAwesome.Circle;
        }
    }

    public UiColor TypeIconColor
    {
        get
        {
            var objectTypeName = _gameObject.GetType().Name;
            if (objectTypeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
            {
                return UiColor.Rgb(255, 220, 100);
            }

            foreach (var component in _gameObject.Components)
            {
                var typeName = component.GetType().Name;
                if (typeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
                {
                    return UiColor.Rgb(255, 220, 100);
                }

                if (typeName.Contains("Camera", StringComparison.OrdinalIgnoreCase))
                {
                    return UiColor.Rgb(100, 180, 160);
                }

                if (typeName.Contains("Mesh", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Renderer", StringComparison.OrdinalIgnoreCase))
                {
                    return UiColor.Rgb(150, 200, 150);
                }
            }

            return _gameObject.Children.Count > 0
                ? UiColor.Rgb(200, 200, 200)
                : UiColor.Rgb(128, 128, 128);
        }
    }

    public EditorViewModel Editor => _editor;
    public GameObject GameObject => _gameObject;

    public string Name
    {
        get => _gameObject.Name;
        set => _gameObject.Name = value;
    }

    public bool IsActive
    {
        get => _gameObject.IsActive;
        set => _gameObject.IsActive = value;
    }

    public string? Tag
    {
        get => _gameObject.Tag;
        set => _gameObject.Tag = value;
    }

    // Transform properties - read/write directly from/to GameObject
    public float PositionX
    {
        get => _gameObject.LocalPosition.X;
        set
        {
            var pos = _gameObject.LocalPosition;
            _gameObject.LocalPosition = new Vector3(value, pos.Y, pos.Z);
        }
    }

    public float PositionY
    {
        get => _gameObject.LocalPosition.Y;
        set
        {
            var pos = _gameObject.LocalPosition;
            _gameObject.LocalPosition = new Vector3(pos.X, value, pos.Z);
        }
    }

    public float PositionZ
    {
        get => _gameObject.LocalPosition.Z;
        set
        {
            var pos = _gameObject.LocalPosition;
            _gameObject.LocalPosition = new Vector3(pos.X, pos.Y, value);
        }
    }

    public float RotationX
    {
        get => GetEulerAngles().X;
        set => SetEulerAngles(value, GetEulerAngles().Y, GetEulerAngles().Z);
    }

    public float RotationY
    {
        get => GetEulerAngles().Y;
        set => SetEulerAngles(GetEulerAngles().X, value, GetEulerAngles().Z);
    }

    public float RotationZ
    {
        get => GetEulerAngles().Z;
        set => SetEulerAngles(GetEulerAngles().X, GetEulerAngles().Y, value);
    }

    public float ScaleX
    {
        get => _gameObject.LocalScale.X;
        set
        {
            var scale = _gameObject.LocalScale;
            _gameObject.LocalScale = new Vector3(value, scale.Y, scale.Z);
        }
    }

    public float ScaleY
    {
        get => _gameObject.LocalScale.Y;
        set
        {
            var scale = _gameObject.LocalScale;
            _gameObject.LocalScale = new Vector3(scale.X, value, scale.Z);
        }
    }

    public float ScaleZ
    {
        get => _gameObject.LocalScale.Z;
        set
        {
            var scale = _gameObject.LocalScale;
            _gameObject.LocalScale = new Vector3(scale.X, scale.Y, value);
        }
    }

    public List<GameObjectViewModel> Children { get; } = [];
    public List<ComponentViewModel> Components { get; } = [];

    public bool IsExpanded { get; set; } = true;
    public bool IsSelected { get; set; }
    public bool IsAddingComponent { get; set; }

    private Vector3 GetEulerAngles()
    {
        var q = _gameObject.LocalRotation;
        var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        var pitch = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp);

        var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        return new Vector3(roll, pitch, yaw) * (180f / MathF.PI);
    }

    private void SetEulerAngles(float x, float y, float z)
    {
        var euler = new Vector3(x, y, z) * (MathF.PI / 180f);
        _gameObject.LocalRotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
    }

    public void AddChild(GameObjectViewModel child)
    {
        _gameObject.AddChild(child.GameObject);
        Children.Add(child);
    }

    public void RemoveChild(GameObjectViewModel child)
    {
        _gameObject.RemoveChild(child.GameObject);
        Children.Remove(child);
    }

    public void AddComponent(IComponent component)
    {
        _gameObject.AddComponent(component);
        Components.Add(new ComponentViewModel(component, this));
    }

    public void RemoveComponent(ComponentViewModel componentVm)
    {
        _gameObject.RemoveComponent(componentVm.Component);
        Components.Remove(componentVm);
    }

    public void ToggleAddComponentPanel()
    {
        IsAddingComponent = !IsAddingComponent;
    }

    public void AddComponentOfType(Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            return;
        }

        var component = (IComponent?)Activator.CreateInstance(componentType);
        if (component != null)
        {
            FitComponentToMeshBounds(component);
            AddComponent(component);
        }

        IsAddingComponent = false;
    }

    private void FitComponentToMeshBounds(IComponent component)
    {
        var mesh = _gameObject.GetComponent<MeshComponent>()?.Mesh;
        if (mesh == null)
        {
            return;
        }

        var bounds = mesh.Bounds;
        var size = bounds.Size * _gameObject.LocalScale;
        var center = bounds.Center;

        switch (component)
        {
            case BoxCollider box:
                box.Size = size;
                box.Center = center;
                break;
            case SphereCollider sphere:
                sphere.Radius = MathF.Max(size.X, MathF.Max(size.Y, size.Z)) * 0.5f;
                sphere.Center = center;
                break;
            case CapsuleCollider capsule:
                capsule.Radius = MathF.Max(size.X, size.Z) * 0.5f;
                capsule.Height = size.Y;
                capsule.Center = center;
                break;
            case CylinderCollider cylinder:
                cylinder.Radius = MathF.Max(size.X, size.Z) * 0.5f;
                cylinder.Height = size.Y;
                cylinder.Center = center;
                break;
        }
    }

    public IEnumerable<Type> GetAvailableComponentTypes()
    {
        var existingTypes = Components.Select(c => c.Component.GetType()).ToHashSet();

        foreach (var typeName in ComponentRegistry.GetRegisteredTypes())
        {
            var type = GetComponentTypeByName(typeName);
            if (type != null && !existingTypes.Contains(type))
            {
                yield return type;
            }
        }
    }

    private static Type? GetComponentTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null && typeof(IComponent).IsAssignableFrom(type))
            {
                return type;
            }

            type = assembly.GetTypes().FirstOrDefault(t =>
                t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                typeof(IComponent).IsAssignableFrom(t));
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
