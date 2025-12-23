using System.Numerics;
using ECS.Components;
using Flecs.NET.Core;
using RuntimeAssets.Components;
using RuntimeAssets.GltfModels;

namespace RuntimeAssets;

public sealed class ModelSpawnResult
{
    public IReadOnlyList<Entity> Entities { get; init; } = [];
    public IReadOnlyDictionary<int, Entity> NodeToEntity { get; init; } = new Dictionary<int, Entity>();
    public Entity RootEntity { get; init; }
}

public static class ModelEntitySpawner
{
    public static ModelSpawnResult Spawn(
        World world,
        ModelLoadResult model,
        Transform rootTransform,
        StandardMaterial? defaultMaterial = null)
    {
        if (!model.Success || model.Nodes.Count == 0)
        {
            return new ModelSpawnResult();
        }

        var nodeToEntity = new Dictionary<int, Entity>();
        var entities = new List<Entity>();
        Entity rootEntity = default;

        var rootNodes = model.Nodes
            .Where(n => !n.ParentIndex.HasValue)
            .ToList();

        foreach (var rootNode in rootNodes)
        {
            var entity = SpawnNodeHierarchy(
                world, model, rootNode, default,
                rootTransform, defaultMaterial, nodeToEntity, entities);

            if (!rootEntity.IsValid())
            {
                rootEntity = entity;
            }
        }

        return new ModelSpawnResult
        {
            Entities = entities,
            NodeToEntity = nodeToEntity,
            RootEntity = rootEntity
        };
    }

    public static ModelSpawnResult SpawnSkinned(
        World world,
        ModelLoadResult model,
        Transform rootTransform,
        RuntimeSkeletonHandle skeleton,
        RuntimeAnimationHandle animation,
        StandardMaterial? defaultMaterial = null)
    {
        if (!model.Success || model.Nodes.Count == 0)
        {
            return new ModelSpawnResult();
        }

        var nodeToEntity = new Dictionary<int, Entity>();
        var entities = new List<Entity>();
        Entity rootEntity = default;

        var rootNodes = model.Nodes
            .Where(n => !n.ParentIndex.HasValue)
            .ToList();

        foreach (var rootNode in rootNodes)
        {
            var entity = SpawnNodeHierarchySkinned(
                world, model, rootNode, default,
                rootTransform, skeleton, animation, defaultMaterial,
                nodeToEntity, entities);

            if (!rootEntity.IsValid())
            {
                rootEntity = entity;
            }
        }

        return new ModelSpawnResult
        {
            Entities = entities,
            NodeToEntity = nodeToEntity,
            RootEntity = rootEntity
        };
    }

    private static Entity SpawnNodeHierarchy(
        World world,
        ModelLoadResult model,
        GltfNodeInfo node,
        Entity parentEntity,
        Transform rootTransform,
        StandardMaterial? defaultMaterial,
        Dictionary<int, Entity> nodeToEntity,
        List<Entity> entities)
    {
        var entity = world.Entity();
        nodeToEntity[node.Index] = entity;
        entities.Add(entity);

        var localTransform = ConvertNodeTransform(node.LocalTransform);

        if (parentEntity.IsValid())
        {
            entity.Set(localTransform);
            entity.ChildOf(parentEntity);
        }
        else
        {
            var combinedTransform = CombineTransforms(rootTransform, localTransform);
            entity.Set(combinedTransform);
        }

        if (node.MeshIndex.HasValue && node.MeshIndex.Value < model.MeshHandles.Count)
        {
            var meshHandle = model.MeshHandles[node.MeshIndex.Value];
            entity.Set(new MeshComponent(meshHandle));

            var material = defaultMaterial ?? new StandardMaterial
            {
                BaseColor = Vector4.One,
                Metallic = 0f,
                Roughness = 0.5f
            };
            entity.Set(material);
        }

        foreach (var childIndex in node.ChildIndices)
        {
            if (childIndex >= 0 && childIndex < model.Nodes.Count)
            {
                var childNode = model.Nodes[childIndex];
                SpawnNodeHierarchy(
                    world, model, childNode, entity,
                    rootTransform, defaultMaterial, nodeToEntity, entities);
            }
        }

        return entity;
    }

    private static Entity SpawnNodeHierarchySkinned(
        World world,
        ModelLoadResult model,
        GltfNodeInfo node,
        Entity parentEntity,
        Transform rootTransform,
        RuntimeSkeletonHandle skeleton,
        RuntimeAnimationHandle animation,
        StandardMaterial? defaultMaterial,
        Dictionary<int, Entity> nodeToEntity,
        List<Entity> entities)
    {
        var entity = world.Entity();
        nodeToEntity[node.Index] = entity;
        entities.Add(entity);

        var localTransform = ConvertNodeTransform(node.LocalTransform);

        if (parentEntity.IsValid())
        {
            entity.Set(localTransform);
            entity.ChildOf(parentEntity);
        }
        else
        {
            var combinedTransform = CombineTransforms(rootTransform, localTransform);
            entity.Set(combinedTransform);
        }

        if (node.MeshIndex.HasValue && node.MeshIndex.Value < model.MeshHandles.Count)
        {
            var meshHandle = model.MeshHandles[node.MeshIndex.Value];
            entity.Set(new MeshComponent(meshHandle));

            var material = defaultMaterial ?? new StandardMaterial
            {
                BaseColor = Vector4.One,
                Metallic = 0f,
                Roughness = 0.5f
            };
            entity.Set(material);

            if (node.SkinIndex.HasValue && skeleton.IsValid)
            {
                var skinInfo = node.SkinIndex.Value < model.Skins.Count
                    ? model.Skins[node.SkinIndex.Value]
                    : null;

                var numJoints = skinInfo?.JointIndices.Count ?? model.InverseBindMatrices.Count;
                var inverseBindMatrices = skinInfo?.InverseBindMatrices ?? model.InverseBindMatrices;

                var animator = new AnimatorComponent(skeleton)
                {
                    CurrentAnimation = animation,
                    IsPlaying = animation.IsValid,
                    Loop = true,
                    PlaybackSpeed = 1.0f
                };
                entity.Set(animator);

                var boneMatrices = new BoneMatricesComponent(numJoints, inverseBindMatrices);
                entity.Set(boneMatrices);
            }
        }

        foreach (var childIndex in node.ChildIndices)
        {
            if (childIndex >= 0 && childIndex < model.Nodes.Count)
            {
                var childNode = model.Nodes[childIndex];
                SpawnNodeHierarchySkinned(
                    world, model, childNode, entity,
                    rootTransform, skeleton, animation, defaultMaterial,
                    nodeToEntity, entities);
            }
        }

        return entity;
    }

    private static Transform ConvertNodeTransform(Matrix4x4 nodeMatrix)
    {
        var converted = GltfCoordinateConversion.ConvertMatrixHandedness(nodeMatrix);

        Matrix4x4.Decompose(converted, out var scale, out var rotation, out var translation);

        return new Transform(translation, rotation, scale);
    }

    private static Transform CombineTransforms(Transform parent, Transform child)
    {
        var parentMatrix = parent.Matrix;
        var childMatrix = child.Matrix;
        var combined = childMatrix * parentMatrix;

        Matrix4x4.Decompose(combined, out var scale, out var rotation, out var translation);

        return new Transform(translation, rotation, scale);
    }
}
