using System.Threading;
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
    /// Phase 1: Initialize core resources before UseSkia.
    /// </summary>
    internal static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        // Disable Avalonia's sync context - we manage our own game loop
        AvaloniaSynchronizationContext.AutoInstall = false;

        // Create render timer
        _renderTimer = new DenOfIzRenderTimer();

        // Create platform graphics
        _platformGraphics = new DenOfIzPlatformGraphics();
    }

    /// <summary>
    /// Called by UseWindowingSubsystem to register platform services.
    /// </summary>
    internal static void InitializeWindowing()
    {
        var locator = AvaloniaLocator.CurrentMutable;

        // Register dispatcher - this is critical and must happen first
        locator.Bind<IDispatcherImpl>().ToConstant(new DenOfIzDispatcherImpl(Thread.CurrentThread));

        // Register render timer
        locator.Bind<IRenderTimer>().ToConstant(_renderTimer!);

        // Register platform graphics
        locator.Bind<IPlatformGraphics>().ToConstant(_platformGraphics!);

        // Register windowing platform (stub - we don't create windows)
        locator.Bind<IWindowingPlatform>().ToConstant(new DenOfIzWindowingPlatform());

        // Register input services
        locator.Bind<ICursorFactory>().ToConstant(new DenOfIzCursorFactory());
        locator.Bind<IClipboard>().ToConstant(new DenOfIzClipboard());
        locator.Bind<IPlatformSettings>().ToConstant(new DenOfIzPlatformSettings());
        locator.Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice());

        // Platform hotkey configuration
        locator.Bind<PlatformHotkeyConfiguration>().ToConstant(
            OperatingSystem.IsMacOS()
                ? new PlatformHotkeyConfiguration(KeyModifiers.Meta)
                : new PlatformHotkeyConfiguration(KeyModifiers.Control));

        // Create compositor with our platform graphics
        _compositor = new Compositor(_platformGraphics);
    }

    /// <summary>
    /// Triggers a render tick. Call this from your game loop.
    /// </summary>
    public static void TriggerRenderTick(TimeSpan elapsed)
    {
        // Run dispatcher jobs first
        Dispatcher.UIThread.RunJobs();

        // Then trigger render timer
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

/// <summary>
/// Dispatcher implementation for DenOfIz - runs on the game's main thread.
/// </summary>
internal sealed class DenOfIzDispatcherImpl : IDispatcherImpl
{
    private readonly Thread _mainThread;

    public DenOfIzDispatcherImpl(Thread mainThread)
    {
        _mainThread = mainThread;
    }

    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _mainThread;

#pragma warning disable CS0067 // Events are required by interface but not used
    public event Action? Signaled;
    public event Action? Timer;
    public event Action<DispatcherPriority?>? ReadyForBackgroundProcessing;
#pragma warning restore CS0067

    public void Signal()
    {
        // Signal is used to wake up the dispatcher - not needed since we pump manually
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
        // Timer is handled by game loop via TriggerRenderTick
    }

    public long Now => Environment.TickCount64;
}

/// <summary>
/// Stub windowing platform - we don't create native windows.
/// </summary>
internal sealed class DenOfIzWindowingPlatform : IWindowingPlatform
{
    public IWindowImpl CreateWindow()
        => throw new NotSupportedException("DenOfIz platform doesn't support creating windows. Use DenOfIzTopLevel instead.");

    public IWindowImpl CreateEmbeddableWindow()
        => throw new NotSupportedException("DenOfIz platform doesn't support creating windows. Use DenOfIzTopLevel instead.");

    public ITopLevelImpl CreateEmbeddableTopLevel()
        => throw new NotSupportedException("Use DenOfIzTopLevel constructor directly.");

    public ITrayIconImpl? CreateTrayIcon()
        => null;
}
