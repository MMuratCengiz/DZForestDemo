using System.Runtime.CompilerServices;
using Application.Events;
using Application.Timing;
using Application.Windowing;
using DenOfIz;
using Flecs.NET.Core;
using Graphics;
using Physics;
using RuntimeAssets;

namespace Application;

public sealed class App(ApplicationOptions options) : IDisposable
{
    private readonly EventQueue _eventQueue = new();
    private readonly FixedTimestep _fixedTimestep = new(options.FixedUpdateRate);
    private readonly ApplicationOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private bool _disposed;

    private bool _initialized;

    public App() : this(new ApplicationOptions())
    {
    }

    public AppWindow Window { get; } = new(options.Title, options.Width, options.Height);
    public FrameClock Clock { get; } = new();
    public World World { get; } = World.Create();

    public GraphicsDesc Graphics => _options.Graphics;

    private bool IsRunning { get; set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        GraphicsSystems.WaitIdle(World);
        World.Dispose();
        Window.Dispose();
        Engine.Shutdown();

        GC.SuppressFinalize(this);
    }


    public void Run()
    {
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());

        Window.Show();

        var timeResource = new TimeResource(Clock);
        World.Set(timeResource);
        World.Set<ITimeResource>(timeResource);

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

        var fixedSteps = _fixedTimestep.Accumulate(Clock.DeltaTime);
        if (World.Has<PhysicsResource>())
        {
            ref var physics = ref World.GetMut<PhysicsResource>();
            physics.AccumulatedSteps = fixedSteps;
        }

        World.Progress((float)Clock.DeltaTime);
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

                if (ev.Window.Event == WindowEventType.Resized)
                {
                    GraphicsSystems.HandleResize(World, (uint)ev.Window.Data1, (uint)ev.Window.Data2);
                }
            }

            if (World.Has<EventHandlers>())
            {
                World.Get<EventHandlers>().Dispatch(ref ev);
            }
        }
    }

    private void Shutdown()
    {
        GraphicsSystems.WaitIdle(World);
    }
}

/// <summary>
/// Event handler registry for dispatching input events.
/// </summary>
public class EventHandlers
{
    private readonly List<EventHandler> _handlers = [];

    public delegate bool EventHandler(ref Event ev);

    public void Register(EventHandler handler) => _handlers.Add(handler);
    public void Unregister(EventHandler handler) => _handlers.Remove(handler);

    public void Dispatch(ref Event ev)
    {
        foreach (var handler in _handlers)
        {
            if (handler(ref ev))
            {
                break; // Event was consumed
            }
        }
    }
}
