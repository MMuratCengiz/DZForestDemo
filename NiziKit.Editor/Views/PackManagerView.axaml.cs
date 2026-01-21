using Avalonia.Controls;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class PackManagerView : UserControl
{
    public PackManagerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PackManagerViewModel vm)
        {
            // Wire up selection changed to load pack
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PackManagerViewModel.SelectedPack) && vm.SelectedPack != null)
                {
                    vm.LoadPackCommand.Execute(vm.SelectedPack);
                }
            };
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Set up TopLevel for file dialogs
        if (DataContext is PackManagerViewModel vm)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                vm.SetTopLevel(topLevel);
            }
        }
    }
}
