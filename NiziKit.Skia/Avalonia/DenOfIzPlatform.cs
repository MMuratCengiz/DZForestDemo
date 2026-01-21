using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Initializes the Avalonia platform for DenOfIz integration.
/// Registers all required services for Avalonia to render to DenOfIz textures.
/// </summary>
public static class DenOfIzPlatform
{
    private static bool _initialized;
    private static DenOfIzPlatformGraphics? _platformGraphics;
    private static DenOfIzRenderTimer? _renderTimer;
    private static Compositor? _compositor;

    /// <summary>
    /// Gets the platform graphics instance for creating render surfaces.
    /// </summary>
    public static DenOfIzPlatformGraphics PlatformGraphics =>
        _platformGraphics ?? throw new InvalidOperationException("DenOfIz platform not initialized");

    /// <summary>
    /// Gets the compositor instance.
    /// </summary>
    public static Compositor Compositor =>
        _compositor ?? throw new InvalidOperationException("DenOfIz platform not initialized");

    /// <summary>
    /// Initializes the DenOfIz platform for Avalonia.
    /// This should be called BEFORE AppBuilder.Configure().
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        // Create render timer
        _renderTimer = new DenOfIzRenderTimer();

        // Create platform graphics
        _platformGraphics = new DenOfIzPlatformGraphics();
    }

    /// <summary>
    /// Called by UseWindowingSubsystem to complete platform initialization.
    /// At this point, headless platform has registered the dispatcher.
    /// </summary>
    internal static void CompleteInitialization()
    {
        var locator = AvaloniaLocator.CurrentMutable;

        // Override specific services for our GPU rendering
        locator.Bind<IRenderTimer>().ToConstant(_renderTimer!);
        locator.Bind<IPlatformGraphics>().ToConstant(_platformGraphics!);

        // Register our input services
        locator.Bind<ICursorFactory>().ToConstant(new DenOfIzCursorFactory());
        locator.Bind<IClipboard>().ToConstant(new DenOfIzClipboard());
        locator.Bind<IPlatformSettings>().ToConstant(new DenOfIzPlatformSettings());

        // Create compositor after all services are registered
        // The headless platform has already set up the dispatcher properly
        _compositor = new Compositor(null);
    }

    /// <summary>
    /// Triggers a render tick. Call this from your game loop.
    /// </summary>
    public static void TriggerRenderTick(TimeSpan elapsed)
    {
        // Run dispatcher jobs
        Dispatcher.UIThread.RunJobs();
        _renderTimer?.TriggerTick(elapsed);
    }
}

/// <summary>
/// Render timer that integrates with the game loop.
/// </summary>
internal sealed class DenOfIzRenderTimer : IRenderTimer
{
    public event Action<TimeSpan>? Tick;

    public bool RunsInBackground => false;

    internal void TriggerTick(TimeSpan elapsed)
    {
        Tick?.Invoke(elapsed);
    }
}

/// <summary>
/// Stub cursor factory - cursors are handled by DenOfIz/game engine.
/// </summary>
internal sealed class DenOfIzCursorFactory : ICursorFactory
{
    public ICursorImpl GetCursor(StandardCursorType cursorType)
        => new DenOfIzCursor(cursorType);

    public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        => new DenOfIzCursor(StandardCursorType.Arrow);
}

internal sealed class DenOfIzCursor : ICursorImpl
{
    public StandardCursorType CursorType { get; }

    public DenOfIzCursor(StandardCursorType cursorType)
    {
        CursorType = cursorType;
    }

    public void Dispose() { }
}

/// <summary>
/// Stub clipboard - can be implemented to integrate with OS clipboard.
/// </summary>
internal sealed class DenOfIzClipboard : IClipboard
{
    private string? _text;
    private IDataObject? _data;

    public Task ClearAsync()
    {
        _text = null;
        _data = null;
        return Task.CompletedTask;
    }

    public Task<object?> GetDataAsync(string format)
        => Task.FromResult<object?>(_data);

    public Task<string?> GetTextAsync()
        => Task.FromResult(_text);

    public Task SetDataObjectAsync(IDataObject data)
    {
        _data = data;
        return Task.CompletedTask;
    }

    public Task SetTextAsync(string? text)
    {
        _text = text;
        return Task.CompletedTask;
    }

    public Task<string[]> GetFormatsAsync()
        => Task.FromResult(Array.Empty<string>());
}

/// <summary>
/// Platform settings for DenOfIz.
/// </summary>
internal sealed class DenOfIzPlatformSettings : IPlatformSettings
{
    public event EventHandler<PlatformColorValues>? ColorValuesChanged;

    public Size GetTapSize(PointerType type) => new(10, 10);
    public Size GetDoubleTapSize(PointerType type) => new(10, 10);
    public TimeSpan GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(500);
    public TimeSpan HoldWaitDuration => TimeSpan.FromMilliseconds(500);

    public PlatformHotkeyConfiguration HotkeyConfiguration { get; } =
        OperatingSystem.IsMacOS()
            ? new PlatformHotkeyConfiguration(KeyModifiers.Meta)
            : new PlatformHotkeyConfiguration(KeyModifiers.Control);

    public PlatformColorValues GetColorValues() => new()
    {
        ThemeVariant = PlatformThemeVariant.Dark
    };
}
