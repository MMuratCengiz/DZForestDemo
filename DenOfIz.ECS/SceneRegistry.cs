namespace ECS;

public sealed class SceneRegistry<T> : IResource, IDisposable where T : struct, IGameState
{
    private readonly Dictionary<T, IGameScene> _scenes = new();
    private readonly World _world;
    private bool _disposed;
    private bool _initialized;

    public SceneRegistry(World world)
    {
        _world = world;
    }

    public IGameScene? ActiveScene { get; private set; }

    public void Register(T state, IGameScene scene)
    {
        if (_scenes.ContainsKey(state))
        {
            throw new InvalidOperationException($"Scene already registered for state {state}");
        }

        var ecsScene = _world.Scenes.CreateScene(scene.Name);
        scene.OnRegister(_world, ecsScene);
        _scenes[state] = scene;
    }

    public IGameScene? GetScene(T state)
    {
        return _scenes.TryGetValue(state, out var scene) ? scene : null;
    }

    public void Initialize(T initialState)
    {
        if (_initialized)
        {
            return;
        }
        _initialized = true;
        OnStateEnter(initialState);
    }

    public void OnStateEnter(T state)
    {
        if (!_scenes.TryGetValue(state, out var scene))
        {
            return;
        }

        if (ActiveScene != null && ActiveScene != scene)
        {
            ActiveScene.OnExit();
            ActiveScene.Scene.Unload();
        }

        ActiveScene = scene;
        scene.Scene.Load();
        _world.Scenes.SetActiveScene(scene.Scene);
        scene.OnEnter();
    }

    public void OnStateExit(T state)
    {
        if (!_scenes.TryGetValue(state, out var scene))
        {
            return;
        }

        scene.OnExit();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (var scene in _scenes.Values)
        {
            scene.Dispose();
        }
        _scenes.Clear();
    }
}

public static class WorldStateExtensions
{
    public static void InitState<T>(this World world, T initialState) where T : struct, IGameState
    {
        world.RegisterResource(new State<T>(initialState));
        world.RegisterResource(new NextState<T>());
        world.RegisterResource(new StateCallbacks<T>());
    }

    public static void InitStateWithScenes<T>(this World world, T initialState) where T : struct, IGameState
    {
        world.InitState(initialState);
        world.RegisterResource(new SceneRegistry<T>(world));
    }

    public static void AddOnEnter<T>(this World world, T state, OnStateEnter<T> callback) where T : struct, IGameState
    {
        var callbacks = world.GetResource<StateCallbacks<T>>();
        callbacks.AddOnEnter(state, callback);
    }

    public static void AddOnExit<T>(this World world, T state, OnStateExit<T> callback) where T : struct, IGameState
    {
        var callbacks = world.GetResource<StateCallbacks<T>>();
        callbacks.AddOnExit(state, callback);
    }

    public static void RegisterScene<T>(this World world, T state, IGameScene scene) where T : struct, IGameState
    {
        var registry = world.GetResource<SceneRegistry<T>>();
        registry.Register(state, scene);

        world.AddOnEnter(state, (w, s) => registry.OnStateEnter(s));
    }

    public static void InitializeScenes<T>(this World world) where T : struct, IGameState
    {
        var registry = world.GetResource<SceneRegistry<T>>();
        var state = world.GetResource<State<T>>();
        registry.Initialize(state.Current);
    }

    public static void SetNextState<T>(this World world, T state) where T : struct, IGameState
    {
        var nextState = world.GetResource<NextState<T>>();
        nextState.Set(state);
    }

    public static T GetCurrentState<T>(this World world) where T : struct, IGameState
    {
        var state = world.GetResource<State<T>>();
        return state.Current;
    }

    public static bool InState<T>(this World world, T state) where T : struct, IGameState
    {
        var current = world.GetResource<State<T>>();
        return EqualityComparer<T>.Default.Equals(current.Current, state);
    }

    public static IGameScene? GetActiveScene<T>(this World world) where T : struct, IGameState
    {
        var registry = world.TryGetResource<SceneRegistry<T>>();
        return registry?.ActiveScene;
    }
}
