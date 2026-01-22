using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class PackManagerView : UserControl
{
    private PackManagerViewModel? _subscribedVm;

    public PackManagerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_subscribedVm != null)
        {
            _subscribedVm.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedVm = null;
        }

        if (DataContext is PackManagerViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedVm = vm;
            TrySetTopLevel(vm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (sender is not PackManagerViewModel vm)
        {
            return;
        }

        if (args.PropertyName == nameof(PackManagerViewModel.SelectedPack) && vm.SelectedPack != null)
        {
            vm.LoadPackCommand.Execute(vm.SelectedPack);
        }
        else if (args.PropertyName == nameof(PackManagerViewModel.EditingAssetType))
        {
            UpdateAssetEditorVisibility(vm.EditingAssetType);
        }
    }

    private void UpdateAssetEditorVisibility(AssetFileType type)
    {
        var materialPanel = this.FindControl<StackPanel>("MaterialEditorPanel");
        var shaderPanel = this.FindControl<StackPanel>("ShaderEditorPanel");

        if (materialPanel != null)
        {
            materialPanel.IsVisible = type == AssetFileType.Material;
        }
        if (shaderPanel != null)
        {
            shaderPanel.IsVisible = type == AssetFileType.Shader;
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is PackManagerViewModel vm)
        {
            TrySetTopLevel(vm);
        }
    }

    private void TrySetTopLevel(PackManagerViewModel vm)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            vm.SetTopLevel(topLevel);
        }
    }

    private void OnAssetCardDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: PackAssetEntry entry } &&
            DataContext is PackManagerViewModel vm)
        {
            vm.OpenAssetEditorCommand.Execute(entry);
        }
    }
}
