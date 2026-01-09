using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics.Batching;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Binding;

public sealed class GpuDrawBatcher : IDisposable
{
    private const int MaxBonesPerDraw = 128;

    private readonly GraphicsContext _ctx;
    private readonly uint _numFrames;
    private readonly int _maxInstances;
    private readonly int _maxBones;

    private readonly Buffer[] _instanceBuffers;
    private readonly IntPtr[] _instanceMappedPtrs;
    private readonly BindGroup[] _instanceBindGroups;

    private readonly Buffer[] _boneBuffers;
    private readonly IntPtr[] _boneMappedPtrs;
    private readonly BindGroup[] _skinnedBindGroups;

    private readonly List<GpuDraw> _staticDraws = new();
    private readonly List<GpuDraw> _skinnedDraws = new();

    private int _currentInstanceOffset;
    private int _currentBoneOffset;
    private uint _currentFrameIndex;

    private bool _disposed;

    public GpuDrawBatcher(GraphicsContext ctx, int maxInstances = 4096, int maxBones = 256 * MaxBonesPerDraw)
    {
        _ctx = ctx;
        _numFrames = ctx.NumFrames;
        _maxInstances = maxInstances;
        _maxBones = maxBones;

        _instanceBuffers = new Buffer[_numFrames];
        _instanceMappedPtrs = new IntPtr[_numFrames];
        _instanceBindGroups = new BindGroup[_numFrames];

        _boneBuffers = new Buffer[_numFrames];
        _boneMappedPtrs = new IntPtr[_numFrames];
        _skinnedBindGroups = new BindGroup[_numFrames];

        var instanceStride = (ulong)Unsafe.SizeOf<GpuInstanceData>();
        var instanceBufferSize = instanceStride * (ulong)maxInstances;

        var boneStride = (ulong)Unsafe.SizeOf<Matrix4x4>();
        var boneBufferSize = boneStride * (ulong)maxBones;

        for (var i = 0; i < _numFrames; i++)
        {
            _instanceBuffers[i] = ctx.LogicalDevice.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                NumBytes = instanceBufferSize,
                StructureDesc = new StructuredBufferDesc
                {
                    Offset = 0,
                    NumElements = (ulong)maxInstances,
                    Stride = instanceStride
                },
                DebugName = StringView.Create($"GpuDrawBatcher_Instances_{i}")
            });
            _instanceMappedPtrs[i] = _instanceBuffers[i].MapMemory();
            _instanceBindGroups[i] = ctx.LogicalDevice.CreateBindGroup(new BindGroupDesc
            {
                Layout = ctx.BindGroupLayoutStore.Draw
            });
            _instanceBindGroups[i].BeginUpdate();
            _instanceBindGroups[i].SrvBuffer(GpuDrawLayout.Instances.Binding, _instanceBuffers[i]);
            _instanceBindGroups[i].EndUpdate();

            _boneBuffers[i] = ctx.LogicalDevice.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                NumBytes = boneBufferSize,
                StructureDesc = new StructuredBufferDesc
                {
                    Offset = 0,
                    NumElements = (ulong)maxBones,
                    Stride = boneStride
                },
                DebugName = StringView.Create($"GpuDrawBatcher_Bones_{i}")
            });
            _boneMappedPtrs[i] = _boneBuffers[i].MapMemory();

            _skinnedBindGroups[i] = ctx.LogicalDevice.CreateBindGroup(new BindGroupDesc
            {
                Layout = ctx.BindGroupLayoutStore.SkinnedDraw
            });
            _skinnedBindGroups[i].BeginUpdate();
            _skinnedBindGroups[i].SrvBuffer(GpuDrawLayout.Instances.Binding, _instanceBuffers[i]);
            _skinnedBindGroups[i].SrvBuffer(GpuDrawLayout.BoneMatrices.Binding, _boneBuffers[i]);
            _skinnedBindGroups[i].EndUpdate();
        }
    }

    public void BeginFrame(uint frameIndex)
    {
        _currentFrameIndex = frameIndex;
        _currentInstanceOffset = 0;
        _currentBoneOffset = 0;
        _staticDraws.Clear();
        _skinnedDraws.Clear();
    }

    public GpuDraw AddStaticDraw(MeshId mesh, ReadOnlySpan<GpuInstanceData> instances, int materialIndex = 0)
    {
        if (_currentInstanceOffset + instances.Length > _maxInstances)
        {
            return default;
        }

        var offset = _currentInstanceOffset;
        WriteInstances((int)_currentFrameIndex, offset, instances);
        _currentInstanceOffset += instances.Length;

        var draw = GpuDraw.CreateStatic(mesh, offset, instances.Length, materialIndex);
        _staticDraws.Add(draw);
        return draw;
    }

    public GpuDraw AddStaticDraw(MeshId mesh, in GpuInstanceData instance, int materialIndex = 0)
    {
        if (_currentInstanceOffset >= _maxInstances)
        {
            return default;
        }

        var offset = _currentInstanceOffset;
        WriteInstance((int)_currentFrameIndex, offset, in instance);
        _currentInstanceOffset++;

        var draw = GpuDraw.CreateStatic(mesh, offset, 1, materialIndex);
        _staticDraws.Add(draw);
        return draw;
    }

    public GpuDraw AddSkinnedDraw(MeshId mesh, GpuInstanceData instance, ReadOnlySpan<Matrix4x4> bones,
        int materialIndex = 0)
    {
        if (_currentInstanceOffset >= _maxInstances)
        {
            return default;
        }

        if (_currentBoneOffset + bones.Length > _maxBones)
        {
            return default;
        }

        var boneOffset = _currentBoneOffset;
        WriteBoneMatrices((int)_currentFrameIndex, boneOffset, bones);
        _currentBoneOffset += bones.Length;

        instance.BoneOffset = (uint)boneOffset;

        var instanceOffset = _currentInstanceOffset;
        WriteInstance((int)_currentFrameIndex, instanceOffset, in instance);
        _currentInstanceOffset++;

        var draw = GpuDraw.CreateSkinned(mesh, instanceOffset, boneOffset, bones.Length, materialIndex);
        _skinnedDraws.Add(draw);
        return draw;
    }

    public ReadOnlySpan<GpuDraw> StaticDraws => CollectionsMarshal.AsSpan(_staticDraws);
    public ReadOnlySpan<GpuDraw> SkinnedDraws => CollectionsMarshal.AsSpan(_skinnedDraws);

    public BindGroup GetInstanceBindGroup(uint frameIndex) => _instanceBindGroups[frameIndex];
    public BindGroup GetSkinnedBindGroup(uint frameIndex) => _skinnedBindGroups[frameIndex];

    public int CurrentInstanceOffset => _currentInstanceOffset;
    public int CurrentBoneOffset => _currentBoneOffset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void WriteInstance(int frameIndex, int instanceIndex, in GpuInstanceData data)
    {
        var ptr = (GpuInstanceData*)_instanceMappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void WriteInstances(int frameIndex, int startIndex, ReadOnlySpan<GpuInstanceData> data)
    {
        var ptr = (GpuInstanceData*)_instanceMappedPtrs[frameIndex].ToPointer();
        data.CopyTo(new Span<GpuInstanceData>(ptr + startIndex, data.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void WriteBoneMatrices(int frameIndex, int startIndex, ReadOnlySpan<Matrix4x4> bones)
    {
        var ptr = (Matrix4x4*)_boneMappedPtrs[frameIndex].ToPointer();
        bones.CopyTo(new Span<Matrix4x4>(ptr + startIndex, bones.Length));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _numFrames; i++)
        {
            _instanceBuffers[i].UnmapMemory();
            _instanceBindGroups[i].Dispose();
            _instanceBuffers[i].Dispose();

            _boneBuffers[i].UnmapMemory();
            _skinnedBindGroups[i].Dispose();
            _boneBuffers[i].Dispose();
        }
    }
}