using Avalonia;
using Avalonia.Themes.Simple;
using DenOfIz;
using NiziKit.Skia.Avalonia;
using AvaloniaApp = global::Avalonia.Application;
using AvaloniaAppBuilder = global::Avalonia.AppBuilder;

namespace NiziKit.Skia;

public sealed class SkiaUI
{
    private static bool _initialized;

    private readonly DenOfIzTopLevel _topLevel;

    public object? Content
    {
        get => _topLevel.Content;
        set => _topLevel.Content = value;
    }

    public Texture? Texture => _topLevel.Texture;

    public SkiaUI()
    {
        if (!_initialized)
        {
            _initialized = true;
            AvaloniaAppBuilder.Configure<SkiaAvaloniaApp>()
                .UseDenOfIz()
                .SetupWithoutStarting();
        }

        _topLevel = new DenOfIzTopLevel();
    }

    public void Update(float dt) => _topLevel.Update(dt);

    public void OnEvent(ref Event ev) => _topLevel.ProcessEvent(ref ev);

    public bool HitTest(double x, double y) => _topLevel.HitTest(x, y);

    private sealed class SkiaAvaloniaApp : AvaloniaApp
    {
        public override void Initialize() => Styles.Add(new SimpleTheme());
        public override void OnFrameworkInitializationCompleted() { }
    }
}
