using NiziKit.Application;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<Demo2DGame>(new GameDesc
        {
            Title = "DenOfIz Scene Demo - Press F1/F2 to switch scenes",
            Maximized = true,
            Resizable =  true,
        });
    }
}
