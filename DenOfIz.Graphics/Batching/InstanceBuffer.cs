using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Batching;

public sealed class InstanceBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[] _buffers;
    private readonly ResourceBindGroup[] _bindGroups;
    private readonly IntPtr[] _mappedPtrs;
    private readonly int _maxInstances;
    private bool _disposed;

    public InstanceBuffer(
        LogicalDevice logicalDevice,
        RootSignature rootSignature,
        uint registerSpace,
        uint binding,
        int maxInstances,
        int numFrames)
    {
        _maxInstances = maxInstances;
        _buffers = new Buffer[numFrames];
        _bindGroups = new ResourceBindGroup[numFrames];
        _mappedPtrs = new IntPtr[numFrames];

        var stride = (ulong)Unsafe.SizeOf<T>();
        var bufferSize = stride * (ulong)maxInstances;

        for (var i = 0; i < numFrames; i++)
        {
            _buffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                // Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
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

    public int MaxInstances => _maxInstances;

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

        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ResourceBindGroup GetBindGroup(int frameIndex) => _bindGroups[frameIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* GetMappedPtr(int frameIndex) => (T*)_mappedPtrs[frameIndex].ToPointer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteInstance(int frameIndex, int instanceIndex, in T data)
    {
        if (instanceIndex >= _maxInstances)
        {
            return;
        }

        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteInstances(int frameIndex, int startIndex, ReadOnlySpan<T> data)
    {
        if (startIndex >= _maxInstances)
        {
            return;
        }

        var count = Math.Min(data.Length, _maxInstances - startIndex);
        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        data.Slice(0, count).CopyTo(new Span<T>(ptr + startIndex, count));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> GetWriteSpan(int frameIndex, int startIndex, int count)
    {
        if (startIndex >= _maxInstances)
        {
            return Span<T>.Empty;
        }

        var actualCount = Math.Min(count, _maxInstances - startIndex);
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

    private Buffer[] _buffers;
    private ResourceBindGroup[] _bindGroups;
    private IntPtr[] _mappedPtrs;
    private int _capacity;
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
        _capacity = initialCapacity;

        _buffers = new Buffer[numFrames];
        _bindGroups = new ResourceBindGroup[numFrames];
        _mappedPtrs = new IntPtr[numFrames];

        AllocateBuffers(initialCapacity);
    }

    public int Capacity => _capacity;

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

        GC.SuppressFinalize(this);
    }

    public void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _capacity)
        {
            return;
        }

        var newCapacity = Math.Max(requiredCapacity, _capacity * 2);

        for (var i = 0; i < _numFrames; i++)
        {
            _buffers[i].UnmapMemory();
            _bindGroups[i].Dispose();
            _buffers[i].Dispose();
        }

        AllocateBuffers(newCapacity);
        _capacity = newCapacity;
    }

    private void AllocateBuffers(int capacity)
    {
        var stride = (ulong)Unsafe.SizeOf<T>();
        var bufferSize = stride * (ulong)capacity;

        for (var i = 0; i < _numFrames; i++)
        {
            _buffers[i] = _logicalDevice.CreateBuffer(new BufferDesc
            {
                // Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
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
        if (instanceIndex >= _capacity)
        {
            return;
        }

        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Span<T> GetWriteSpan(int frameIndex, int startIndex, int count)
    {
        if (startIndex >= _capacity)
        {
            return Span<T>.Empty;
        }

        var actualCount = Math.Min(count, _capacity - startIndex);
        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        return new Span<T>(ptr + startIndex, actualCount);
    }
}
