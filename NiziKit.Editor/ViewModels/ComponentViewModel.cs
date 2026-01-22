using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Components;

namespace NiziKit.Editor.ViewModels;

public partial class ComponentViewModel : ObservableObject
{
    private readonly IComponent _component;
    private readonly GameObjectViewModel _owner;

    public ComponentViewModel(IComponent component, GameObjectViewModel owner)
    {
        _component = component;
        _owner = owner;
    }

    public IComponent Component => _component;
    public GameObjectViewModel Owner => _owner;

    public string TypeName => _component.GetType().Name;

    public string DisplayName
    {
        get
        {
            var name = TypeName;
            // Remove "Component" suffix if present for cleaner display
            if (name.EndsWith("Component"))
            {
                name = name[..^9];
            }
            return name;
        }
    }

    [ObservableProperty]
    private bool _isExpanded = true;

    public void NotifyChanged()
    {
        _component.Owner?.NotifyComponentChanged(_component);
    }

    [RelayCommand]
    private void Remove()
    {
        _owner.RemoveComponent(this);
    }
}
