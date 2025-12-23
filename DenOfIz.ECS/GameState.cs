using Flecs.NET.Core;

namespace ECS;

/// <summary>
/// Marker interface for game state types.
/// </summary>
public interface IGameState;

/// <summary>
/// Holds the current game state.
/// </summary>
public class State<T> where T : struct, IGameState
{
    public T Current { get; internal set; }
    public State(T initial) => Current = initial;
}

/// <summary>
/// Holds pending state transition.
/// </summary>
public class NextState<T> where T : struct, IGameState
{
    private T? _next;

    public bool HasPending => _next.HasValue;
    public T? Pending => _next;

    public void Set(T state) => _next = state;

    internal T? Take()
    {
        var result = _next;
        _next = null;
        return result;
    }

    internal void Clear() => _next = null;
}

public readonly struct StateTransitionEvent<T> where T : struct, IGameState
{
    public T From { get; init; }
    public T To { get; init; }
}

public delegate void OnStateEnter<T>(World world, T state) where T : struct, IGameState;
public delegate void OnStateExit<T>(World world, T state) where T : struct, IGameState;

/// <summary>
/// Holds state transition callbacks.
/// </summary>
public class StateCallbacks<T> where T : struct, IGameState
{
    private readonly Dictionary<T, List<OnStateEnter<T>>> _onEnter = new();
    private readonly Dictionary<T, List<OnStateExit<T>>> _onExit = new();

    public void AddOnEnter(T state, OnStateEnter<T> callback)
    {
        if (!_onEnter.TryGetValue(state, out var list))
        {
            list = [];
            _onEnter[state] = list;
        }
        list.Add(callback);
    }

    public void AddOnExit(T state, OnStateExit<T> callback)
    {
        if (!_onExit.TryGetValue(state, out var list))
        {
            list = [];
            _onExit[state] = list;
        }
        list.Add(callback);
    }

    internal void InvokeOnEnter(World world, T state)
    {
        if (_onEnter.TryGetValue(state, out var list))
        {
            foreach (var cb in list) cb(world, state);
        }
    }

    internal void InvokeOnExit(World world, T state)
    {
        if (_onExit.TryGetValue(state, out var list))
        {
            foreach (var cb in list) cb(world, state);
        }
    }
}

public enum LoadState
{
    NotLoaded,
    Loading,
    Loaded,
    Failed
}

/// <summary>
/// Extension methods for state management on Flecs World.
/// </summary>
public static class StateExtensions
{
    /// <summary>
    /// Initialize state management with an initial state.
    /// </summary>
    public static void InitState<T>(this World world, T initialState) where T : struct, IGameState
    {
        world.Set(new State<T>(initialState));
        world.Set(new NextState<T>());
        world.Set(new StateCallbacks<T>());
    }

    /// <summary>
    /// Get the current state.
    /// </summary>
    public static T GetCurrentState<T>(this World world) where T : struct, IGameState
    {
        return world.Get<State<T>>().Current;
    }

    /// <summary>
    /// Set the next state (will transition on next ProcessStateTransitions call).
    /// </summary>
    public static void SetNextState<T>(this World world, T newState) where T : struct, IGameState
    {
        world.GetMut<NextState<T>>().Set(newState);
    }

    /// <summary>
    /// Add a callback to be invoked when entering a state.
    /// </summary>
    public static void AddOnEnter<T>(this World world, T state, OnStateEnter<T> callback) where T : struct, IGameState
    {
        world.GetMut<StateCallbacks<T>>().AddOnEnter(state, callback);
    }

    /// <summary>
    /// Add a callback to be invoked when exiting a state.
    /// </summary>
    public static void AddOnExit<T>(this World world, T state, OnStateExit<T> callback) where T : struct, IGameState
    {
        world.GetMut<StateCallbacks<T>>().AddOnExit(state, callback);
    }

    /// <summary>
    /// Process pending state transitions. Call this once per frame.
    /// </summary>
    public static void ProcessStateTransitions<T>(this World world) where T : struct, IGameState
    {
        ref var nextState = ref world.GetMut<NextState<T>>();
        var pending = nextState.Take();
        if (!pending.HasValue)
        {
            return;
        }

        ref var state = ref world.GetMut<State<T>>();
        var callbacks = world.Get<StateCallbacks<T>>();

        var oldState = state.Current;
        callbacks.InvokeOnExit(world, oldState);

        state.Current = pending.Value;
        callbacks.InvokeOnEnter(world, pending.Value);
    }
}
