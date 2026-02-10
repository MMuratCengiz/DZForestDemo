using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NiziKit.Components;

namespace NiziKit.Editor.ViewModels;

public partial class ComponentViewModel : ObservableObject
{
    private static readonly Geometry ChevronRight = Geometry.Parse("M8,5.14V19.14L19,12.14L8,5.14Z");
    private static readonly Geometry ChevronDown = Geometry.Parse("M7,10L12,15L17,10H7Z");
    private static readonly IBrush ExpandedColor = new SolidColorBrush(Color.Parse("#E0E0E8"));
    private static readonly IBrush CollapsedColor = new SolidColorBrush(Color.Parse("#606070"));

    public static IValueConverter ExpandIconConverter { get; } = new FuncValueConverter<bool, Geometry>(
        expanded => expanded ? ChevronDown : ChevronRight);

    public static IValueConverter ExpandColorConverter { get; } = new FuncValueConverter<bool, IBrush>(
        expanded => expanded ? ExpandedColor : CollapsedColor);

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

    [ObservableProperty]
    private bool _isExpanded = true;

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

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
