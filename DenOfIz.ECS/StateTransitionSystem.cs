using DenOfIz;

namespace ECS;

public sealed class StateTransitionSystem<T> : ISystem where T : struct, IGameState
{
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
    }

    public void Run()
    {
        var nextState = _world.TryGetResource<NextState<T>>();
        if (nextState == null || !nextState.HasPending)
        {
            return;
        }

        var pending = nextState.Take();
        if (!pending.HasValue)
        {
            return;
        }

        var state = _world.TryGetResource<State<T>>();
        if (state == null)
        {
            return;
        }

        var callbacks = _world.TryGetResource<StateCallbacks<T>>();
        var oldState = state.Current;
        var newState = pending.Value;

        if (EqualityComparer<T>.Default.Equals(oldState, newState))
        {
            return;
        }

        callbacks?.InvokeOnExit(_world, oldState);

        state.Current = newState;

        callbacks?.InvokeOnEnter(_world, newState);
    }

    public bool OnEvent(ref Event ev) => false;

    public void Shutdown() { }

    public void Dispose() { }
}

public sealed class AssetLoadTrackerSystem : ISystem
{
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
    }

    public void Run()
    {
        var tracker = _world.TryGetResource<AssetLoadTracker>();
        tracker?.Update();
    }

    public bool OnEvent(ref Event ev) => false;

    public void Shutdown() { }

    public void Dispose() { }
}
