using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Batching;

public sealed class InstanceBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[] _buffers;
    private readonly ResourceBindGroup[] _bindGroups;
    private readonly IntPtr[] _mappedPtrs;
    private bool _disposed;

    public InstanceBuffer(
        LogicalDevice logicalDevice,
        RootSignature rootSignature,
        uint registerSpace,
        uint binding,
        int maxInstances,
        int numFrames)
    {
        MaxInstances = maxInstances;
        _buffers = new Buffer[numFrames];
        _bindGroups = new ResourceBindGroup[numFrames];
        _mappedPtrs = new IntPtr[numFrames];

        var stride = (ulong)Unsafe.SizeOf<T>();
        var bufferSize = stride * (ulong)maxInstances;

        for (var i = 0; i < numFrames; i++)
        {
            _buffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                NumBytes = bufferSize,
                StructureDesc = new StructuredBufferDesc
                {
                    Offset = 0,
                    NumElements = (ulong)maxInstances,
                    Stride = stride
                },
                DebugName = StringView.Create($"InstanceBuffer_{typeof(T).Name}_{i}")
            });
            _mappedPtrs[i] = _buffers[i].MapMemory();

            _bindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = rootSignature,
                RegisterSpace = registerSpace
            });
            _bindGroups[i].BeginUpdate();
            _bindGroups[i].SrvBuffer(binding, _buffers[i]);
            _bindGroups[i].EndUpdate();
        }
    }

    public int MaxInstances { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _buffers.Length; i++)
        {
            _buffers[i].UnmapMemory();
            _bindGroups[i].Dispose();
            _buffers[i].Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindGroup GetBindGroup(int frameIndex) => _bindGroups[frameIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* GetMappedPtr(int frameIndex) => (T*)_mappedPtrs[frameIndex].ToPointer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteInstance(int frameIndex, int instanceIndex, in T data)
    {
        if (instanceIndex >= MaxInstances)
        {
            return;
        }

        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteInstances(int frameIndex, int startIndex, ReadOnlySpan<T> data)
    {
        if (startIndex >= MaxInstances)
        {
            return;
        }

        var count = Math.Min(data.Length, MaxInstances - startIndex);
        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        data.Slice(0, count).CopyTo(new Span<T>(ptr + startIndex, count));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> GetWriteSpan(int frameIndex, int startIndex, int count)
    {
        if (startIndex >= MaxInstances)
        {
            return Span<T>.Empty;
        }

        var actualCount = Math.Min(count, MaxInstances - startIndex);
        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        return new Span<T>(ptr + startIndex, actualCount);
    }
}

public sealed class DynamicInstanceBuffer<T> : IDisposable where T : unmanaged
{
    private readonly LogicalDevice _logicalDevice;
    private readonly RootSignature _rootSignature;
    private readonly uint _registerSpace;
    private readonly uint _binding;
    private readonly int _numFrames;

    private readonly Buffer[] _buffers;
    private readonly ResourceBindGroup[] _bindGroups;
    private readonly IntPtr[] _mappedPtrs;
    private bool _disposed;

    public DynamicInstanceBuffer(
        LogicalDevice logicalDevice,
        RootSignature rootSignature,
        uint registerSpace,
        uint binding,
        int initialCapacity,
        int numFrames)
    {
        _logicalDevice = logicalDevice;
        _rootSignature = rootSignature;
        _registerSpace = registerSpace;
        _binding = binding;
        _numFrames = numFrames;
        Capacity = initialCapacity;

        _buffers = new Buffer[numFrames];
        _bindGroups = new ResourceBindGroup[numFrames];
        _mappedPtrs = new IntPtr[numFrames];

        AllocateBuffers(initialCapacity);
    }

    public int Capacity { get; private set; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _buffers.Length; i++)
        {
            _buffers[i].UnmapMemory();
            _bindGroups[i].Dispose();
            _buffers[i].Dispose();
        }
    }

    public void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= Capacity)
        {
            return;
        }

        var newCapacity = Math.Max(requiredCapacity, Capacity * 2);

        for (var i = 0; i < _numFrames; i++)
        {
            _buffers[i].UnmapMemory();
            _bindGroups[i].Dispose();
            _buffers[i].Dispose();
        }

        AllocateBuffers(newCapacity);
        Capacity = newCapacity;
    }

    private void AllocateBuffers(int capacity)
    {
        var stride = (ulong)Unsafe.SizeOf<T>();
        var bufferSize = stride * (ulong)capacity;

        for (var i = 0; i < _numFrames; i++)
        {
            _buffers[i] = _logicalDevice.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                NumBytes = bufferSize,
                StructureDesc = new StructuredBufferDesc
                {
                    Offset = 0,
                    NumElements = (ulong)capacity,
                    Stride = stride
                },
                DebugName = StringView.Create($"DynamicInstanceBuffer_{typeof(T).Name}_{i}")
            });
            _mappedPtrs[i] = _buffers[i].MapMemory();

            _bindGroups[i] = _logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = _registerSpace
            });
            _bindGroups[i].BeginUpdate();
            _bindGroups[i].SrvBuffer(_binding, _buffers[i]);
            _bindGroups[i].EndUpdate();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindGroup GetBindGroup(int frameIndex) => _bindGroups[frameIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* GetMappedPtr(int frameIndex) => (T*)_mappedPtrs[frameIndex].ToPointer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteInstance(int frameIndex, int instanceIndex, in T data)
    {
        if (instanceIndex >= Capacity)
        {
            return;
        }

        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> GetWriteSpan(int frameIndex, int startIndex, int count)
    {
        if (startIndex >= Capacity)
        {
            return Span<T>.Empty;
        }

        var actualCount = Math.Min(count, Capacity - startIndex);
        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        return new Span<T>(ptr + startIndex, actualCount);
    }
}
