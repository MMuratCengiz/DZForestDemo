using Avalonia;
using NiziKit.Editor;

Editor.Run(new EditorConfig
{
    GameProjectDir = "../DZForestDemo",
    InitialScene = "Scenes/VikingShowcase.niziscene.json"
});

public partial class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<EditorApp>()
            .UseSkia();
}
