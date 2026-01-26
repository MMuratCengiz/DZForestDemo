using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NiziKit.Editor.Views;

public partial class ViewportToolbar : UserControl
{
    public ViewportToolbar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
