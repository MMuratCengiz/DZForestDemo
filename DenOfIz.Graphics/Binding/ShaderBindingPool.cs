using System.Collections.Concurrent;
using DenOfIz;

namespace Graphics.Binding;

public class ShaderBindingPool : IDisposable
{
    private readonly ConcurrentStack<ShaderBinding> _freePool = new();
    private readonly ConcurrentDictionary<int, ShaderBinding> _allBindings = new();
    private readonly LogicalDevice _logicalDevice;
    private readonly ShaderRootSignature _rootSignature;
    private readonly uint _registerSpace;

    private readonly ReaderWriterLockSlim _growLock = new();
    private readonly Lock _growTaskLock = new();

    private volatile int _totalCount;
    private volatile int _inUseCount;
    private volatile bool _isGrowing;
    private volatile bool _disposed;

    private const float GrowthThreshold = 0.6f;
    private const float GrowthFactor = 1.5f;

    public int TotalCount => _totalCount;
    public int InUseCount => _inUseCount;
    public int FreeCount => _totalCount - _inUseCount;
    public float UsageRatio => _totalCount > 0 ? (float)_inUseCount / _totalCount : 0f;

    public ShaderBinding this[int handle]
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_allBindings.TryGetValue(handle, out var binding))
            {
                throw new ArgumentOutOfRangeException(nameof(handle), $"No binding exists with handle {handle}");
            }
            return binding;
        }
    }

    public bool TryGet(int handle, out ShaderBinding? binding)
    {
        if (_disposed)
        {
            binding = null;
            return false;
        }
        return _allBindings.TryGetValue(handle, out binding);
    }

    public ShaderBindingPool(LogicalDevice logicalDevice, ShaderRootSignature rootSignature, uint registerSpace,
        int initialCapacity = 256)
    {
        _logicalDevice = logicalDevice;
        _rootSignature = rootSignature;
        _registerSpace = registerSpace;

        CreateBindings(initialCapacity);
    }

    private void CreateBindings(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var binding = CreateBinding();
            var handle = Interlocked.Increment(ref _totalCount) - 1;
            binding.PoolHandle = handle;
            _allBindings[handle] = binding;
            _freePool.Push(binding);
        }
    }

    private ShaderBinding CreateBinding()
    {
        var ctx = new BindingContext(_logicalDevice, _rootSignature);
        return new ShaderBinding(ctx, _registerSpace);
    }

    public ShaderBinding Acquire()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CheckAndTriggerGrowth();
        _growLock.EnterReadLock();
        try
        {
            if (_freePool.TryPop(out var binding))
            {
                Interlocked.Increment(ref _inUseCount);
                return binding;
            }
        }
        finally
        {
            _growLock.ExitReadLock();
        }
        
        GrowSync(Math.Max(16, _totalCount / 2));
        _growLock.EnterReadLock();
        try
        {
            if (_freePool.TryPop(out var binding))
            {
                Interlocked.Increment(ref _inUseCount);
                return binding;
            }
        }
        finally
        {
            _growLock.ExitReadLock();
        }

        throw new InvalidOperationException("Failed to acquire binding from pool");
    }

    public void Release(ShaderBinding binding)
    {
        if (binding.PoolHandle < 0)
        {
            throw new ArgumentException("Binding does not belong to this pool", nameof(binding));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        binding.Reset();

        _growLock.EnterReadLock();
        try
        {
            _freePool.Push(binding);
            Interlocked.Decrement(ref _inUseCount);
        }
        finally
        {
            _growLock.ExitReadLock();
        }
    }

    private void CheckAndTriggerGrowth()
    {
        if (_isGrowing)
        {
            return;
        }

        var ratio = UsageRatio;
        if (ratio >= GrowthThreshold)
        {
            lock (_growTaskLock)
            {
                if (!_isGrowing && UsageRatio >= GrowthThreshold)
                {
                    _isGrowing = true;
                    var growAmount = (int)(_totalCount * (GrowthFactor - 1));
                    growAmount = Math.Max(growAmount, 16);
                    _ = GrowAsync(growAmount);
                }
            }
        }
    }

    private async Task GrowAsync(int count)
    {
        try
        {
            var newBindings = await Task.Run(() =>
            {
                var bindings = new List<ShaderBinding>(count);
                for (var i = 0; i < count; i++)
                {
                    bindings.Add(CreateBinding());
                }
                return bindings;
            });

            _growLock.EnterWriteLock();
            try
            {
                foreach (var binding in newBindings)
                {
                    var handle = Interlocked.Increment(ref _totalCount) - 1;
                    binding.PoolHandle = handle;
                    _allBindings[handle] = binding;
                    _freePool.Push(binding);
                }
            }
            finally
            {
                _growLock.ExitWriteLock();
            }
        }
        finally
        {
            _isGrowing = false;
        }
    }

    private void GrowSync(int count)
    {
        _growLock.EnterWriteLock();
        try
        {
            for (var i = 0; i < count; i++)
            {
                var binding = CreateBinding();
                var handle = Interlocked.Increment(ref _totalCount) - 1;
                binding.PoolHandle = handle;
                _allBindings[handle] = binding;
                _freePool.Push(binding);
            }
        }
        finally
        {
            _growLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var binding in _allBindings.Values)
        {
            binding.Dispose();
        }

        _allBindings.Clear();
        _freePool.Clear();
        _growLock.Dispose();

        GC.SuppressFinalize(this);
    }
}