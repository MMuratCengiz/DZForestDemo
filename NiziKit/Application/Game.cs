using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Application.Windowing;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.Services;

namespace NiziKit.Application;

public class Game : IDisposable
{
    public virtual Type RendererType => typeof(ForwardRenderer);
    private static Game? _instance;
    public static Game Instance => _instance ?? throw new InvalidOperationException("Game not initialized");

    private readonly FixedTimestep _fixedTimestep;
    private readonly GameComposition _composition;

    public AppWindow Window { get; }
    public bool IsRunning { get; set; }

    public static void Run<TGame>(GameDesc? desc = null) where TGame : Game
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("A game is already running. Only one game instance is allowed per process.");
        }

        Log.Initialize();
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());
        using var game = (TGame)Activator.CreateInstance(typeof(TGame), desc)!;
        game.Run();
    }

    protected Game(GameDesc? desc = null)
    {
        desc ??= new GameDesc();
        _fixedTimestep = new FixedTimestep(desc.FixedUpdateRate);
        Window = new AppWindow(desc.Title, desc.Width, desc.Height, desc.Resizable);

        _composition = new GameComposition(Window.NativeWindow, desc.Graphics);
        _ = _composition.Time;
        _ = _composition.Input;
        _ = _composition.Graphics;
        _ = _composition.Assets;
        _ = _composition.World;

        _instance = this;
    }


    protected virtual void Load(Game game) { }
    protected virtual void FixedUpdate(float fixedDt) { }
    protected virtual void Update(float dt) { }
    protected virtual void OnEvent(ref Event ev) { }
    protected virtual void OnShutdown() { }

    private void Run()
    {
        Window.Show();
        Load(this);

        IsRunning = true;
        Time.Start();

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
        _instance = null;
        _composition.Dispose();
        Window.Dispose();
        Engine.Shutdown();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RunFrame()
    {
        Time.Tick();
        ProcessEvents();

        if (!IsRunning)
        {
            return;
        }

        if (Window.IsMinimized)
        {
            return;
        }

        World.CurrentScene?.ProcessGameObjectLifecycle();

        var fixedSteps = _fixedTimestep.Accumulate(Time.UnscaledDeltaTime);
        for (var i = 0; i < fixedSteps; i++)
        {
            World.PhysicsWorld?.Step((float)_fixedTimestep.FixedDeltaTime);
            World.CurrentScene?.PhysicsUpdateGameObjects();
            FixedUpdate((float)_fixedTimestep.FixedDeltaTime);
        }

        World.CurrentScene?.UpdateGameObjects();
        World.AnimationWorld.Update(Time.DeltaTime);
        Update(Time.DeltaTime);

        World.CurrentScene?.UpdateCameras(Time.DeltaTime);
        World.CurrentScene?.PostUpdateGameObjects();
    }

    private void ProcessEvents()
    {
        Input.Update();

        while (InputSystem.PollEvent(out var ev))
        {
            Input.ProcessEvent(ref ev);
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
                    var width = (uint)ev.Window.Data1;
                    var height = (uint)ev.Window.Data2;
                    GraphicsContext.Resize(width, height);
                    World.CurrentScene?.OnCameraResize(width, height);
                }
            }

            World.CurrentScene?.HandleCameraEvent(in ev);
            OnEvent(ref ev);
        }
    }
}
