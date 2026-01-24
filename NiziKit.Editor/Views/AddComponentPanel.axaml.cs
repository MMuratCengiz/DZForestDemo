using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class AddComponentPanel : UserControl
{
    private ItemsControl? _componentTypesList;
    private GameObjectViewModel? _currentViewModel;

    public AddComponentPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _componentTypesList = this.FindControl<ItemsControl>("ComponentTypesList");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_currentViewModel != null)
        {
            _currentViewModel.Components.CollectionChanged -= OnComponentsCollectionChanged;
        }

        _currentViewModel = DataContext as GameObjectViewModel;

        if (_currentViewModel != null)
        {
            _currentViewModel.Components.CollectionChanged += OnComponentsCollectionChanged;
        }

        RefreshComponentTypes();
    }

    private void OnComponentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshComponentTypes();
    }

    public void RefreshComponentTypes()
    {
        if (_componentTypesList == null || DataContext is not GameObjectViewModel vm)
        {
            return;
        }

        var componentTypes = vm.GetAvailableComponentTypes().ToList();
        _componentTypesList.ItemsSource = componentTypes;
    }

    private void OnComponentTypeClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Type componentType)
        {
            if (DataContext is GameObjectViewModel vm)
            {
                vm.AddComponentOfType(componentType);
            }
        }
    }
}
