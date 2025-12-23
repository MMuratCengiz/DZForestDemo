using System.Numerics;
using System.Runtime.CompilerServices;
using ECS.Components;
using Flecs.NET.Core;

namespace ECS;

public static class TransformSystem
{
    public static void Register(World world)
    {
        world.System<Transform>("TransformSystem")
            .Kind(Ecs.OnUpdate)
            .Each((Entity entity, ref Transform transform) =>
            {
                var parent = entity.Parent();

                if (!parent.IsValid() || !parent.Has<Transform>())
                {
                    transform.LocalToWorld = transform.Matrix;
                }
                else
                {
                    ref readonly var parentTransform = ref parent.Get<Transform>();
                    transform.LocalToWorld = transform.Matrix * parentTransform.LocalToWorld;
                }
            });
    }
}

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
