using System.Diagnostics.CodeAnalysis;
using Avalonia;
using NiziKit.Editor;

Editor.Run(new EditorDesc
{
    GameProjectDir = "../DZForestDemo",
    InitialScene = "Scenes/VikingShowcase.niziscene.json"
});

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public partial class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<EditorApp>()
            .UseSkia();
}
