using NiziKit.Application;

namespace NiziKit.Editor;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<EditorGame>(new GameDesc
        {
            Title = "NiziKit Editor",
            Width = 1920,
            Height = 1080
        });
    }
}
