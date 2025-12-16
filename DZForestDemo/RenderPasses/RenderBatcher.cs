using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using ECS;
using ECS.Components;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo.RenderPasses;

public readonly struct MeshBatch(RuntimeMeshHandle meshHandle, int startInstance, int instanceCount)
{
    public readonly RuntimeMeshHandle MeshHandle = meshHandle;
    public readonly int StartInstance = startInstance;
    public readonly int InstanceCount = instanceCount;
}

/// <summary>
/// Cached instance data including transform AND material - no per-frame component lookups needed!
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct InstanceData
{
    public readonly Entity Entity;
    public readonly Matrix4x4 WorldMatrix;
    public readonly Vector4 BaseColor;
    public readonly float Metallic;
    public readonly float Roughness;
    public readonly float AmbientOcclusion;
    private readonly float _padding;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InstanceData(Entity entity, Matrix4x4 worldMatrix, StandardMaterial material)
    {
        Entity = entity;
        WorldMatrix = worldMatrix;
        BaseColor = material.BaseColor;
        Metallic = material.Metallic;
        Roughness = material.Roughness;
        AmbientOcclusion = material.AmbientOcclusion;
        _padding = 0;
    }

    public static InstanceData Default => new(
        default,
        Matrix4x4.Identity,
        new StandardMaterial
        {
            BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
            Metallic = 0.0f,
            Roughness = 0.5f,
            AmbientOcclusion = 1.0f
        }
    );
}

public sealed class RenderBatcher : IDisposable
{
    private static readonly StandardMaterial DefaultMaterial = new()
    {
        BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        Metallic = 0.0f,
        Roughness = 0.5f,
        AmbientOcclusion = 1.0f
    };

    private readonly World _world;
    private readonly int _maxInstances;
    private readonly Dictionary<RuntimeMeshHandle, List<InstanceData>> _meshBatches = new();
    private readonly List<RuntimeMeshHandle> _sortedMeshKeys = [];
    private readonly List<MeshBatch> _batches = [];
    private readonly List<InstanceData> _allInstances = [];

    private bool _disposed;

    public RenderBatcher(World world, int maxInstances = 4096)
    {
        _world = world;
        _maxInstances = maxInstances;
    }

    public IReadOnlyList<MeshBatch> Batches => _batches;

    public IReadOnlyList<InstanceData> AllInstances => _allInstances;

    public int TotalInstanceCount => _allInstances.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _meshBatches.Clear();
        _sortedMeshKeys.Clear();
        _batches.Clear();
        _allInstances.Clear();

        GC.SuppressFinalize(this);
    }

    public void BuildBatches()
    {
        // Clear previous frame data
        foreach (var list in _meshBatches.Values)
        {
            list.Clear();
        }
        _sortedMeshKeys.Clear();
        _batches.Clear();
        _allInstances.Clear();

        // Query for entities with mesh, transform, AND material in one pass
        // This eliminates per-entity TryGetComponent calls during rendering!
        foreach (var (entity, mesh, transform, material) in _world.Query<MeshComponent, Transform, StandardMaterial>())
        {
            if (!mesh.IsValid)
            {
                continue;
            }

            if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
            {
                list = [];
                _meshBatches[mesh.Mesh] = list;
            }

            list.Add(new InstanceData(entity, transform.Matrix, material));
        }

        // Also handle entities without materials (use default)
        foreach (var (entity, mesh, transform) in _world.Query<MeshComponent, Transform>())
        {
            if (!mesh.IsValid)
            {
                continue;
            }

            // Skip if already processed (has material)
            if (_world.HasComponent<StandardMaterial>(entity))
            {
                continue;
            }

            if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
            {
                list = [];
                _meshBatches[mesh.Mesh] = list;
            }

            list.Add(new InstanceData(entity, transform.Matrix, DefaultMaterial));
        }

        // Collect non-empty mesh keys
        foreach (var kvp in _meshBatches)
        {
            if (kvp.Value.Count > 0)
            {
                _sortedMeshKeys.Add(kvp.Key);
            }
        }

        // Build final batches
        var totalInstances = 0;
        foreach (var meshHandle in _sortedMeshKeys)
        {
            var instances = _meshBatches[meshHandle];
            if (instances.Count == 0)
            {
                continue;
            }

            var instanceCount = Math.Min(instances.Count, _maxInstances - totalInstances);
            if (instanceCount <= 0)
            {
                break;
            }

            var startInstance = totalInstances;
            for (var i = 0; i < instanceCount; i++)
            {
                _allInstances.Add(instances[i]);
            }

            _batches.Add(new MeshBatch(meshHandle, startInstance, instanceCount));
            totalInstances += instanceCount;

            if (totalInstances >= _maxInstances)
            {
                break;
            }
        }
    }
}

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
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
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

    public ResourceBindGroup GetBindGroup(int frameIndex) => _bindGroups[frameIndex];

    public unsafe T* GetMappedPtr(int frameIndex) => (T*)_mappedPtrs[frameIndex].ToPointer();

    public unsafe void WriteInstance(int frameIndex, int instanceIndex, in T data)
    {
        if (instanceIndex >= _maxInstances)
        {
            return;
        }

        var ptr = (T*)_mappedPtrs[frameIndex].ToPointer();
        ptr[instanceIndex] = data;
    }
}
