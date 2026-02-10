using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class ComponentsPanel : UserControl
{
    private SmoothScrollViewer? _inspectorScroll;
    private GameObjectViewModel? _lastSelectedObject;

    public ComponentsPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _inspectorScroll = this.FindControl<SmoothScrollViewer>("InspectorScroll");
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is EditorViewModel vm)
        {
            vm.PropertyChanged += OnEditorPropertyChanged;
        }
    }

    private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.SelectedGameObject) && _inspectorScroll != null)
        {
            var vm = (EditorViewModel)sender!;
            if (vm.SelectedGameObject != _lastSelectedObject)
            {
                _lastSelectedObject = vm.SelectedGameObject;
                _inspectorScroll.ScrollToTop();
            }
        }
    }
}
