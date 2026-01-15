using DenOfIz;
using NiziKit.Application;
using NiziKit.Graphics;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<DemoGame>(new GameDesc
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
                    Windows = APIPreferenceWindows.Directx12,
                    OSX = APIPreferenceOSX.Metal
                }
            }
        });
    }
}
