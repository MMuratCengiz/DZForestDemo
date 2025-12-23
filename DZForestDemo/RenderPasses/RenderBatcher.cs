using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ECS.Components;
using Flecs.NET.Core;
using RuntimeAssets;
using RuntimeAssets.Components;

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

        GC.SuppressFinalize(this);
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

        world.Query<MeshComponent, Transform, StandardMaterial>()
            .Each((Entity entity, ref MeshComponent mesh, ref Transform transform, ref StandardMaterial material) =>
            {
                if (!mesh.IsValid)
                {
                    return;
                }

                if (entity.Has<AnimatorComponent>() && entity.Has<BoneMatricesComponent>())
                {
                    ref var boneMatrices = ref entity.GetMut<BoneMatricesComponent>();
                    if (boneMatrices.IsValid)
                    {
                        _animatedInstances.Add(new AnimatedInstanceData(
                            entity,
                            mesh.Mesh,
                            transform.LocalToWorld,
                            material,
                            boneMatrices.Data));
                        return;
                    }
                }

                if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
                {
                    list = [];
                    _meshBatches[mesh.Mesh] = list;
                }

                list.Add(new InstanceData(entity, transform.LocalToWorld, material));
            });

        world.Query<MeshComponent, Transform>()
            .Each((Entity entity, ref MeshComponent mesh, ref Transform transform) =>
            {
                // Skip entities that have StandardMaterial (handled in query above)
                if (entity.Has<StandardMaterial>())
                {
                    return;
                }

                if (!mesh.IsValid)
                {
                    return;
                }

                if (entity.Has<AnimatorComponent>() && entity.Has<BoneMatricesComponent>())
                {
                    ref var boneMatrices = ref entity.GetMut<BoneMatricesComponent>();
                    if (boneMatrices.IsValid)
                    {
                        _animatedInstances.Add(new AnimatedInstanceData(
                            entity,
                            mesh.Mesh,
                            transform.LocalToWorld,
                            DefaultMaterial,
                            boneMatrices.Data));
                        return;
                    }
                }

                if (!_meshBatches.TryGetValue(mesh.Mesh, out var list))
                {
                    list = [];
                    _meshBatches[mesh.Mesh] = list;
                }

                list.Add(new InstanceData(entity, transform.LocalToWorld, DefaultMaterial));
            });

        foreach (var kvp in _meshBatches)
        {
            if (kvp.Value.Count > 0)
            {
                _sortedMeshKeys.Add(kvp.Key);
            }
        }

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