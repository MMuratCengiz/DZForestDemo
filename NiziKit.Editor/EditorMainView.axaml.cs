using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        };
    }

    public void RefreshScene()
    {
        _viewModel?.LoadFromCurrentScene();
    }
}
