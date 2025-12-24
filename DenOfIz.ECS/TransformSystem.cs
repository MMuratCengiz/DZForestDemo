using System.Numerics;
using System.Runtime.CompilerServices;
using ECS.Components;
using Flecs.NET.Core;

namespace ECS;

/// <summary>
/// Factory for creating the transform hierarchy system.
/// </summary>
public static class TransformSystem
{
    /// <summary>
    /// Registers the transform system that updates LocalToWorld matrices.
    /// Uses Flecs ChildOf relationships for hierarchy.
    /// </summary>
    public static void Register(World world)
    {
        world.System<Transform>("TransformSystem")
            .Without(Ecs.ChildOf, Ecs.Wildcard) // Only root entities
            .Kind(Ecs.OnUpdate)
            .Each((Entity entity, ref Transform transform) =>
            {
                transform.LocalToWorld = transform.Matrix;
                UpdateChildren(entity, transform.LocalToWorld);
            });
    }

    private static void UpdateChildren(Entity parent, Matrix4x4 parentWorld)
    {
        parent.Children(child =>
        {
            if (!child.Has<Transform>())
            {
                return;
            }

            ref var transform = ref child.GetMut<Transform>();
            transform.LocalToWorld = transform.Matrix * parentWorld;
            UpdateChildren(child, transform.LocalToWorld);
        });
    }
}

/// <summary>
/// Helper methods for transform hierarchy.
/// </summary>
public static class TransformHierarchy
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 ComputeWorldMatrix(Entity entity)
    {
        if (!entity.Has<Transform>())
        {
            return Matrix4x4.Identity;
        }

        ref readonly var transform = ref entity.Get<Transform>();
        var localMatrix = transform.Matrix;

        var parent = entity.Parent();
        if (parent.IsValid() && parent.Has<Transform>())
        {
            return localMatrix * ComputeWorldMatrix(parent);
        }

        return localMatrix;
    }

    public static void SetParent(Entity child, Entity parent)
    {
        if (parent.IsValid())
        {
            child.ChildOf(parent);
        }
        else
        {
            child.Remove(Ecs.ChildOf, Ecs.Wildcard);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity GetParent(Entity entity) => entity.Parent();
}
