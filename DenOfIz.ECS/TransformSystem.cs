using System.Numerics;
using System.Runtime.CompilerServices;
using ECS.Components;

namespace ECS;

public sealed class TransformSystem : ISystem
{
    private World _world = null!;
    private readonly List<Entity> _rootEntities = [];
    private readonly Dictionary<Entity, List<Entity>> _childrenMap = [];
    private readonly HashSet<Entity> _processedEntities = [];

    public void Initialize(World world)
    {
        _world = world;
    }

    public void Run()
    {
        BuildHierarchy();
        UpdateWorldMatrices();
    }

    private void BuildHierarchy()
    {
        _rootEntities.Clear();
        _childrenMap.Clear();

        foreach (var item in _world.Query<Transform>())
        {
            var entity = item.Entity;

            if (_world.TryGetComponent<Parent>(entity, out var parent) && parent.HasParent)
            {
                if (!_childrenMap.TryGetValue(parent.Value, out var children))
                {
                    children = [];
                    _childrenMap[parent.Value] = children;
                }
                children.Add(entity);
            }
            else
            {
                _rootEntities.Add(entity);
            }
        }
    }

    private void UpdateWorldMatrices()
    {
        _processedEntities.Clear();

        foreach (var root in _rootEntities)
        {
            UpdateEntityAndChildren(root, Matrix4x4.Identity);
        }
    }

    private void UpdateEntityAndChildren(Entity entity, Matrix4x4 parentWorld)
    {
        if (_processedEntities.Contains(entity))
        {
            return;
        }
        _processedEntities.Add(entity);

        ref var transform = ref _world.GetComponent<Transform>(entity);

        var localMatrix = transform.Matrix;
        var worldMatrix = localMatrix * parentWorld;
        transform.LocalToWorld = worldMatrix;

        if (_childrenMap.TryGetValue(entity, out var children))
        {
            foreach (var child in children)
            {
                UpdateEntityAndChildren(child, worldMatrix);
            }
        }
    }

    public void Dispose()
    {
        _rootEntities.Clear();
        _childrenMap.Clear();
        _processedEntities.Clear();
    }
}

public static class TransformHierarchy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ComputeWorldMatrix(World world, Entity entity)
    {
        if (!world.TryGetComponent<Transform>(entity, out var transform))
        {
            return Matrix4x4.Identity;
        }

        var localMatrix = transform.Matrix;

        if (world.TryGetComponent<Parent>(entity, out var parent) && parent.HasParent)
        {
            var parentWorld = ComputeWorldMatrix(world, parent.Value);
            return localMatrix * parentWorld;
        }

        return localMatrix;
    }

    public static void SetParent(World world, Entity child, Entity parent)
    {
        if (parent.IsValid)
        {
            if (world.HasComponent<Parent>(child))
            {
                ref var existingParent = ref world.GetComponent<Parent>(child);
                existingParent.Value = parent;
            }
            else
            {
                world.AddComponent(child, new Parent(parent));
            }
        }
        else if (world.HasComponent<Parent>(child))
        {
            world.RemoveComponent<Parent>(child);
        }
    }
}
