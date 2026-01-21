using Avalonia.Themes.Fluent;

namespace NiziKit.Editor;

public class EditorApp : Avalonia.Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
