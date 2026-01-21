using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class FileBrowserGrid : UserControl
{
    public FileBrowserGrid()
    {
        InitializeComponent();
        FileGrid.DoubleTapped += OnFileGridDoubleTapped;
    }

    private void OnFileGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not FileBrowserViewModel vm)
        {
            return;
        }

        if (vm.SelectedEntry != null)
        {
            vm.HandleDoubleClick(vm.SelectedEntry);
        }
    }
}
