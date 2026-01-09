using System.Runtime.CompilerServices;
using DenOfIz.World.Application.Timing;
using DenOfIz.World.Application.Windowing;
using DenOfIz.World.Graphics;
using DenOfIz.World.Graphics.Renderer;
using DenOfIz.World.SceneManagement;

namespace DenOfIz.World.Application;

public class Game : IDisposable
{
    private readonly FixedTimestep _fixedTimestep;
    private readonly IRenderer? _renderer = null;
    private readonly SceneManagement.World _world;
    private bool _disposed;
    
    public AppWindow Window { get; }
    public FrameClock Clock { get; }
    public GraphicsContext Graphics { get; }
    public SceneManagement.World World => _world;
    
    public bool IsRunning { get; set; }
    
    static Game()
    {
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());
    }

    public Game(GameDesc? desc = null)
    {
        desc ??= new GameDesc();
        _fixedTimestep = new FixedTimestep(desc.FixedUpdateRate);
        Window = new AppWindow(desc.Title, desc.Width, desc.Height);
        Clock = new FrameClock();
        Graphics = new GraphicsContext(Window.NativeWindow, desc.Graphics);
        _world = new SceneManagement.World(Graphics.LogicalDevice);
    }


    public void LoadScene(Scene scene)
    {
        World.LoadScene(scene);
    }

    
    protected virtual void Load(Game game) { }
    protected virtual void FixedUpdate(float fixedDt) { }
    protected virtual void Update(float dt) { }
    protected virtual void OnEvent(ref Event ev) { }
    protected virtual void OnShutdown() { }
    
    public void Run()
    {
        Window.Show();
        Load(this);

        IsRunning = true;
        Clock.Start();

        while (IsRunning)
        {
            RunFrame();
        }

        OnShutdown();
    }

    public void Quit()
    {
        IsRunning = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _renderer?.Dispose();
        Graphics.Dispose();
        Window.Dispose();
        Engine.Shutdown();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Clock.Tick();
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
        for (var i = 0; i < fixedSteps; i++)
        {
            FixedUpdate((float)_fixedTimestep.FixedDeltaTime);
        }

        Update((float)Clock.DeltaTime);
    }

    private void ProcessEvents()
    {
        while (InputSystem.PollEvent(out var ev))
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
                    Graphics.Resize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
                    _renderer.OnResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
                }
            }

            OnEvent(ref ev);
        }
    }
}