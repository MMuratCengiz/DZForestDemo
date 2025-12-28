using System.Runtime.CompilerServices;
using DenOfIz;

namespace Graphics.Binding;

public sealed class BindingContext : IDisposable
{
    private readonly CpuVisibleBufferAllocator _cbvAllocator;
    private bool _disposed;

    public LogicalDevice LogicalDevice { get; }
    public ShaderRootSignature RootSignature { get; }

    public BindingContext(LogicalDevice logicalDevice, ShaderRootSignature rootSignature)
    {
        LogicalDevice = logicalDevice;
        RootSignature = rootSignature;
        _cbvAllocator = new CpuVisibleBufferAllocator(logicalDevice);
    }


    public ShaderBinding CreateBinding(uint registerSpace)
    {
        return new ShaderBinding(this, registerSpace);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<ResourceBindingSlot> GetSlotsForSpace(uint registerSpace)
    {
        return RootSignature.GetSlotsForSpace(registerSpace);
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
        _cbvAllocator.Dispose();
    }
}
