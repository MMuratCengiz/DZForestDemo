using Application;
using DenOfIz;
using Graphics;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        var demoGame = new DemoGame();
        var renderer = new DemoRenderer(demoGame);
        demoGame.SetRenderer(renderer);

        var desc = new GameDesc
        {
            Title = "DenOfIz Scene Demo - Press F1/F2 to switch scenes",
            Width = 1920,
            Height = 1080,
            Graphics = new GraphicsDesc
            {
                NumFrames = 3,
                BackBufferFormat = Format.B8G8R8A8Unorm,
                DepthBufferFormat = Format.D32Float,
                ApiPreference = new APIPreference
                {
                    Windows = APIPreferenceWindows.Directx12
                }
            }
        };

        using var game = new Game(demoGame, renderer, desc);
        game.Run();
    }
}
