namespace ECS;

public enum LoadState
{
    NotLoaded,
    Loading,
    Loaded,
    Failed
}

public interface IGameState
{
}

public class State<T> : IResource where T : struct, IGameState
{
    public T Current { get; internal set; }

    public State(T initial)
    {
        Current = initial;
    }
}

public class NextState<T> : IResource where T : struct, IGameState
{
    private T? _next;

    public bool HasPending => _next.HasValue;

    public T? Pending => _next;

    public void Set(T state)
    {
        _next = state;
    }

    internal T? Take()
    {
        var result = _next;
        _next = null;
        return result;
    }

    internal void Clear()
    {
        _next = null;
    }
}

public readonly struct StateTransitionEvent<T> where T : struct, IGameState
{
    public T From { get; init; }
    public T To { get; init; }
}

public delegate void OnStateEnter<T>(World world, T state) where T : struct, IGameState;
public delegate void OnStateExit<T>(World world, T state) where T : struct, IGameState;

public class StateCallbacks<T> : IResource where T : struct, IGameState
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
            foreach (var callback in list)
            {
                callback(world, state);
            }
        }
    }

    internal void InvokeOnExit(World world, T state)
    {
        if (_onExit.TryGetValue(state, out var list))
        {
            foreach (var callback in list)
            {
                callback(world, state);
            }
        }
    }
}
