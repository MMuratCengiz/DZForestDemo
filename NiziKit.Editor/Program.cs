using DenOfIz;
using NiziKit.Application;
using NiziKit.Graphics;

namespace NiziKit.Editor;

internal static class Program
{
    private static void Main(string[] args)
    {
        Game.Run<EditorGame>(new GameDesc
        {
            Title = "NiziKit Editor",
            Width = 2560,
            Height = 1440,
            Graphics = new GraphicsDesc
            {
                ApiPreference = new APIPreference
                {
                    Windows = APIPreferenceWindows.Vulkan
                }
            }
        });
    }
}
