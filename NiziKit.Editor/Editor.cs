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
    public uint Width { get; init; } = 2560;
    public uint Height { get; init; } = 1440;
}

public static class Editor
{
    internal static EditorConfig Config { get; private set; } = new();

    public static void Run(
        EditorConfig? config = null,
        [CallerFilePath] string callerFilePath = "")
    {
        Config = config ?? new EditorConfig();

        var callerDir = Path.GetDirectoryName(callerFilePath) ?? ".";
        var gameProjectDir = Path.GetFullPath(Path.Combine(callerDir, Config.GameProjectDir));
        var assetsDir = Path.Combine(gameProjectDir, "Assets");

        Content.Initialize(assetsDir);

        var desc = new GameDesc
        {
            Title = Config.Title,
            Width = Config.Width,
            Height = Config.Height,
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
