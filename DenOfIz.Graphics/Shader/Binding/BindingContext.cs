using System.Runtime.CompilerServices;
using DenOfIz;

namespace Graphics.Shader.Binding;

public sealed class BindingContext : IDisposable
{
    private readonly Dictionary<uint, ShaderBindingPool> _pools = [];
    private readonly Dictionary<uint, List<ResourceBindingSlot>> _slotsBySpace = [];
    private readonly CpuVisibleBufferAllocator _cbvAllocator;
    private bool _disposed;

    public LogicalDevice LogicalDevice { get; }
    public ShaderRootSignature RootSignature { get; }
    public IReadOnlyList<ResourceBindingSlot> ResourceBindingSlots { get; }

    public BindingContext(LogicalDevice logicalDevice, ShaderRootSignature rootSignature)
    {
        LogicalDevice = logicalDevice;
        RootSignature = rootSignature;
        ResourceBindingSlots = rootSignature.GetSlots();
        _cbvAllocator = new CpuVisibleBufferAllocator(logicalDevice);

        foreach (var slot in ResourceBindingSlots)
        {
            if (!_slotsBySpace.TryGetValue(slot.RegisterSpace, out var list))
            {
                list = [];
                _slotsBySpace[slot.RegisterSpace] = list;
            }
            list.Add(slot);
        }
    }

    public ShaderBindingPool GetOrCreatePool(uint registerSpace)
    {
        if (!_pools.TryGetValue(registerSpace, out var pool))
        {
            pool = new ShaderBindingPool(this, registerSpace);
            _pools[registerSpace] = pool;
        }
        return pool;
    }

    public ShaderBinding CreateBinding(uint registerSpace)
    {
        return new ShaderBinding(this, registerSpace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderBinding AcquireBinding(uint registerSpace)
    {
        return GetOrCreatePool(registerSpace).Acquire();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReleaseBinding(ShaderBinding binding)
    {
        if (binding.PoolHandle >= 0 && _pools.TryGetValue(binding.RegisterSpace, out var pool))
        {
            pool.Release(binding);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IReadOnlyList<ResourceBindingSlot> GetSlotsForSpace(uint registerSpace)
    {
        return _slotsBySpace.TryGetValue(registerSpace, out var list)
            ? list
            : Array.Empty<ResourceBindingSlot>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindingSlot GetSlot(string name)
    {
        return RootSignature.GetSlot(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindingDesc GetBindingDesc(string name)
    {
        return RootSignature.GetBindingDesc(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CpuVisibleBufferView GetFreeCpuVisibleAddress(object owner, string name, ulong size)
    {
        return _cbvAllocator.GetOrAllocate(owner, name, size);
    }

    public void ReleaseAllocations(object owner)
    {
        _cbvAllocator.Release(owner);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var pool in _pools.Values)
        {
            pool.Dispose();
        }
        _pools.Clear();
        _cbvAllocator.Dispose();

        GC.SuppressFinalize(this);
    }
}
