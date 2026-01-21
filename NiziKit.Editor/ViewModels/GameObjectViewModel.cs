using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Editor.ViewModels;

public partial class GameObjectViewModel : ObservableObject
{
    private readonly GameObject _gameObject;
    private readonly EditorViewModel _editor;

    public static IValueConverter ActiveToOpacityConverter { get; } = new FuncValueConverter<bool, double>(
        isActive => isActive ? 1.0 : 0.5);

    private static readonly string CubeIcon = "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L5,8.09V15.91L12,19.85L19,15.91V8.09L12,4.15Z";
    private static readonly string LightIcon = "M12,2A7,7 0 0,0 5,9C5,11.38 6.19,13.47 8,14.74V17A1,1 0 0,0 9,18H15A1,1 0 0,0 16,17V14.74C17.81,13.47 19,11.38 19,9A7,7 0 0,0 12,2M9,21A1,1 0 0,0 10,22H14A1,1 0 0,0 15,21V20H9V21Z";
    private static readonly string CameraIcon = "M4,4H7L9,2H15L17,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4M12,7A5,5 0 0,0 7,12A5,5 0 0,0 12,17A5,5 0 0,0 17,12A5,5 0 0,0 12,7M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z";
    private static readonly string EmptyIcon = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z";

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

    public string TypeIconData
    {
        get
        {
            foreach (var component in _gameObject.Components)
            {
                var typeName = component.GetType().Name;
                if (typeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
                {
                    return LightIcon;
                }

                if (typeName.Contains("Camera", StringComparison.OrdinalIgnoreCase))
                {
                    return CameraIcon;
                }

                if (typeName.Contains("Mesh", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Renderer", StringComparison.OrdinalIgnoreCase))
                {
                    return CubeIcon;
                }
            }

            if (_gameObject.Children.Count > 0)
            {
                return CubeIcon;
            }

            return EmptyIcon;
        }
    }

    public IBrush TypeIconColor
    {
        get
        {
            foreach (var component in _gameObject.Components)
            {
                var typeName = component.GetType().Name;
                if (typeName.Contains("Light", StringComparison.OrdinalIgnoreCase))
                {
                    return new SolidColorBrush(Color.FromRgb(255, 220, 100));
                }

                if (typeName.Contains("Camera", StringComparison.OrdinalIgnoreCase))
                {
                    return new SolidColorBrush(Color.FromRgb(100, 180, 255));
                }

                if (typeName.Contains("Mesh", StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains("Renderer", StringComparison.OrdinalIgnoreCase))
                {
                    return new SolidColorBrush(Color.FromRgb(150, 200, 150));
                }
            }

            if (_gameObject.Children.Count > 0)
            {
                return new SolidColorBrush(Color.FromRgb(200, 200, 200));
            }

            return new SolidColorBrush(Color.FromRgb(128, 128, 128));
        }
    }

    public EditorViewModel Editor => _editor;
    public GameObject GameObject => _gameObject;

    public string Name
    {
        get => _gameObject.Name;
        set
        {
            if (_gameObject.Name != value)
            {
                _gameObject.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsActive
    {
        get => _gameObject.IsActive;
        set
        {
            if (_gameObject.IsActive != value)
            {
                _gameObject.IsActive = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Tag
    {
        get => _gameObject.Tag;
        set
        {
            if (_gameObject.Tag != value)
            {
                _gameObject.Tag = value;
                OnPropertyChanged();
            }
        }
    }

    // Transform properties
    public float PositionX
    {
        get => _gameObject.LocalPosition.X;
        set
        {
            var pos = _gameObject.LocalPosition;
            if (pos.X != value)
            {
                _gameObject.LocalPosition = new Vector3(value, pos.Y, pos.Z);
                OnPropertyChanged();
            }
        }
    }

    public float PositionY
    {
        get => _gameObject.LocalPosition.Y;
        set
        {
            var pos = _gameObject.LocalPosition;
            if (pos.Y != value)
            {
                _gameObject.LocalPosition = new Vector3(pos.X, value, pos.Z);
                OnPropertyChanged();
            }
        }
    }

    public float PositionZ
    {
        get => _gameObject.LocalPosition.Z;
        set
        {
            var pos = _gameObject.LocalPosition;
            if (pos.Z != value)
            {
                _gameObject.LocalPosition = new Vector3(pos.X, pos.Y, value);
                OnPropertyChanged();
            }
        }
    }

    // Rotation in euler angles (degrees)
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
            if (scale.X != value)
            {
                _gameObject.LocalScale = new Vector3(value, scale.Y, scale.Z);
                OnPropertyChanged();
            }
        }
    }

    public float ScaleY
    {
        get => _gameObject.LocalScale.Y;
        set
        {
            var scale = _gameObject.LocalScale;
            if (scale.Y != value)
            {
                _gameObject.LocalScale = new Vector3(scale.X, value, scale.Z);
                OnPropertyChanged();
            }
        }
    }

    public float ScaleZ
    {
        get => _gameObject.LocalScale.Z;
        set
        {
            var scale = _gameObject.LocalScale;
            if (scale.Z != value)
            {
                _gameObject.LocalScale = new Vector3(scale.X, scale.Y, value);
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<GameObjectViewModel> Children { get; } = [];
    public ObservableCollection<ComponentViewModel> Components { get; } = [];

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            _editor.SelectedGameObject = this;
        }
    }

    private Vector3 GetEulerAngles()
    {
        var q = _gameObject.LocalRotation;
        // Convert quaternion to euler angles (in degrees)
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
        OnPropertyChanged(nameof(RotationX));
        OnPropertyChanged(nameof(RotationY));
        OnPropertyChanged(nameof(RotationZ));
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

    public void AddComponent(NiziKit.Components.IComponent component)
    {
        _gameObject.AddComponent(component);
        Components.Add(new ComponentViewModel(component, this));
    }

    public void RemoveComponent(ComponentViewModel componentVm)
    {
        _gameObject.RemoveComponent(componentVm.Component);
        Components.Remove(componentVm);
    }

    [ObservableProperty]
    private bool _isAddingComponent;

    [RelayCommand]
    private void ToggleAddComponentPanel()
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
            AddComponent(component);
        }

        IsAddingComponent = false;
    }

    public IEnumerable<Type> GetAvailableComponentTypes()
    {
        // Get component types that aren't already on this object
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
        // Try to find the type in loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null && typeof(IComponent).IsAssignableFrom(type))
            {
                return type;
            }

            // Also try by simple name
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

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(PositionX));
        OnPropertyChanged(nameof(PositionY));
        OnPropertyChanged(nameof(PositionZ));
        OnPropertyChanged(nameof(RotationX));
        OnPropertyChanged(nameof(RotationY));
        OnPropertyChanged(nameof(RotationZ));
        OnPropertyChanged(nameof(ScaleX));
        OnPropertyChanged(nameof(ScaleY));
        OnPropertyChanged(nameof(ScaleZ));
    }
}
