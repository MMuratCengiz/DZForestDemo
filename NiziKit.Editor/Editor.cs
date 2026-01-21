using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Editor;

public sealed class EditorConfig
{
    public string GameProjectDir { get; init; } = ".";
    public string? InitialScene { get; init; }
    public string Title { get; init; } = "NiziKit Editor";
    public uint Width { get; init; } = 3000;
    public uint Height { get; init; } = 2000;
}

public static class Editor
{
    internal static EditorConfig Config { get; private set; } = new();

    public static void Run(
        EditorConfig? config = null,
        [CallerFilePath] string callerFilePath = "")
    {
        Config = config ?? new EditorConfig();
        var displaySize = Display.GetPrimaryDisplay().Size;
        var width = Config.Width == 0 ? (uint)displaySize.Width : Config.Width;
        var height = Config.Height == 0 ? (uint)displaySize.Height : Config.Height;

        var callerDir = Path.GetDirectoryName(callerFilePath) ?? ".";
        var gameProjectDir = Path.GetFullPath(Path.Combine(callerDir, Config.GameProjectDir));
        var assetsDir = Path.Combine(gameProjectDir, "Assets");

        Content.Initialize(assetsDir);

        var desc = new GameDesc
        {
            Title = Config.Title,
            Width = width,
            Height = height,
            Graphics = new GraphicsDesc
            {
                ApiPreference = new APIPreference
                {
                    Windows = APIPreferenceWindows.Vulkan,
                    Linux = APIPreferenceLinux.Vulkan,
                    OSX = APIPreferenceOSX.Metal
                }
            }
        };

        Game.Run<EditorGame>(desc);
    }
}
