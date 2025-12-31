using System.Runtime.CompilerServices;
using Application.Timing;
using Application.Windowing;
using DenOfIz;
using DenOfIz.World;
using Graphics;

namespace Application;

public sealed class Game : IDisposable
{
    private readonly FixedTimestep _fixedTimestep;
    private readonly IGame _game;
    private bool _disposed;
    private Scene? _activeScene;
    private Scene? _pendingScene;

    static Game()
    {
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());
    }

    public Game(IGame game, IRenderer? renderer = null, GameDesc? desc = null)
    {
        desc ??= new GameDesc();
        _game = game;
        _fixedTimestep = new FixedTimestep(desc.FixedUpdateRate);
        Window = new AppWindow(desc.Title, desc.Width, desc.Height);
        Clock = new FrameClock();
        Graphics = new GraphicsContext(Window.NativeWindow, desc.Graphics, renderer);
    }

    public AppWindow Window { get; }
    public FrameClock Clock { get; }
    public GraphicsContext Graphics { get; }
    public Scene? ActiveScene => _activeScene;
    private bool IsRunning { get; set; }

    public void LoadScene(Scene scene)
    {
        _pendingScene = scene;
    }

    public void Run()
    {
        Window.Show();
        _game.OnLoad(this);

        IsRunning = true;
        Clock.Start();

        while (IsRunning)
        {
            RunFrame();
        }

        _game.OnShutdown();
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
        _activeScene?.OnUnload?.Invoke();
        Graphics.Dispose();
        Window.Dispose();
        Engine.Shutdown();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Clock.Tick();

        if (_pendingScene != null)
        {
            _activeScene?.OnUnload?.Invoke();
            _activeScene = _pendingScene;
            _pendingScene = null;
            _activeScene?.OnLoad?.Invoke();
        }

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
            _game.OnFixedUpdate((float)_fixedTimestep.FixedDeltaTime);
        }

        _game.OnUpdate((float)Clock.DeltaTime);

        Graphics.BeginFrame();
        Graphics.Render();
        _game.OnRender();
        Graphics.EndFrame();
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
                }
            }

            _game.OnEvent(ref ev);
        }
    }
}
