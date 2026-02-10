using NiziKit.Components;

namespace NiziKit.Editor.ViewModels;

public class ComponentViewModel
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
            if (name.EndsWith("Component"))
            {
                name = name[..^9];
            }
            return name;
        }
    }

    public bool IsExpanded { get; set; } = true;

    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    public void NotifyChanged()
    {
        _component.Owner?.NotifyComponentChanged(_component);
    }

    public void Remove()
    {
        _owner.RemoveComponent(this);
    }
}
