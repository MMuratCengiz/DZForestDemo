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
}
