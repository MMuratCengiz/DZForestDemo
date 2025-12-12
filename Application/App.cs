using System.Runtime.CompilerServices;
using Application.Events;
using Application.Timing;
using Application.Windowing;
using DenOfIz;

namespace Application;

/// <summary>
/// Main application class that manages the application lifecycle,
/// subsystems, and the core update loop.
/// </summary>
public sealed class App : IDisposable
{
    private readonly ApplicationOptions _options;
    private readonly EventQueue _eventQueue = new();
    private readonly FixedTimestep _fixedTimestep;

    private readonly List<ISubsystem> _subsystems = new();
    private readonly List<IRenderable> _renderables = new();
    private readonly List<IApplicationEventReceiver> _appEventReceivers = new();

    // Cached arrays for zero-alloc iteration in hot path (built at initialization)
    private ISubsystem[] _subsystemsArray = [];
    private IRenderable[] _renderablesArray = [];
    private IApplicationEventReceiver[] _appEventReceiversArray = [];

    // Graphics infrastructure (optional, only created if subsystems need it)

    private bool _initialized;
    private bool _disposed;

    /// <summary>Gets the application window.</summary>
    public AppWindow Window { get; }

    /// <summary>Gets the frame clock for timing information.</summary>
    public FrameClock Clock { get; } = new();

    /// <summary>Gets the event dispatcher for manual event routing.</summary>
    public EventDispatcher Events { get; } = new();

    /// <summary>Gets the graphics API instance. Only available after Initialize().</summary>
    public GraphicsApi? GraphicsApi { get; private set; }

    /// <summary>Gets the logical device. Only available after Initialize().</summary>
    public LogicalDevice? LogicalDevice { get; private set; }

    /// <summary>Gets the command queue. Only available after Initialize().</summary>
    public CommandQueue? CommandQueue { get; private set; }

    /// <summary>Gets the swap chain. Only available after Initialize().</summary>
    public SwapChain? SwapChain { get; private set; }

    /// <summary>Gets the current frame index for multi-buffered resources.</summary>
    public uint FrameIndex { get; private set; }

    /// <summary>Gets the number of back buffers.</summary>
    public uint NumFrames => _options.NumFrames;

    /// <summary>Gets the back buffer format.</summary>
    public Format BackBufferFormat => _options.BackBufferFormat;

    /// <summary>Gets the depth buffer format.</summary>
    public Format DepthBufferFormat => _options.DepthBufferFormat;

    /// <summary>Gets whether the application is currently running.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Creates a new application with default options.
    /// </summary>
    public App() : this(new ApplicationOptions())
    {
    }

    /// <summary>
    /// Creates a new application with the specified options.
    /// </summary>
    public App(ApplicationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        Window = new AppWindow(options.Title, options.Width, options.Height);
        _fixedTimestep = new FixedTimestep(options.FixedUpdateRate);
    }

    /// <summary>
    /// Registers a subsystem with the application.
    /// Must be called before Run().
    /// </summary>
    /// <typeparam name="T">Subsystem type.</typeparam>
    /// <param name="subsystem">The subsystem instance.</param>
    /// <param name="eventPriority">Event priority if subsystem implements IEventReceiver.</param>
    /// <returns>The registered subsystem for chaining.</returns>
    public T AddSubsystem<T>(T subsystem, int eventPriority = 0) where T : ISubsystem
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Cannot add subsystems after initialization.");
        }

        _subsystems.Add(subsystem);

        if (subsystem is IEventReceiver receiver)
        {
            Events.Register(receiver, eventPriority);
        }

        if (subsystem is IRenderable renderable)
        {
            _renderables.Add(renderable);
        }

        if (subsystem is IApplicationEventReceiver appReceiver)
        {
            _appEventReceivers.Add(appReceiver);
        }

        return subsystem;
    }

    /// <summary>
    /// Gets a registered subsystem by type.
    /// </summary>
    /// <typeparam name="T">Subsystem type to find.</typeparam>
    /// <returns>The subsystem instance, or null if not found.</returns>
    public T? GetSubsystem<T>() where T : class, ISubsystem
    {
        foreach (var subsystem in _subsystems)
        {
            if (subsystem is T typed)
            {
                return typed;
            }
        }
        return null;
    }

    /// <summary>
    /// Initializes graphics infrastructure.
    /// Call this before Run() if your subsystems need graphics access during Initialize().
    /// If not called explicitly, it will be called automatically in Run().
    /// </summary>
    public void InitializeGraphics()
    {
        if (GraphicsApi != null)
        {
            return;
        }

        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());

        GraphicsApi = new GraphicsApi(_options.ApiPreference);
        LogicalDevice = GraphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc());

        CommandQueue = LogicalDevice.CreateCommandQueue(new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        });

        SwapChain = LogicalDevice.CreateSwapChain(new SwapChainDesc
        {
            AllowTearing = _options.AllowTearing,
            BackBufferFormat = _options.BackBufferFormat,
            DepthBufferFormat = _options.DepthBufferFormat,
            CommandQueue = CommandQueue,
            WindowHandle = Window.GraphicsHandle,
            Width = Window.Width,
            Height = Window.Height,
            NumBuffers = _options.NumFrames
        });
    }

    /// <summary>
    /// Runs the application main loop. Blocks until the application exits.
    /// </summary>
    public void Run()
    {
        InitializeGraphics();

        Window.Show();

        // Build cached arrays for zero-alloc hot path iteration
        _subsystemsArray = _subsystems.ToArray();
        _renderablesArray = _renderables.ToArray();
        _appEventReceiversArray = _appEventReceivers.ToArray();

        // Initialize all subsystems
        foreach (var subsystem in _subsystems)
        {
            subsystem.Initialize();
        }

        _initialized = true;
        IsRunning = true;
        Clock.Start();

        while (IsRunning)
        {
            RunFrame();
        }

        Shutdown();
    }

    /// <summary>
    /// Requests the application to quit.
    /// </summary>
    public void Quit()
    {
        IsRunning = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Clock.Tick();

        // Poll and process events
        _eventQueue.Poll();
        ProcessEvents();

        if (!IsRunning)
        {
            return;
        }

        // Skip update/render if minimized
        if (Window.IsMinimized)
        {
            return;
        }

        double deltaTime = Clock.DeltaTime;

        // Fixed updates
        int fixedSteps = _fixedTimestep.Accumulate(deltaTime);
        double fixedDelta = _fixedTimestep.FixedDeltaTime;
        ReadOnlySpan<ISubsystem> subsystems = _subsystemsArray;
        for (int i = 0; i < fixedSteps; i++)
        {
            for (int j = 0; j < subsystems.Length; j++)
            {
                subsystems[j].FixedUpdate(fixedDelta);
            }
        }

        // Variable update
        for (int i = 0; i < subsystems.Length; i++)
        {
            subsystems[i].Update(deltaTime);
        }

        // Late update
        for (int i = 0; i < subsystems.Length; i++)
        {
            subsystems[i].LateUpdate(deltaTime);
        }

        // Render
        if (_renderablesArray.Length > 0)
        {
            RenderFrame();
        }
    }

    private void ProcessEvents()
    {
        foreach (ref var ev in _eventQueue)
        {
            // Handle quit event
            if (ev.Type == EventType.Quit)
            {
                IsRunning = false;
                NotifyApplicationEvent(new ApplicationEvent(ApplicationEventType.Quit));
                return;
            }

            // Handle window events
            if (ev.Type == EventType.WindowEvent)
            {
                HandleWindowEvent(ref ev);
            }

            // Dispatch to receivers
            Events.Dispatch(ref ev);
        }
    }

    private void HandleWindowEvent(ref Event ev)
    {
        Window.HandleWindowEvent(ev.Window.Event, ev.Window.Data1, ev.Window.Data2);

        switch (ev.Window.Event)
        {
            case WindowEventType.Resized:
                HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
                NotifyApplicationEvent(new ApplicationEvent(
                    ApplicationEventType.Resized,
                    (uint)ev.Window.Data1,
                    (uint)ev.Window.Data2));
                break;

            case WindowEventType.Minimized:
                NotifyApplicationEvent(new ApplicationEvent(ApplicationEventType.Minimized));
                break;

            case WindowEventType.Restored:
                NotifyApplicationEvent(new ApplicationEvent(ApplicationEventType.Restored));
                break;

            case WindowEventType.FocusGained:
                NotifyApplicationEvent(new ApplicationEvent(ApplicationEventType.FocusGained));
                break;

            case WindowEventType.FocusLost:
                NotifyApplicationEvent(new ApplicationEvent(ApplicationEventType.FocusLost));
                break;
        }
    }

    private void HandleResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        LogicalDevice?.WaitIdle();
        CommandQueue?.WaitIdle();
        SwapChain?.Resize(width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotifyApplicationEvent(in ApplicationEvent ev)
    {
        ReadOnlySpan<IApplicationEventReceiver> receivers = _appEventReceiversArray;
        for (int i = 0; i < receivers.Length; i++)
        {
            receivers[i].OnApplicationEvent(in ev);
        }
    }

    private void RenderFrame()
    {
        FrameIndex = (FrameIndex + 1) % _options.NumFrames;

        var context = new RenderContext
        {
            FrameIndex = FrameIndex,
            Width = Window.Width,
            Height = Window.Height,
            DeltaTime = Clock.DeltaTime,
            TotalTime = Clock.TotalTime
        };

        ReadOnlySpan<IRenderable> renderables = _renderablesArray;
        for (int i = 0; i < renderables.Length; i++)
        {
            renderables[i].Render(ref context);
        }
    }

    private void Shutdown()
    {
        LogicalDevice?.WaitIdle();
        CommandQueue?.WaitIdle();

        // Shutdown subsystems in reverse order
        for (int i = _subsystems.Count - 1; i >= 0; i--)
        {
            _subsystems[i].Shutdown();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        LogicalDevice?.WaitIdle();
        CommandQueue?.WaitIdle();

        // Dispose subsystems in reverse order
        for (int i = _subsystems.Count - 1; i >= 0; i--)
        {
            _subsystems[i].Dispose();
        }

        SwapChain?.Dispose();
        CommandQueue?.Dispose();
        LogicalDevice?.Dispose();

        if (GraphicsApi != null)
        {
            GraphicsApi.ReportLiveObjects();
        }

        Window.Dispose();

        Engine.Shutdown();

        GC.SuppressFinalize(this);
    }
}
