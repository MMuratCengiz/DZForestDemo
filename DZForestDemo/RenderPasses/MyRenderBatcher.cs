using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ECS;
using ECS.Components;
using Graphics.Batching;
using RuntimeAssets;
using RuntimeAssets.Components;

namespace DZForestDemo.RenderPasses;

[StructLayout(LayoutKind.Sequential)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct StaticInstance(Entity entity, Matrix4x4 worldMatrix, in StandardMaterial material)
{
    public readonly Entity Entity = entity;
    public readonly Matrix4x4 WorldMatrix = worldMatrix;
    public readonly Vector4 BaseColor = material.BaseColor;
    public readonly float Metallic = material.Metallic;
    public readonly float Roughness = material.Roughness;
    public readonly float AmbientOcclusion = material.AmbientOcclusion;
    public readonly RuntimeTextureHandle AlbedoTexture = material.AlbedoTexture;

    public static StaticInstance Default => new(
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

[StructLayout(LayoutKind.Sequential)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct AnimatedInstance(
    Entity entity,
    RuntimeMeshHandle meshHandle,
    Matrix4x4 worldMatrix,
    in StandardMaterial material,
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

public sealed class MyRenderBatcher(World world, int maxInstances = 4096) : IDisposable
{
    private static readonly StandardMaterial DefaultMaterial = new()
    {
        BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        Metallic = 0.0f,
        Roughness = 0.5f,
        AmbientOcclusion = 1.0f,
        AlbedoTexture = RuntimeTextureHandle.Invalid
    };

    private readonly RenderBatcher<RuntimeMeshHandle, StaticInstance> _staticBatcher = new(maxInstances);
    private readonly List<AnimatedInstance> _animatedInstances = [];
    private bool _disposed;

    public RenderBatcher<RuntimeMeshHandle, StaticInstance> StaticBatcher => _staticBatcher;

    public IReadOnlyList<AnimatedInstance> AnimatedInstances => _animatedInstances;

    public int StaticInstanceCount => _staticBatcher.InstanceCount;

    public int AnimatedInstanceCount => _animatedInstances.Count;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _staticBatcher.Dispose();
        _animatedInstances.Clear();
        GC.SuppressFinalize(this);
    }

    public void BuildBatches()
    {
        _staticBatcher.Clear();
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
                _animatedInstances.Add(new AnimatedInstance(
                    entity,
                    mesh.Mesh,
                    transform.Matrix,
                    material,
                    boneMatrices.Data));
                continue;
            }

            _staticBatcher.Add(mesh.Mesh, new StaticInstance(entity, transform.Matrix, material));
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
                _animatedInstances.Add(new AnimatedInstance(
                    entity,
                    mesh.Mesh,
                    transform.Matrix,
                    DefaultMaterial,
                    boneMatrices.Data));
                continue;
            }

            _staticBatcher.Add(mesh.Mesh, new StaticInstance(entity, transform.Matrix, DefaultMaterial));
        }

        _staticBatcher.Build();
    }

    public void BuildBatches<TFilter>(TFilter filter) where TFilter : IInstanceFilter
    {
        _staticBatcher.Clear();
        _animatedInstances.Clear();

        foreach (var (entity, mesh, transform, material) in world.Query<MeshComponent, Transform, StandardMaterial>())
        {
            if (!mesh.IsValid)
            {
                continue;
            }

            if (!filter.ShouldInclude(entity, transform.Position))
            {
                continue;
            }

            if (world.HasComponent<AnimatorComponent>(entity) &&
                world.TryGetComponent<BoneMatricesComponent>(entity, out var boneMatrices) &&
                boneMatrices.IsValid)
            {
                _animatedInstances.Add(new AnimatedInstance(
                    entity,
                    mesh.Mesh,
                    transform.Matrix,
                    material,
                    boneMatrices.Data));
                continue;
            }

            _staticBatcher.Add(mesh.Mesh, new StaticInstance(entity, transform.Matrix, material));
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

            if (!filter.ShouldInclude(entity, transform.Position))
            {
                continue;
            }

            if (world.HasComponent<AnimatorComponent>(entity) &&
                world.TryGetComponent<BoneMatricesComponent>(entity, out var boneMatrices) &&
                boneMatrices.IsValid)
            {
                _animatedInstances.Add(new AnimatedInstance(
                    entity,
                    mesh.Mesh,
                    transform.Matrix,
                    DefaultMaterial,
                    boneMatrices.Data));
                continue;
            }

            _staticBatcher.Add(mesh.Mesh, new StaticInstance(entity, transform.Matrix, DefaultMaterial));
        }

        _staticBatcher.Build();
    }
}

public interface IInstanceFilter
{
    bool ShouldInclude(Entity entity, Vector3 position);
}

public readonly struct FrustumFilter(Matrix4x4 viewProjection) : IInstanceFilter
{
    private readonly Plane[] _planes = ExtractFrustumPlanes(viewProjection);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldInclude(Entity entity, Vector3 position)
    {
        foreach (var plane in _planes)
        {
            if (Plane.DotCoordinate(plane, position) < 0)
            {
                return false;
            }
        }
        return true;
    }

    private static Plane[] ExtractFrustumPlanes(Matrix4x4 m)
    {
        return
        [
            Plane.Normalize(new Plane(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41)),
            Plane.Normalize(new Plane(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41)),
            Plane.Normalize(new Plane(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42)),
            Plane.Normalize(new Plane(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42)),
            Plane.Normalize(new Plane(m.M13, m.M23, m.M33, m.M43)),
            Plane.Normalize(new Plane(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43))
        ];
    }
}

public readonly struct DistanceFilter(Vector3 cameraPosition, float maxDistance) : IInstanceFilter
{
    private readonly Vector3 _cameraPosition = cameraPosition;
    private readonly float _maxDistanceSq = maxDistance * maxDistance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldInclude(Entity entity, Vector3 position)
    {
        return Vector3.DistanceSquared(_cameraPosition, position) <= _maxDistanceSq;
    }
}

public readonly struct NoFilter : IInstanceFilter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldInclude(Entity entity, Vector3 position) => true;
}
