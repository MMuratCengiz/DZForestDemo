using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using ECS;
using ECS.Components;
using RuntimeAssets;
using RuntimeAssets.Components;
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
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct InstanceData(Entity entity, Matrix4x4 worldMatrix, StandardMaterial material)
{
    public readonly Entity Entity = entity;
    public readonly Matrix4x4 WorldMatrix = worldMatrix;
    public readonly Vector4 BaseColor = material.BaseColor;
    public readonly float Metallic = material.Metallic;
    public readonly float Roughness = material.Roughness;
    public readonly float AmbientOcclusion = material.AmbientOcclusion;
    public readonly RuntimeTextureHandle AlbedoTexture = material.AlbedoTexture;

    public static InstanceData Default => new(
        default,
        Matrix4x4.Identity,
        new StandardMaterial
        {
            BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
            Metallic = 0.0f,
            Roughness = 0.5f,
            AmbientOcclusion = 1.0f,
            AlbedoTexture = RuntimeTextureHandle.Invalid
        }
    );
}

/// <summary>
/// Data for animated/skinned mesh entities that need individual bone matrices
/// </summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct AnimatedInstanceData(
    Entity entity,
    RuntimeMeshHandle meshHandle,
    Matrix4x4 worldMatrix,
    StandardMaterial material,
    BoneMatricesData? boneMatrices)
{
    public readonly Entity Entity = entity;
    public readonly RuntimeMeshHandle MeshHandle = meshHandle;
    public readonly Matrix4x4 WorldMatrix = worldMatrix;
    public readonly Vector4 BaseColor = material.BaseColor;
    public readonly float Metallic = material.Metallic;
    public readonly float Roughness = material.Roughness;
    public readonly float AmbientOcclusion = material.AmbientOcclusion;
    public readonly RuntimeTextureHandle AlbedoTexture = material.AlbedoTexture;
    public readonly BoneMatricesData? BoneMatrices = boneMatrices;
}

public sealed class RenderBatcher(World world, int maxInstances = 4096) : IDisposable
{
    private static readonly StandardMaterial DefaultMaterial = new()
    {
        BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        Metallic = 0.0f,
        Roughness = 0.5f,
        AmbientOcclusion = 1.0f,
        AlbedoTexture = RuntimeTextureHandle.Invalid
    };

    private readonly Dictionary<RuntimeMeshHandle, List<InstanceData>> _meshBatches = new();
    private readonly List<RuntimeMeshHandle> _sortedMeshKeys = [];
    private readonly List<MeshBatch> _batches = [];
    private readonly List<InstanceData> _allInstances = [];
    private readonly List<AnimatedInstanceData> _animatedInstances = [];

    private bool _disposed;

    public IReadOnlyList<MeshBatch> Batches => _batches;

    public IReadOnlyList<InstanceData> AllInstances => _allInstances;

    public IReadOnlyList<AnimatedInstanceData> AnimatedInstances => _animatedInstances;

    public int TotalInstanceCount => _allInstances.Count;

    public int AnimatedInstanceCount => _animatedInstances.Count;

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
        _animatedInstances.Clear();
    }

    public void BuildBatches()
    {
        foreach (var list in _meshBatches.Values)
        {
            list.Clear();
        }
        _sortedMeshKeys.Clear();
        _batches.Clear();
        _allInstances.Clear();
        _animatedInstances.Clear();

        foreach (var (entity, mesh, transform, material) in world.Query<MeshComponent, Transform, StandardMaterial>())
        {
            if (!mesh.IsValid)
            {
                continue;
            }

            if (world.HasComponent<AnimatorComponent>(entity) &&
                world.TryGetComponent<BoneMatricesComponent>(entity, out var boneMatrices) &&
                boneMatrices.IsValid)
            {
                _animatedInstances.Add(new AnimatedInstanceData(
                    entity,
                    mesh.Mesh,
                    transform.LocalToWorld,
                    material,
                    boneMatrices.Data));
                continue;
            }

            if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
            {
                list = [];
                _meshBatches[mesh.Mesh] = list;
            }

            list.Add(new InstanceData(entity, transform.LocalToWorld, material));
        }

        foreach (var (entity, mesh, transform) in world.Query<MeshComponent, Transform>())
        {
            if (!mesh.IsValid)
            {
                continue;
            }

            if (world.HasComponent<StandardMaterial>(entity))
            {
                continue;
            }

            if (world.HasComponent<AnimatorComponent>(entity) &&
                world.TryGetComponent<BoneMatricesComponent>(entity, out var boneMatrices) &&
                boneMatrices.IsValid)
            {
                _animatedInstances.Add(new AnimatedInstanceData(
                    entity,
                    mesh.Mesh,
                    transform.LocalToWorld,
                    DefaultMaterial,
                    boneMatrices.Data));
                continue;
            }

            if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
            {
                list = [];
                _meshBatches[mesh.Mesh] = list;
            }

            list.Add(new InstanceData(entity, transform.LocalToWorld, DefaultMaterial));
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

            var instanceCount = Math.Min(instances.Count, maxInstances - totalInstances);
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

            if (totalInstances >= maxInstances)
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
