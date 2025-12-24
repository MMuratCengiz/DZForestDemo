using System.Runtime.CompilerServices;
using DenOfIz;

namespace ECS;

public class World : IDisposable
{
    private readonly List<ISystem> _allSystems = [];
    private readonly Dictionary<Type, IResource> _resources = new();
    private readonly List<SystemDescriptor> _descriptors = [];

    private readonly Dictionary<Schedule, ISystem[]> _schedules = new();
    private ISystem[] _allSystemsArray = [];
    private bool _disposed;

    private bool _initialized;

    public World()
    {
        Entities = new EntityStore();
        Scenes = new SceneManager(Entities);
        Commands = new Commands(Entities);
    }

    public EntityStore Entities { get; }

    public SceneManager Scenes { get; }

    public Commands Commands { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = _allSystems.Count - 1; i >= 0; i--)
        {
            _allSystems[i].Dispose();
        }

        foreach (var resource in _resources.Values)
        {
            if (resource is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }

    public void RegisterResource<T>(T resource) where T : class, IResource
    {
        _resources[typeof(T)] = resource;
    }

    public T GetResource<T>() where T : class, IResource
    {
        return (T)_resources[typeof(T)];
    }

    public T? TryGetResource<T>() where T : class, IResource
    {
        return _resources.TryGetValue(typeof(T), out var resource) ? (T)resource : null;
    }

    public SystemDescriptor AddSystem<T>(T system, Schedule schedule) where T : ISystem
    {
        if (_initialized)
        {
            throw new InvalidOperationException("Cannot add systems after initialization.");
        }

        var descriptor = new SystemDescriptor(system, schedule);
        _descriptors.Add(descriptor);
        _allSystems.Add(system);
        return descriptor;
    }

    public T? GetSystem<T>() where T : class, ISystem
    {
        foreach (var system in _allSystems)
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
        BuildSchedules();
        _allSystemsArray = _allSystems.ToArray();
        _initialized = true;

        foreach (var system in _allSystemsArray)
        {
            system.Initialize(this);
        }
    }

    private void BuildSchedules()
    {
        var bySchedule = _descriptors.GroupBy(d => d.Schedule);

        foreach (var group in bySchedule)
        {
            var sorted = TopologicalSort(group.ToList());
            _schedules[group.Key] = sorted;
        }
    }

    private ISystem[] TopologicalSort(List<SystemDescriptor> descriptors)
    {
        if (descriptors.Count == 0)
        {
            return [];
        }

        var typeToDescriptor = descriptors.ToDictionary(d => d.System.GetType(), d => d);
        var inDegree = new Dictionary<SystemDescriptor, int>();
        var graph = new Dictionary<SystemDescriptor, List<SystemDescriptor>>();

        foreach (var desc in descriptors)
        {
            inDegree[desc] = 0;
            graph[desc] = [];
        }

        foreach (var desc in descriptors)
        {
            foreach (var beforeType in desc.RunBefore)
            {
                if (typeToDescriptor.TryGetValue(beforeType, out var beforeDesc))
                {
                    graph[desc].Add(beforeDesc);
                    inDegree[beforeDesc]++;
                }
            }

            foreach (var afterType in desc.RunAfter)
            {
                if (typeToDescriptor.TryGetValue(afterType, out var afterDesc))
                {
                    graph[afterDesc].Add(desc);
                    inDegree[desc]++;
                }
            }
        }

        var queue = new Queue<SystemDescriptor>();
        foreach (var desc in descriptors)
        {
            if (inDegree[desc] == 0)
            {
                queue.Enqueue(desc);
            }
        }

        var result = new List<ISystem>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current.System);

            foreach (var next in graph[current])
            {
                inDegree[next]--;
                if (inDegree[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        if (result.Count != descriptors.Count)
        {
            throw new InvalidOperationException("Circular dependency detected in system ordering.");
        }

        return result.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunSchedule(Schedule schedule)
    {
        if (!_schedules.TryGetValue(schedule, out var systems))
        {
            return;
        }

        ReadOnlySpan<ISystem> span = systems;
        for (var i = 0; i < span.Length; i++)
        {
            span[i].Run();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool OnEvent(ref Event ev)
    {
        ReadOnlySpan<ISystem> systems = _allSystemsArray;
        for (var i = 0; i < systems.Length; i++)
        {
            if (systems[i].OnEvent(ref ev))
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Spawn()
    {
        return Entities.Spawn();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Despawn(Entity entity)
    {
        Entities.Despawn(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddComponent<T>(Entity entity, in T component) where T : struct
    {
        Entities.AddComponent(entity, in component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        Entities.RemoveComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        return ref Entities.GetComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity entity) where T : struct
    {
        return Entities.HasComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct
    {
        return Entities.TryGetComponent(entity, out component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Query<T1> Query<T1>() where T1 : struct
    {
        return Entities.Query<T1>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Query<T1, T2> Query<T1, T2>() where T1 : struct where T2 : struct
    {
        return Entities.Query<T1, T2>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Query<T1, T2, T3> Query<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct
    {
        return Entities.Query<T1, T2, T3>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Query<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        return Entities.Query<T1, T2, T3, T4>();
    }

    public void ApplyCommands()
    {
        Commands.Apply();
        Scenes.CleanupDespawnedEntities();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T1>(T1 c1) where T1 : struct
    {
        return Entities.Create(c1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T1, T2>(T1 c1, T2 c2) where T1 : struct where T2 : struct
    {
        return Entities.Create(c1, c2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T1, T2, T3>(T1 c1, T2 c2, T3 c3) where T1 : struct where T2 : struct where T3 : struct
    {
        return Entities.Create(c1, c2, c3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T1, T2, T3, T4>(T1 c1, T2 c2, T3 c3, T4 c4) where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        return Entities.Create(c1, c2, c3, c4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Create<T1, T2, T3, T4, T5>(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5) where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        return Entities.Create(c1, c2, c3, c4, c5);
    }

    public void Shutdown()
    {
        for (var i = _allSystems.Count - 1; i >= 0; i--)
        {
            _allSystems[i].Shutdown();
        }
    }
}