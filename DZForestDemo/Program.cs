using NiziKit.Application;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        var gameMode = args.Length > 0 ? args[0] : "";
        switch (gameMode)
        {
            case "--snake":
                Game.Run<SnakeGame>(new GameDesc
                {
                    Title = "Snake 3D - WASD to move, R to restart",
                    Width = 2560,
                    Height = 1440
                });
                break;
            default:
                Game.Run<DemoGame>(new GameDesc
                {
                    Title = "DenOfIz Scene Demo - Press F1/F2 to switch scenes",
                    Width = 1920,
                    Height = 1080
                });
                break;
        }
    }
}
