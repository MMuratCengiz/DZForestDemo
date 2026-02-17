using NiziKit.Components;

namespace NiziKit.Editor.ViewModels;

public class ComponentViewModel(NiziComponent component, GameObjectViewModel owner)
{
    public NiziComponent Component => component;
    public GameObjectViewModel Owner => owner;

    public string TypeName => component.GetType().Name;

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
        component.Owner?.NotifyComponentChanged(component);
    }

    public void Remove()
    {
        owner.RemoveComponent(this);
    }
}
