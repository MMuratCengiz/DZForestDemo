using DenOfIz;
using DenOfIz.World.Application;
using DenOfIz.World.Graphics;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
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

        using var game = new DemoGame(desc);
        game.Run();
    }
}
