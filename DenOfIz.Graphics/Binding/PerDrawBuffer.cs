using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Binding;

/// <summary>
/// A ring buffer for per-draw data. Creates separate small buffers per draw slot,
/// each with its own bind group. Shader reads from Instances[0] and each draw
/// binds a different bind group pointing to its own buffer.
/// </summary>
public sealed class PerDrawBuffer<T> : IDisposable where T : unmanaged
{
    private readonly Buffer[][] _buffers;
    private readonly IntPtr[][] _mappedPtrs;
    private readonly ResourceBindGroup[][] _bindGroups;
    private readonly int[] _currentIndex;
    private readonly uint _numFrames;
    private readonly int _maxDrawsPerFrame;
    private readonly int _stride;

    public PerDrawBuffer(LogicalDevice device, RootSignature rootSignature, uint numFrames, int maxDrawsPerFrame, uint registerSpace, uint binding)
    {
        _numFrames = numFrames;
        _maxDrawsPerFrame = maxDrawsPerFrame;
        _stride = Unsafe.SizeOf<T>();

        _buffers = new Buffer[numFrames][];
        _mappedPtrs = new IntPtr[numFrames][];
        _bindGroups = new ResourceBindGroup[numFrames][];
        _currentIndex = new int[numFrames];

        for (var frame = 0; frame < numFrames; frame++)
        {
            _buffers[frame] = new Buffer[maxDrawsPerFrame];
            _mappedPtrs[frame] = new IntPtr[maxDrawsPerFrame];
            _bindGroups[frame] = new ResourceBindGroup[maxDrawsPerFrame];

            for (var draw = 0; draw < maxDrawsPerFrame; draw++)
            {
                // Create a small buffer for each draw slot
                _buffers[frame][draw] = device.CreateBuffer(new BufferDesc
                {
                    HeapType = HeapType.CpuGpu,
                    NumBytes = (ulong)_stride,
                    StructureDesc = new StructuredBufferDesc
                    {
                        Offset = 0,
                        NumElements = 1,
                        Stride = (ulong)_stride
                    },
                    DebugName = StringView.Create($"PerDraw_{typeof(T).Name}_{frame}_{draw}")
                });
                _mappedPtrs[frame][draw] = _buffers[frame][draw].MapMemory();

                // Create bind group pointing to this buffer
                var bindGroup = device.CreateResourceBindGroup(new ResourceBindGroupDesc
                {
                    RootSignature = rootSignature,
                    RegisterSpace = registerSpace
                });
                bindGroup.BeginUpdate();
                bindGroup.SrvBuffer(binding, _buffers[frame][draw]);
                bindGroup.EndUpdate();
                _bindGroups[frame][draw] = bindGroup;
            }
        }
    }

    public void BeginFrame(uint frameIndex)
    {
        _currentIndex[frameIndex] = 0;
    }

    /// <summary>
    /// Allocates a slot, writes the data to its buffer, and returns the bind group.
    /// Bind this group and draw - shader reads from Instances[0].
    /// </summary>
    public unsafe ResourceBindGroup Allocate(uint frameIndex, in T data)
    {
        var index = _currentIndex[frameIndex]++;
        if (index >= _maxDrawsPerFrame)
        {
            throw new InvalidOperationException($"Exceeded max draws per frame ({_maxDrawsPerFrame})");
        }

        var ptr = (T*)_mappedPtrs[frameIndex][index].ToPointer();
        *ptr = data;
        return _bindGroups[frameIndex][index];
    }

    public void Dispose()
    {
        for (var frame = 0; frame < _numFrames; frame++)
        {
            for (var draw = 0; draw < _maxDrawsPerFrame; draw++)
            {
                _bindGroups[frame][draw].Dispose();
                _buffers[frame][draw].UnmapMemory();
                _buffers[frame][draw].Dispose();
            }
        }
    }
}
