using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor;

public partial class EditorMainView : UserControl
{
    private EditorViewModel? _viewModel;

    public EditorViewModel? ViewModel => _viewModel;

    public EditorMainView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Initialize()
    {
        _viewModel = new EditorViewModel();
        DataContext = _viewModel;
        _viewModel.LoadFromCurrentScene();
    }

    public void RefreshScene()
    {
        _viewModel?.LoadFromCurrentScene();
    }
}
