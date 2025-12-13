using System.Runtime.CompilerServices;
using DenOfIz;

namespace ECS;

public class World : IDisposable
{
    private readonly Dictionary<Type, IContext> _contexts = new();
    private readonly List<ISystem> _systems = [];
    private ISystem[] _systemsArray = [];
    private bool _initialized;
    private bool _disposed;

    public void RegisterContext<T>(T context) where T : class, IContext
    {
        _contexts[typeof(T)] = context;
    }

    public T GetContext<T>() where T : class, IContext
    {
        return (T)_contexts[typeof(T)];
    }

    public T? TryGetContext<T>() where T : class, IContext
    {
        return _contexts.TryGetValue(typeof(T), out var context) ? (T)context : null;
    }

    public T AddSystem<T>(T system) where T : ISystem
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Cannot add systems after initialization.");
        }

        _systems.Add(system);
        return system;
    }

    public T? GetSystem<T>() where T : class, ISystem
    {
        foreach (var system in _systems)
        {
            if (system is T typed)
            {
                return typed;
            }
        }
        return null;
    }

    public void Initialize()
    {
        _systemsArray = _systems.ToArray();
        _initialized = true;

        foreach (var system in _systemsArray)
        {
            system.Initialize(this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(double deltaTime)
    {
        ReadOnlySpan<ISystem> systems = _systemsArray;
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Update(deltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LateUpdate(double deltaTime)
    {
        ReadOnlySpan<ISystem> systems = _systemsArray;
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].LateUpdate(deltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FixedUpdate(double fixedDeltaTime)
    {
        ReadOnlySpan<ISystem> systems = _systemsArray;
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].FixedUpdate(fixedDeltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Render(double deltaTime)
    {
        ReadOnlySpan<ISystem> systems = _systemsArray;
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Render(deltaTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool OnEvent(ref Event ev)
    {
        ReadOnlySpan<ISystem> systems = _systemsArray;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i].OnEvent(ref ev))
            {
                return true;
            }
        }
        return false;
    }

    public void Shutdown()
    {
        for (int i = _systems.Count - 1; i >= 0; i--)
        {
            _systems[i].Shutdown();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (int i = _systems.Count - 1; i >= 0; i--)
        {
            _systems[i].Dispose();
        }

        foreach (var context in _contexts.Values)
        {
            if (context is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }
}
