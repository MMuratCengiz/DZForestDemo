using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Themes.Simple;
using DenOfIz;
using NiziKit.Skia.Avalonia;
using AvaloniaApp = global::Avalonia.Application;
using AvaloniaAppBuilder = global::Avalonia.AppBuilder;

namespace NiziKit.Skia;

public sealed class NiziAvalonia
{
    private static bool _initialized;

    private readonly DenOfIzTopLevel _topLevel;

    public object? Content
    {
        get => _topLevel.Content;
        set => _topLevel.Content = value;
    }

    public Texture? Texture => _topLevel.Texture;

    public static AvaloniaAppBuilder BuildAvaloniaApp()
    {
        var buildAvaloniaApp = AvaloniaAppBuilder.Configure<SkiaAvaloniaApp>();
        buildAvaloniaApp.UseDenOfIz();
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            buildAvaloniaApp.UseManagedSystemDialogs();
        }
        return buildAvaloniaApp;
    }

    public NiziAvalonia(Action<AvaloniaAppBuilder>? appFactory = null)
    {
        if (!_initialized)
        {
            _initialized = true;
            var appBuilder = BuildAvaloniaApp();
            appBuilder.UseDenOfIz();
            appFactory?.Invoke(appBuilder);
            appBuilder.SetupWithoutStarting();
        }

        _topLevel = new DenOfIzTopLevel();
    }

    public void Update(float dt)
    {
        _topLevel.Update(dt);
    }

    public void OnEvent(ref Event ev)
    {
        _topLevel.ProcessEvent(ref ev);
    }

    public bool HitTest(double x, double y) => _topLevel.HitTest(x, y);

    private sealed class SkiaAvaloniaApp : AvaloniaApp
    {
        public override void Initialize() => Styles.Add(new SimpleTheme());
        public override void OnFrameworkInitializationCompleted() { }
    }
}
