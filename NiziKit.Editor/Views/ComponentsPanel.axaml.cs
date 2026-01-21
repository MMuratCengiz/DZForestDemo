using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NiziKit.Editor.Views;

public partial class ComponentsPanel : UserControl
{
    public ComponentsPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
