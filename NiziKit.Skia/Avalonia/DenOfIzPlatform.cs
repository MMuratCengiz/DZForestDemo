using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace NiziKit.Skia.Avalonia;

public static class DenOfIzPlatform
{
    private static bool _initialized;
    private static DenOfIzPlatformGraphics? _platformGraphics;
    private static DenOfIzRenderTimer? _renderTimer;
    private static Compositor? _compositor;

    public static DenOfIzPlatformGraphics PlatformGraphics =>
        _platformGraphics ?? throw new InvalidOperationException("DenOfIz platform not initialized");

    public static Compositor Compositor =>
        _compositor ?? throw new InvalidOperationException("DenOfIz platform not initialized");

    internal static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // Disable Avalonia's sync context - we manage our own game loop
        AvaloniaSynchronizationContext.AutoInstall = false;

        _renderTimer = new DenOfIzRenderTimer();
        _platformGraphics = new DenOfIzPlatformGraphics();
    }

    internal static void InitializeWindowing()
    {
        var locator = AvaloniaLocator.CurrentMutable;

        locator.Bind<IDispatcherImpl>().ToConstant(new DenOfIzDispatcherImpl(Thread.CurrentThread));
        locator.Bind<IRenderTimer>().ToConstant(_renderTimer!);
        locator.Bind<IPlatformGraphics>().ToConstant(_platformGraphics!);
        locator.Bind<IWindowingPlatform>().ToConstant(new DenOfIzWindowingPlatform());
        locator.Bind<ICursorFactory>().ToConstant(new DenOfIzCursorFactory());
        locator.Bind<IClipboard>().ToConstant(new DenOfIzClipboard());
        locator.Bind<IPlatformSettings>().ToConstant(new DenOfIzPlatformSettings());
        locator.Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice());

        locator.Bind<PlatformHotkeyConfiguration>().ToConstant(
            OperatingSystem.IsMacOS()
                ? new PlatformHotkeyConfiguration(KeyModifiers.Meta)
                : new PlatformHotkeyConfiguration(KeyModifiers.Control));

        _compositor = new Compositor(_platformGraphics);
    }

    public static void TriggerRenderTick(TimeSpan elapsed)
    {
        Dispatcher.UIThread.RunJobs();
        _renderTimer?.TriggerTick(elapsed);
    }
}

internal sealed class DenOfIzRenderTimer : IRenderTimer
{
    public event Action<TimeSpan>? Tick;

    public bool RunsInBackground => false;

    internal void TriggerTick(TimeSpan elapsed)
    {
        Tick?.Invoke(elapsed);
    }
}

internal sealed class DenOfIzCursorFactory : ICursorFactory
{
    public ICursorImpl GetCursor(StandardCursorType cursorType)
        => new DenOfIzCursor(cursorType);

    public ICursorImpl CreateCursor(IBitmapImpl cursor, PixelPoint hotSpot)
        => new DenOfIzCursor(StandardCursorType.Arrow);
}

internal sealed class DenOfIzCursor(StandardCursorType cursorType) : ICursorImpl
{
    public StandardCursorType CursorType { get; } = cursorType;

    public void Dispose() { }
}

internal sealed class DenOfIzClipboard : IClipboard
{
    private string? _text;
#pragma warning disable CS0618 // IDataObject is obsolete
    private IDataObject? _data;
#pragma warning restore CS0618

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

#pragma warning disable CS0618 // IDataObject is obsolete
    public Task SetDataObjectAsync(IDataObject data)
    {
        _data = data;
        return Task.CompletedTask;
    }
#pragma warning restore CS0618

    public Task SetTextAsync(string? text)
    {
        _text = text;
        return Task.CompletedTask;
    }

    public Task<string[]> GetFormatsAsync()
        => Task.FromResult(Array.Empty<string>());

    public Task SetDataAsync(IAsyncDataTransfer? data)
        => Task.CompletedTask;

    public Task FlushAsync()
        => Task.CompletedTask;

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
        => Task.FromResult<IAsyncDataTransfer?>(null);

#pragma warning disable CS0618 // IDataObject is obsolete
    public Task<IDataObject?> TryGetInProcessDataObjectAsync()
        => Task.FromResult<IDataObject?>(null);

    public Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
        => Task.FromResult<IAsyncDataTransfer?>(null);
#pragma warning restore CS0618

    public Task<(bool Success, object? Value)> TryGetInProcessDataAsync(string format)
        => Task.FromResult<(bool Success, object? Value)>((false, null));
}

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

internal sealed class DenOfIzDispatcherImpl(Thread mainThread) : IDispatcherImpl
{
    public bool CurrentThreadIsLoopThread => Thread.CurrentThread == mainThread;

#pragma warning disable CS0067 // Events are required by interface but not used
    public event Action? Signaled;
    public event Action? Timer;
    public event Action<DispatcherPriority?>? ReadyForBackgroundProcessing;
#pragma warning restore CS0067

    public void Signal()
    {
    }

    public void UpdateTimer(long? dueTimeInMs)
    {
    }

    public long Now => Environment.TickCount64;
}

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
