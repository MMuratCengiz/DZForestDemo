using System.Runtime.CompilerServices;
using Application.Events;
using Application.Timing;
using Application.Windowing;
using DenOfIz;
using ECS;

namespace Application;

public sealed class App(ApplicationOptions options) : IDisposable
{
    private readonly ApplicationOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly EventQueue _eventQueue = new();
    private readonly FixedTimestep _fixedTimestep = new(options.FixedUpdateRate);

    private bool _initialized;
    private bool _disposed;

    public AppWindow Window { get; } = new(options.Title, options.Width, options.Height);
    public FrameClock Clock { get; } = new();
    public World World { get; } = new();

    public uint NumFrames => _options.NumFrames;
    public Format BackBufferFormat => _options.BackBufferFormat;
    public Format DepthBufferFormat => _options.DepthBufferFormat;
    public APIPreference ApiPreference => _options.ApiPreference;
    public bool AllowTearing => _options.AllowTearing;

    private bool IsRunning { get; set; }

    public App() : this(new ApplicationOptions())
    {
    }

    public SystemDescriptor AddSystem<T>(T system, Schedule schedule) where T : ISystem
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Cannot add systems after initialization.");
        }

        return World.AddSystem(system, schedule);
    }

    public T? GetSystem<T>() where T : class, ISystem
    {
        return World.GetSystem<T>();
    }

    public void Run()
    {
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());

        Window.Show();

        World.Initialize();
        _initialized = true;
        IsRunning = true;
        Clock.Start();

        while (IsRunning)
        {
            RunFrame();
        }

        Shutdown();
    }

    public void Quit()
    {
        IsRunning = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Clock.Tick();

        _eventQueue.Poll();
        ProcessEvents();

        if (!IsRunning)
        {
            return;
        }

        if (Window.IsMinimized)
        {
            return;
        }

        World.RunSchedule(Schedule.First);
        World.RunSchedule(Schedule.PreUpdate);

        var fixedSteps = _fixedTimestep.Accumulate(Clock.DeltaTime);
        for (var i = 0; i < fixedSteps; i++)
        {
            World.RunSchedule(Schedule.FixedUpdate);
        }

        World.RunSchedule(Schedule.Update);
        World.RunSchedule(Schedule.PostUpdate);
        World.RunSchedule(Schedule.Last);

        World.RunSchedule(Schedule.PrepareFrame);
        World.RunSchedule(Schedule.Render);
        World.RunSchedule(Schedule.PostRender);
    }

    private void ProcessEvents()
    {
        foreach (ref var ev in _eventQueue)
        {
            if (ev.Type == EventType.Quit)
            {
                IsRunning = false;
                return;
            }

            if (ev.Type == EventType.WindowEvent)
            {
                Window.HandleWindowEvent(ev.Window.Event, ev.Window.Data1, ev.Window.Data2);
            }

            World.OnEvent(ref ev);
        }
    }

    private void Shutdown()
    {
        World.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        World.Dispose();
        Window.Dispose();
        Engine.Shutdown();

        GC.SuppressFinalize(this);
    }
}
