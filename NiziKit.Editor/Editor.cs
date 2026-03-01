using System.Reflection;
using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Editor;

public sealed class EditorDesc
{
    public string GameProjectDir { get; init; } = ".";
    public string? InitialScene { get; init; }
    public string Title { get; init; } = "NiziKit Editor";
    public uint Width { get; init; } = 0;
    public uint Height { get; init; } = 0;
    public Type? RendererType { get; init; }
}

public static class Editor
{
    internal static EditorDesc Desc { get; private set; } = new();

    public static void Run(
        EditorDesc? config = null,
        [CallerFilePath] string callerFilePath = "")
    {
        LoadAssembliesFromBaseDirectory();

        Desc = config ?? new EditorDesc();
        var callerDir = Path.GetDirectoryName(callerFilePath) ?? ".";
        var gameProjectDir = Path.GetFullPath(Path.Combine(callerDir, Desc.GameProjectDir));
        var assetsDir = Path.Combine(gameProjectDir, "Assets");

        Content.Initialize(assetsDir);

        var engineShaderDir = FindEngineShaderDirectory(gameProjectDir);
        if (engineShaderDir != null)
        {
            var gameShaderDir = Path.Combine(assetsDir, "Shaders");
            ShaderHotReload.Enable(engineShaderDir, gameShaderDir);
        }

        var desc = new GameDesc
        {
            Title = Desc.Title,
            Width = Desc.Width,
            Height = Desc.Height,
            Resizable = true,
            Maximized = true,
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

    private static string? FindEngineShaderDirectory(string startDir)
    {
        var dir = startDir;
        for (var i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(dir, "NiziKit", "Shaders");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            var parent = Path.GetDirectoryName(dir);
            if (parent == null || parent == dir)
            {
                break;
            }

            dir = parent;
        }

        return null;
    }

    private static void LoadAssembliesFromBaseDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var loadedPaths = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => a.Location)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
        {
            if (loadedPaths.Contains(dll))
            {
                continue;
            }

            try
            {
                var name = AssemblyName.GetAssemblyName(dll);
                if (name.Name != null &&
                    !name.Name.StartsWith("System", StringComparison.Ordinal) &&
                    !name.Name.StartsWith("Microsoft", StringComparison.Ordinal))
                {
                    Assembly.LoadFrom(dll);
                }
            }
            catch
            {
                // Ignore assemblies that can't be loaded
            }
        }
    }
}
