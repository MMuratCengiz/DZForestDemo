using System.Runtime.CompilerServices;

namespace ECS;

public ref struct Query<T1> where T1 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Query(EntityStore store)
    {
        _store = store;
        _c1 = Component<T1>.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<T1> GetEnumerator()
    {
        return new QueryEnumerator<T1>(_store, _c1);
    }
}

public ref struct QueryEnumerator<T1> where T1 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ReadOnlySpan<Archetype> _archetypes;
    private int _archetypeIndex;
    private int _entityIndex;
    private Archetype? _currentArchetype;
    private ComponentColumn<T1>? _column1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryEnumerator(EntityStore store, ComponentId c1)
    {
        _store = store;
        _c1 = c1;
        _archetypes = store.Archetypes;
        _archetypeIndex = -1;
        _entityIndex = -1;
        _currentArchetype = null;
        _column1 = null;
    }

    public QueryItem<T1> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_currentArchetype!.GetEntity(_entityIndex), ref _column1!.Get(_entityIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (true)
        {
            if (_currentArchetype != null)
            {
                _entityIndex++;
                if (_entityIndex < _currentArchetype.EntityCount)
                {
                    return true;
                }
            }

            _archetypeIndex++;
            while (_archetypeIndex < _archetypes.Length)
            {
                var archetype = _archetypes[_archetypeIndex];
                if (archetype.HasComponent(_c1) && archetype.EntityCount > 0)
                {
                    _currentArchetype = archetype;
                    _column1 = archetype.GetColumn<T1>();
                    _entityIndex = 0;
                    return true;
                }

                _archetypeIndex++;
            }

            return false;
        }
    }
}

public ref struct QueryItem<T1> where T1 : struct
{
    public readonly Entity Entity;
    private readonly ref T1 _c1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryItem(Entity entity, ref T1 c1)
    {
        Entity = entity;
        _c1 = ref c1;
    }

    public ref T1 Component1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Entity entity, out T1 c1)
    {
        entity = Entity;
        c1 = _c1;
    }
}

public ref struct Query<T1, T2> where T1 : struct where T2 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Query(EntityStore store)
    {
        _store = store;
        _c1 = Component<T1>.Id;
        _c2 = Component<T2>.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<T1, T2> GetEnumerator()
    {
        return new QueryEnumerator<T1, T2>(_store, _c1, _c2);
    }
}

public ref struct QueryEnumerator<T1, T2> where T1 : struct where T2 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;
    private readonly ReadOnlySpan<Archetype> _archetypes;
    private int _archetypeIndex;
    private int _entityIndex;
    private Archetype? _currentArchetype;
    private ComponentColumn<T1>? _column1;
    private ComponentColumn<T2>? _column2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryEnumerator(EntityStore store, ComponentId c1, ComponentId c2)
    {
        _store = store;
        _c1 = c1;
        _c2 = c2;
        _archetypes = store.Archetypes;
        _archetypeIndex = -1;
        _entityIndex = -1;
        _currentArchetype = null;
        _column1 = null;
        _column2 = null;
    }

    public QueryItem<T1, T2> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_currentArchetype!.GetEntity(_entityIndex), ref _column1!.Get(_entityIndex),
            ref _column2!.Get(_entityIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (true)
        {
            if (_currentArchetype != null)
            {
                _entityIndex++;
                if (_entityIndex < _currentArchetype.EntityCount)
                {
                    return true;
                }
            }

            _archetypeIndex++;
            while (_archetypeIndex < _archetypes.Length)
            {
                var archetype = _archetypes[_archetypeIndex];
                if (archetype.HasComponent(_c1) && archetype.HasComponent(_c2) && archetype.EntityCount > 0)
                {
                    _currentArchetype = archetype;
                    _column1 = archetype.GetColumn<T1>();
                    _column2 = archetype.GetColumn<T2>();
                    _entityIndex = 0;
                    return true;
                }

                _archetypeIndex++;
            }

            return false;
        }
    }
}

public ref struct QueryItem<T1, T2> where T1 : struct where T2 : struct
{
    public readonly Entity Entity;
    private readonly ref T1 _c1;
    private readonly ref T2 _c2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryItem(Entity entity, ref T1 c1, ref T2 c2)
    {
        Entity = entity;
        _c1 = ref c1;
        _c2 = ref c2;
    }

    public ref T1 Component1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c1;
    }

    public ref T2 Component2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Entity entity, out T1 c1, out T2 c2)
    {
        entity = Entity;
        c1 = _c1;
        c2 = _c2;
    }
}

public ref struct Query<T1, T2, T3> where T1 : struct where T2 : struct where T3 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;
    private readonly ComponentId _c3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Query(EntityStore store)
    {
        _store = store;
        _c1 = Component<T1>.Id;
        _c2 = Component<T2>.Id;
        _c3 = Component<T3>.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<T1, T2, T3> GetEnumerator()
    {
        return new QueryEnumerator<T1, T2, T3>(_store, _c1, _c2, _c3);
    }
}

public ref struct QueryEnumerator<T1, T2, T3> where T1 : struct where T2 : struct where T3 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;
    private readonly ComponentId _c3;
    private readonly ReadOnlySpan<Archetype> _archetypes;
    private int _archetypeIndex;
    private int _entityIndex;
    private Archetype? _currentArchetype;
    private ComponentColumn<T1>? _column1;
    private ComponentColumn<T2>? _column2;
    private ComponentColumn<T3>? _column3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryEnumerator(EntityStore store, ComponentId c1, ComponentId c2, ComponentId c3)
    {
        _store = store;
        _c1 = c1;
        _c2 = c2;
        _c3 = c3;
        _archetypes = store.Archetypes;
        _archetypeIndex = -1;
        _entityIndex = -1;
        _currentArchetype = null;
        _column1 = null;
        _column2 = null;
        _column3 = null;
    }

    public QueryItem<T1, T2, T3> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_currentArchetype!.GetEntity(_entityIndex), ref _column1!.Get(_entityIndex),
            ref _column2!.Get(_entityIndex), ref _column3!.Get(_entityIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (true)
        {
            if (_currentArchetype != null)
            {
                _entityIndex++;
                if (_entityIndex < _currentArchetype.EntityCount)
                {
                    return true;
                }
            }

            _archetypeIndex++;
            while (_archetypeIndex < _archetypes.Length)
            {
                var archetype = _archetypes[_archetypeIndex];
                if (archetype.HasComponent(_c1) && archetype.HasComponent(_c2) && archetype.HasComponent(_c3) &&
                    archetype.EntityCount > 0)
                {
                    _currentArchetype = archetype;
                    _column1 = archetype.GetColumn<T1>();
                    _column2 = archetype.GetColumn<T2>();
                    _column3 = archetype.GetColumn<T3>();
                    _entityIndex = 0;
                    return true;
                }

                _archetypeIndex++;
            }

            return false;
        }
    }
}

public ref struct QueryItem<T1, T2, T3> where T1 : struct where T2 : struct where T3 : struct
{
    public readonly Entity Entity;
    private readonly ref T1 _c1;
    private readonly ref T2 _c2;
    private readonly ref T3 _c3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryItem(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
    {
        Entity = entity;
        _c1 = ref c1;
        _c2 = ref c2;
        _c3 = ref c3;
    }

    public ref T1 Component1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c1;
    }

    public ref T2 Component2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c2;
    }

    public ref T3 Component3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Entity entity, out T1 c1, out T2 c2, out T3 c3)
    {
        entity = Entity;
        c1 = _c1;
        c2 = _c2;
        c3 = _c3;
    }
}

public ref struct Query<T1, T2, T3, T4> where T1 : struct where T2 : struct where T3 : struct where T4 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;
    private readonly ComponentId _c3;
    private readonly ComponentId _c4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Query(EntityStore store)
    {
        _store = store;
        _c1 = Component<T1>.Id;
        _c2 = Component<T2>.Id;
        _c3 = Component<T3>.Id;
        _c4 = Component<T4>.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QueryEnumerator<T1, T2, T3, T4> GetEnumerator()
    {
        return new QueryEnumerator<T1, T2, T3, T4>(_store, _c1, _c2, _c3, _c4);
    }
}

public ref struct QueryEnumerator<T1, T2, T3, T4>
    where T1 : struct where T2 : struct where T3 : struct where T4 : struct
{
    private readonly EntityStore _store;
    private readonly ComponentId _c1;
    private readonly ComponentId _c2;
    private readonly ComponentId _c3;
    private readonly ComponentId _c4;
    private readonly ReadOnlySpan<Archetype> _archetypes;
    private int _archetypeIndex;
    private int _entityIndex;
    private Archetype? _currentArchetype;
    private ComponentColumn<T1>? _column1;
    private ComponentColumn<T2>? _column2;
    private ComponentColumn<T3>? _column3;
    private ComponentColumn<T4>? _column4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryEnumerator(EntityStore store, ComponentId c1, ComponentId c2, ComponentId c3, ComponentId c4)
    {
        _store = store;
        _c1 = c1;
        _c2 = c2;
        _c3 = c3;
        _c4 = c4;
        _archetypes = store.Archetypes;
        _archetypeIndex = -1;
        _entityIndex = -1;
        _currentArchetype = null;
        _column1 = null;
        _column2 = null;
        _column3 = null;
        _column4 = null;
    }

    public QueryItem<T1, T2, T3, T4> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_currentArchetype!.GetEntity(_entityIndex), ref _column1!.Get(_entityIndex),
            ref _column2!.Get(_entityIndex), ref _column3!.Get(_entityIndex), ref _column4!.Get(_entityIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        while (true)
        {
            if (_currentArchetype != null)
            {
                _entityIndex++;
                if (_entityIndex < _currentArchetype.EntityCount)
                {
                    return true;
                }
            }

            _archetypeIndex++;
            while (_archetypeIndex < _archetypes.Length)
            {
                var archetype = _archetypes[_archetypeIndex];
                if (archetype.HasComponent(_c1) && archetype.HasComponent(_c2) && archetype.HasComponent(_c3) &&
                    archetype.HasComponent(_c4) && archetype.EntityCount > 0)
                {
                    _currentArchetype = archetype;
                    _column1 = archetype.GetColumn<T1>();
                    _column2 = archetype.GetColumn<T2>();
                    _column3 = archetype.GetColumn<T3>();
                    _column4 = archetype.GetColumn<T4>();
                    _entityIndex = 0;
                    return true;
                }

                _archetypeIndex++;
            }

            return false;
        }
    }
}

public ref struct QueryItem<T1, T2, T3, T4> where T1 : struct where T2 : struct where T3 : struct where T4 : struct
{
    public readonly Entity Entity;
    private readonly ref T1 _c1;
    private readonly ref T2 _c2;
    private readonly ref T3 _c3;
    private readonly ref T4 _c4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryItem(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
    {
        Entity = entity;
        _c1 = ref c1;
        _c2 = ref c2;
        _c3 = ref c3;
        _c4 = ref c4;
    }

    public ref T1 Component1
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c1;
    }

    public ref T2 Component2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c2;
    }

    public ref T3 Component3
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c3;
    }

    public ref T4 Component4
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _c4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Entity entity, out T1 c1, out T2 c2, out T3 c3, out T4 c4)
    {
        entity = Entity;
        c1 = _c1;
        c2 = _c2;
        c3 = _c3;
        c4 = _c4;
    }
}

public static class QueryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Query<T1> Query<T1>(this EntityStore store) where T1 : struct
    {
        return new Query<T1>(store);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Query<T1, T2> Query<T1, T2>(this EntityStore store) where T1 : struct where T2 : struct
    {
        return new Query<T1, T2>(store);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Query<T1, T2, T3> Query<T1, T2, T3>(this EntityStore store)
        where T1 : struct where T2 : struct where T3 : struct
    {
        return new Query<T1, T2, T3>(store);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Query<T1, T2, T3, T4> Query<T1, T2, T3, T4>(this EntityStore store) where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        return new Query<T1, T2, T3, T4>(store);
    }
}

/// <summary>
/// Interface for jobs that process chunks of entities in parallel.
/// Implement this to define parallel workloads.
/// </summary>
public interface IChunkJob<T1> where T1 : struct
{
    void Execute(ReadOnlySpan<Entity> entities, Span<T1> c1);
}

public interface IChunkJob<T1, T2> where T1 : struct where T2 : struct
{
    void Execute(ReadOnlySpan<Entity> entities, Span<T1> c1, Span<T2> c2);
}

public interface IChunkJob<T1, T2, T3> where T1 : struct where T2 : struct where T3 : struct
{
    void Execute(ReadOnlySpan<Entity> entities, Span<T1> c1, Span<T2> c2, Span<T3> c3);
}

/// <summary>
/// Archetype chunk info for parallel processing.
/// </summary>
public readonly struct ArchetypeChunk
{
    public readonly Archetype Archetype;
    public readonly int Start;
    public readonly int Count;

    public ArchetypeChunk(Archetype archetype, int start, int count)
    {
        Archetype = archetype;
        Start = start;
        Count = count;
    }
}

/// <summary>
/// Parallel query utilities for processing ECS data across multiple threads.
/// Uses archetype-level chunking for safe parallel iteration.
/// </summary>
public static class ParallelQuery
{
    /// <summary>
    /// Process all matching entities in parallel using a job struct.
    /// Each archetype is processed by a separate task.
    /// </summary>
    public static void Run<TJob, T1>(EntityStore store, TJob job)
        where TJob : struct, IChunkJob<T1>
        where T1 : struct
    {
        var c1Id = Component<T1>.Id;
        var chunks = new List<ArchetypeChunk>();

        foreach (var archetype in store.Archetypes)
        {
            if (archetype.HasComponent(c1Id) && archetype.EntityCount > 0)
            {
                chunks.Add(new ArchetypeChunk(archetype, 0, archetype.EntityCount));
            }
        }

        Parallel.ForEach(chunks, chunk =>
        {
            var localJob = job; // Copy for thread safety
            localJob.Execute(
                chunk.Archetype.Entities,
                chunk.Archetype.GetColumn<T1>().AsSpan()
            );
        });
    }

    /// <summary>
    /// Process all matching entities in parallel using a job struct.
    /// </summary>
    public static void Run<TJob, T1, T2>(EntityStore store, TJob job)
        where TJob : struct, IChunkJob<T1, T2>
        where T1 : struct where T2 : struct
    {
        var c1Id = Component<T1>.Id;
        var c2Id = Component<T2>.Id;
        var chunks = new List<ArchetypeChunk>();

        foreach (var archetype in store.Archetypes)
        {
            if (archetype.HasComponent(c1Id) && archetype.HasComponent(c2Id) && archetype.EntityCount > 0)
            {
                chunks.Add(new ArchetypeChunk(archetype, 0, archetype.EntityCount));
            }
        }

        Parallel.ForEach(chunks, chunk =>
        {
            var localJob = job;
            localJob.Execute(
                chunk.Archetype.Entities,
                chunk.Archetype.GetColumn<T1>().AsSpan(),
                chunk.Archetype.GetColumn<T2>().AsSpan()
            );
        });
    }

    /// <summary>
    /// Process all matching entities in parallel using a job struct.
    /// </summary>
    public static void Run<TJob, T1, T2, T3>(EntityStore store, TJob job)
        where TJob : struct, IChunkJob<T1, T2, T3>
        where T1 : struct where T2 : struct where T3 : struct
    {
        var c1Id = Component<T1>.Id;
        var c2Id = Component<T2>.Id;
        var c3Id = Component<T3>.Id;
        var chunks = new List<ArchetypeChunk>();

        foreach (var archetype in store.Archetypes)
        {
            if (archetype.HasComponent(c1Id) && archetype.HasComponent(c2Id) &&
                archetype.HasComponent(c3Id) && archetype.EntityCount > 0)
            {
                chunks.Add(new ArchetypeChunk(archetype, 0, archetype.EntityCount));
            }
        }

        Parallel.ForEach(chunks, chunk =>
        {
            var localJob = job;
            localJob.Execute(
                chunk.Archetype.Entities,
                chunk.Archetype.GetColumn<T1>().AsSpan(),
                chunk.Archetype.GetColumn<T2>().AsSpan(),
                chunk.Archetype.GetColumn<T3>().AsSpan()
            );
        });
    }

    /// <summary>
    /// Process entities in parallel with fine-grained batch size for better load balancing.
    /// </summary>
    public static void RunBatched<TJob, T1, T2>(EntityStore store, int batchSize, TJob job)
        where TJob : struct, IChunkJob<T1, T2>
        where T1 : struct where T2 : struct
    {
        var c1Id = Component<T1>.Id;
        var c2Id = Component<T2>.Id;
        var chunks = new List<ArchetypeChunk>();

        foreach (var archetype in store.Archetypes)
        {
            if (!archetype.HasComponent(c1Id) || !archetype.HasComponent(c2Id) || archetype.EntityCount == 0)
            {
                continue;
            }

            var count = archetype.EntityCount;
            for (var i = 0; i < count; i += batchSize)
            {
                chunks.Add(new ArchetypeChunk(archetype, i, Math.Min(batchSize, count - i)));
            }
        }

        Parallel.ForEach(chunks, chunk =>
        {
            var localJob = job;
            localJob.Execute(
                chunk.Archetype.Entities.Slice(chunk.Start, chunk.Count),
                chunk.Archetype.GetColumn<T1>().AsSpan().Slice(chunk.Start, chunk.Count),
                chunk.Archetype.GetColumn<T2>().AsSpan().Slice(chunk.Start, chunk.Count)
            );
        });
    }

    /// <summary>
    /// Get matching archetypes for manual parallel iteration.
    /// </summary>
    public static List<Archetype> GetMatchingArchetypes<T1>(EntityStore store)
        where T1 : struct
    {
        var c1Id = Component<T1>.Id;
        var result = new List<Archetype>();

        foreach (var archetype in store.Archetypes)
        {
            if (archetype.HasComponent(c1Id) && archetype.EntityCount > 0)
            {
                result.Add(archetype);
            }
        }

        return result;
    }

    /// <summary>
    /// Get matching archetypes for manual parallel iteration.
    /// </summary>
    public static List<Archetype> GetMatchingArchetypes<T1, T2>(EntityStore store)
        where T1 : struct where T2 : struct
    {
        var c1Id = Component<T1>.Id;
        var c2Id = Component<T2>.Id;
        var result = new List<Archetype>();

        foreach (var archetype in store.Archetypes)
        {
            if (archetype.HasComponent(c1Id) && archetype.HasComponent(c2Id) && archetype.EntityCount > 0)
            {
                result.Add(archetype);
            }
        }

        return result;
    }
}