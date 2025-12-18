using System.Runtime.CompilerServices;

namespace Graphics.Batching;

public readonly struct Batch<TKey>(TKey key, int startIndex, int count) where TKey : struct
{
    public readonly TKey Key = key;
    public readonly int StartIndex = startIndex;
    public readonly int Count = count;
}

public sealed class RenderBatcher<TKey, TInstance>(int maxInstances = 65536) : IDisposable
    where TKey : struct, IEquatable<TKey>
    where TInstance : struct
{
    private readonly Dictionary<TKey, List<TInstance>> _groups = new();
    private readonly List<TKey> _orderedKeys = [];
    private readonly List<Batch<TKey>> _batches = [];
    private readonly List<TInstance> _instances = [];
    private bool _disposed;

    public IReadOnlyList<Batch<TKey>> Batches => _batches;

    public IReadOnlyList<TInstance> Instances => _instances;

    public int BatchCount => _batches.Count;

    public int InstanceCount => _instances.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Clear();
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        foreach (var list in _groups.Values)
        {
            list.Clear();
        }
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, in TInstance instance)
    {
        if (!_groups.TryGetValue(key, out var list))
        {
            list = new List<TInstance>();
            _groups[key] = list;
        }
        list.Add(instance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(TKey key, ReadOnlySpan<TInstance> instances)
    {
        if (!_groups.TryGetValue(key, out var list))
        {
            list = new List<TInstance>(instances.Length);
            _groups[key] = list;
        }

        foreach (var inst in instances)
        {
            list.Add(inst);
        }
    }

    public void Build()
    {
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();

        foreach (var kvp in _groups)
        {
            if (kvp.Value.Count > 0)
            {
                _orderedKeys.Add(kvp.Key);
            }
        }

        var totalInstances = 0;
        foreach (var key in _orderedKeys)
        {
            var group = _groups[key];
            if (group.Count == 0)
            {
                continue;
            }

            var instanceCount = Math.Min(group.Count, maxInstances - totalInstances);
            if (instanceCount <= 0)
            {
                break;
            }

            var startIndex = totalInstances;
            for (var i = 0; i < instanceCount; i++)
            {
                _instances.Add(group[i]);
            }

            _batches.Add(new Batch<TKey>(key, startIndex, instanceCount));
            totalInstances += instanceCount;

            if (totalInstances >= maxInstances)
            {
                break;
            }
        }
    }

    public void Build<TComparer>(TComparer comparer) where TComparer : IComparer<TKey>
    {
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();

        foreach (var kvp in _groups)
        {
            if (kvp.Value.Count > 0)
            {
                _orderedKeys.Add(kvp.Key);
            }
        }

        _orderedKeys.Sort(comparer);

        var totalInstances = 0;
        foreach (var key in _orderedKeys)
        {
            var group = _groups[key];
            if (group.Count == 0)
            {
                continue;
            }

            var instanceCount = Math.Min(group.Count, maxInstances - totalInstances);
            if (instanceCount <= 0)
            {
                break;
            }

            var startIndex = totalInstances;
            for (var i = 0; i < instanceCount; i++)
            {
                _instances.Add(group[i]);
            }

            _batches.Add(new Batch<TKey>(key, startIndex, instanceCount));
            totalInstances += instanceCount;

            if (totalInstances >= maxInstances)
            {
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TInstance> GetBatchInstances(in Batch<TKey> batch)
    {
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_instances)
            .Slice(batch.StartIndex, batch.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TInstance GetInstance(int index)
    {
        return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_instances)[index];
    }

    public BatchEnumerator GetEnumerator() => new(this);

    public ref struct BatchEnumerator
    {
        private readonly RenderBatcher<TKey, TInstance> _batcher;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BatchEnumerator(RenderBatcher<TKey, TInstance> batcher)
        {
            _batcher = batcher;
            _index = -1;
        }

        public Batch<TKey> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _batcher._batches[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _batcher._batches.Count;
        }
    }
}

public sealed class MultiKeyBatcher<TKey1, TKey2, TInstance>(int maxInstances = 65536) : IDisposable
    where TKey1 : struct, IEquatable<TKey1>
    where TKey2 : struct, IEquatable<TKey2>
    where TInstance : struct
{
    private readonly Dictionary<(TKey1, TKey2), List<TInstance>> _groups = new();
    private readonly List<(TKey1, TKey2)> _orderedKeys = [];
    private readonly List<Batch<(TKey1, TKey2)>> _batches = [];
    private readonly List<TInstance> _instances = [];
    private bool _disposed;

    public IReadOnlyList<Batch<(TKey1, TKey2)>> Batches => _batches;

    public IReadOnlyList<TInstance> Instances => _instances;

    public int BatchCount => _batches.Count;

    public int InstanceCount => _instances.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Clear();
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        foreach (var list in _groups.Values)
        {
            list.Clear();
        }
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey1 key1, TKey2 key2, in TInstance instance)
    {
        var key = (key1, key2);
        if (!_groups.TryGetValue(key, out var list))
        {
            list = new List<TInstance>();
            _groups[key] = list;
        }
        list.Add(instance);
    }

    public void Build()
    {
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();

        foreach (var kvp in _groups)
        {
            if (kvp.Value.Count > 0)
            {
                _orderedKeys.Add(kvp.Key);
            }
        }

        var totalInstances = 0;
        foreach (var key in _orderedKeys)
        {
            var group = _groups[key];
            if (group.Count == 0)
            {
                continue;
            }

            var instanceCount = Math.Min(group.Count, maxInstances - totalInstances);
            if (instanceCount <= 0)
            {
                break;
            }

            var startIndex = totalInstances;
            for (var i = 0; i < instanceCount; i++)
            {
                _instances.Add(group[i]);
            }

            _batches.Add(new Batch<(TKey1, TKey2)>(key, startIndex, instanceCount));
            totalInstances += instanceCount;

            if (totalInstances >= maxInstances)
            {
                break;
            }
        }
    }

    public void Build<TComparer>(TComparer comparer) where TComparer : IComparer<(TKey1, TKey2)>
    {
        _orderedKeys.Clear();
        _batches.Clear();
        _instances.Clear();

        foreach (var kvp in _groups)
        {
            if (kvp.Value.Count > 0)
            {
                _orderedKeys.Add(kvp.Key);
            }
        }

        _orderedKeys.Sort(comparer);

        var totalInstances = 0;
        foreach (var key in _orderedKeys)
        {
            var group = _groups[key];
            if (group.Count == 0)
            {
                continue;
            }

            var instanceCount = Math.Min(group.Count, maxInstances - totalInstances);
            if (instanceCount <= 0)
            {
                break;
            }

            var startIndex = totalInstances;
            for (var i = 0; i < instanceCount; i++)
            {
                _instances.Add(group[i]);
            }

            _batches.Add(new Batch<(TKey1, TKey2)>(key, startIndex, instanceCount));
            totalInstances += instanceCount;

            if (totalInstances >= maxInstances)
            {
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<TInstance> GetBatchInstances(in Batch<(TKey1, TKey2)> batch)
    {
        return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_instances)
            .Slice(batch.StartIndex, batch.Count);
    }

    public BatchEnumerator GetEnumerator() => new(this);

    public ref struct BatchEnumerator
    {
        private readonly MultiKeyBatcher<TKey1, TKey2, TInstance> _batcher;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BatchEnumerator(MultiKeyBatcher<TKey1, TKey2, TInstance> batcher)
        {
            _batcher = batcher;
            _index = -1;
        }

        public Batch<(TKey1, TKey2)> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _batcher._batches[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _batcher._batches.Count;
        }
    }
}
