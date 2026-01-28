using Avalonia;
using NiziKit.Editor;

Editor.Run(new EditorDesc
{
    GameProjectDir = "../DZForestDemo",
    InitialScene = "Scenes/VikingShowcase.niziscene.json"
});

namespace DZForestDemo.Editor
{
    public partial class Program
    {
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<EditorApp>()
                .UseSkia();
    }
}
