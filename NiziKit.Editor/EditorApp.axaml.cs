using Avalonia.Markup.Xaml;

namespace NiziKit.Editor;

public partial class EditorApp : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
