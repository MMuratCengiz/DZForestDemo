using System.Runtime.CompilerServices;

namespace ECS;

public readonly struct AssetLoadHandle : IEquatable<AssetLoadHandle>
{
    public readonly uint Id;
    public readonly uint Generation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AssetLoadHandle(uint id, uint generation)
    {
        Id = id;
        Generation = generation;
    }

    public static AssetLoadHandle Invalid => default;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(AssetLoadHandle other) => Id == other.Id && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is AssetLoadHandle other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, Generation);

    public static bool operator ==(AssetLoadHandle left, AssetLoadHandle right) => left.Equals(right);

    public static bool operator !=(AssetLoadHandle left, AssetLoadHandle right) => !left.Equals(right);
}

public sealed class AssetLoadTracker : IResource
{
    private readonly Dictionary<uint, LoadingAsset> _loading = new();
    private readonly Queue<uint> _freeIds = new();
    private uint _nextId = 1;

    public int LoadingCount => _loading.Count;

    public int TotalCount { get; private set; }

    public int CompletedCount { get; private set; }

    public float Progress => TotalCount == 0 ? 1f : (float)CompletedCount / TotalCount;

    public bool IsAllLoaded => _loading.Count == 0 && TotalCount > 0;

    public AssetLoadHandle Track(string name, Task task)
    {
        var id = _freeIds.Count > 0 ? _freeIds.Dequeue() : _nextId++;
        var handle = new AssetLoadHandle(id, 1);

        _loading[id] = new LoadingAsset
        {
            Name = name,
            Task = task,
            State = LoadState.Loading
        };

        TotalCount++;
        return handle;
    }

    public AssetLoadHandle Track(string name)
    {
        var id = _freeIds.Count > 0 ? _freeIds.Dequeue() : _nextId++;
        var handle = new AssetLoadHandle(id, 1);

        _loading[id] = new LoadingAsset
        {
            Name = name,
            Task = null,
            State = LoadState.Loading
        };

        TotalCount++;
        return handle;
    }

    public void MarkComplete(AssetLoadHandle handle)
    {
        if (_loading.TryGetValue(handle.Id, out var asset))
        {
            asset.State = LoadState.Loaded;
            _loading.Remove(handle.Id);
            _freeIds.Enqueue(handle.Id);
            CompletedCount++;
        }
    }

    public void MarkFailed(AssetLoadHandle handle, string? error = null)
    {
        if (_loading.TryGetValue(handle.Id, out var asset))
        {
            asset.State = LoadState.Failed;
            asset.Error = error;
            _loading.Remove(handle.Id);
            _freeIds.Enqueue(handle.Id);
            CompletedCount++;
        }
    }

    public LoadState GetState(AssetLoadHandle handle)
    {
        if (!handle.IsValid)
        {
            return LoadState.NotLoaded;
        }

        if (_loading.TryGetValue(handle.Id, out var asset))
        {
            return asset.State;
        }

        return LoadState.Loaded;
    }

    public void Update()
    {
        var toRemove = new List<uint>();

        foreach (var (id, asset) in _loading)
        {
            if (asset.Task == null)
            {
                continue;
            }

            if (asset.Task.IsCompletedSuccessfully)
            {
                asset.State = LoadState.Loaded;
                toRemove.Add(id);
                CompletedCount++;
            }
            else if (asset.Task.IsFaulted)
            {
                asset.State = LoadState.Failed;
                asset.Error = asset.Task.Exception?.Message;
                toRemove.Add(id);
                CompletedCount++;
            }
            else if (asset.Task.IsCanceled)
            {
                asset.State = LoadState.Failed;
                asset.Error = "Cancelled";
                toRemove.Add(id);
                CompletedCount++;
            }
        }

        foreach (var id in toRemove)
        {
            _loading.Remove(id);
            _freeIds.Enqueue(id);
        }
    }

    public void Reset()
    {
        _loading.Clear();
        _freeIds.Clear();
        _nextId = 1;
        TotalCount = 0;
        CompletedCount = 0;
    }

    public IEnumerable<string> GetLoadingAssetNames()
    {
        foreach (var asset in _loading.Values)
        {
            yield return asset.Name;
        }
    }

    private sealed class LoadingAsset
    {
        public required string Name;
        public Task? Task;
        public LoadState State;
        public string? Error;
    }
}
