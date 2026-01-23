using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;
using NiziKit.Editor.Views.Editors;

namespace NiziKit.Editor;

public partial class EditorMainView : UserControl
{
    private EditorViewModel? _viewModel;
    private AssetPickerDialog? _assetPickerDialog;

    public EditorViewModel? ViewModel => _viewModel;

    public EditorMainView()
    {
        AvaloniaXamlLoader.Load(this);
        _assetPickerDialog = this.FindControl<AssetPickerDialog>("AssetPickerDialog");
    }

    public void Initialize()
    {
        _viewModel = new EditorViewModel();
        DataContext = _viewModel;
        _viewModel.LoadFromCurrentScene();

        if (_assetPickerDialog != null)
        {
            _assetPickerDialog.AssetSelected += asset =>
            {
                _viewModel.OnAssetPickerSelected(asset);
            };
            _assetPickerDialog.Cancelled += () =>
            {
                _viewModel.CloseAssetPickerCommand.Execute(null);
            };
        }

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EditorViewModel.IsAssetPickerOpen) && _viewModel.IsAssetPickerOpen)
            {
                _assetPickerDialog?.Initialize(
                    _viewModel.AssetBrowser,
                    _viewModel.AssetPickerAssetType,
                    _viewModel.AssetPickerCurrentPack,
                    _viewModel.AssetPickerCurrentAssetName);
            }

            if (e.PropertyName == nameof(EditorViewModel.SceneBrowserViewModel) && _viewModel.SceneBrowserViewModel != null)
            {
                _viewModel.SceneBrowserViewModel.FileDoubleClicked += OnSceneBrowserFileDoubleClicked;
            }
        };
    }

    private void OnSceneBrowserFileDoubleClicked(FileEntry entry)
    {
        if (entry.Type == AssetFileType.Scene)
        {
            _viewModel?.OnSceneFileSelected(entry.FullPath);
        }
    }

    public void RefreshScene()
    {
        _viewModel?.LoadFromCurrentScene();
    }

    private void OnContentBrowserTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ContentTab tab && _viewModel != null)
        {
            _viewModel.ContentBrowserViewModel.SelectedTab = tab;
        }
    }
}
