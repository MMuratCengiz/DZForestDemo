using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

        AddHandler(DragDrop.DragOverEvent, OnMainDragOver);
        AddHandler(DragDrop.DropEvent, OnMainDrop);
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
                    _viewModel.AssetPickerCurrentAssetPath);
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

    private void OnMainDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void OnMainDrop(object? sender, DragEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        var files = e.Data.GetFiles();
        if (files == null)
        {
            return;
        }

        var importPaths = new List<string>();
        foreach (var item in files)
        {
            if (item.TryGetLocalPath() is { } path && ImportViewModel.IsImportableFile(path))
            {
                importPaths.Add(path);
            }
        }

        if (importPaths.Count > 0)
        {
            _viewModel.OpenImportPanelWithFiles(importPaths);
            e.Handled = true;
        }
    }
}
