using NiziKit.Application;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<DemoGame>(new GameDesc
        {
            Title = "DenOfIz Scene Demo - Press F1/F2 to switch scenes",
            Width = 1920,
            Height = 1080
        });
    }
}
