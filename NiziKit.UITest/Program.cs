using NiziKit.Application;

namespace NiziKit.UITest;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<UITestGame>(new GameDesc
        {
            Title = "NiziKit UI Test",
            Maximized = true,
            Resizable = true,
        });
    }
}
