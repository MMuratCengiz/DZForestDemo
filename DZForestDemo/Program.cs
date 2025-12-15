using Application;
using DenOfIz;
using ECS;
using Graphics;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        AppBuilder.Create()
            .WithTitle("DenOfIz Window Mode")
            .WithSize(1920, 1080)
            .WithNumFrames(3)
            .WithBackBufferFormat(Format.B8G8R8A8Unorm)
            .WithDepthBufferFormat(Format.D32Float)
            .WithApiPreference(new APIPreference
            {
                Windows = APIPreferenceWindows.Directx12
            })
            .WithGraphics()
            .WithPhysics()
            .WithAssets()
            .AddSystem(new GameSystem(), Schedule.Render)
            .Run();
    }
}
