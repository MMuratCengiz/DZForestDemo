using Microsoft.Extensions.Logging;
using NiziKit.Core;

namespace NiziKit.Graphics;

public static class ShaderHotReload
{
    private static readonly List<FileSystemWatcher> Watchers = [];
    private static volatile bool _reloadPending;
    private static Timer? _debounceTimer;
    private static readonly Lock TimerLock = new();

    public static bool IsEnabled { get; private set; }
    public static string? EngineShaderDirectory { get; private set; }

    public static event Action? OnShadersReloaded;

    public static void Enable(string engineShaderDir, params string[] additionalWatchDirs)
    {
        if (IsEnabled)
        {
            return;
        }

        EngineShaderDirectory = Path.GetFullPath(engineShaderDir);
        IsEnabled = true;

        var directories = new List<string> { EngineShaderDirectory };
        foreach (var dir in additionalWatchDirs)
        {
            directories.Add(Path.GetFullPath(dir));
        }

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var watcher = new FileSystemWatcher(dir, "*.hlsl")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            Watchers.Add(watcher);
        }
    }

    public static void Disable()
    {
        if (!IsEnabled)
        {
            return;
        }

        foreach (var watcher in Watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        Watchers.Clear();

        lock (TimerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        IsEnabled = false;
        EngineShaderDirectory = null;
    }

    internal static void ProcessPendingReloads()
    {
        if (!_reloadPending)
        {
            return;
        }

        _reloadPending = false;

        var logger = Log.Get("ShaderHotReload");
        logger.LogInformation("Processing shader hot-reload...");

        GraphicsContext.WaitIdle();

        try
        {
            OnShadersReloaded?.Invoke();
            logger.LogInformation("Shaders reloaded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Shader hot-reload failed");
        }
    }

    private static void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        ScheduleReload();
    }

    private static void OnFileRenamed(object? sender, RenamedEventArgs e)
    {
        ScheduleReload();
    }

    private static void ScheduleReload()
    {
        lock (TimerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => _reloadPending = true, null, 300, Timeout.Infinite);
        }
    }
}
