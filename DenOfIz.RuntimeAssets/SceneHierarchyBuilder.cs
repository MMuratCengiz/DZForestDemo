using System.Numerics;
using ECS;
using ECS.Components;
using RuntimeAssets;
using RuntimeAssets.Components;
using RuntimeAssets.GltfModels;

namespace DZForestDemo.Scenes;

public sealed class SceneHierarchyResult
{
    public required IReadOnlyDictionary<int, Entity> NodeToEntity { get; init; }
    public required IReadOnlyList<Entity> RootEntities { get; init; }
    public required IReadOnlyList<Entity> AllEntities { get; init; }
    public Entity? SkeletonRootEntity { get; init; }
}

public sealed class SceneHierarchyBuilder
{
    private readonly World _world;
    private readonly Scene _scene;
    private readonly Dictionary<int, Entity> _nodeToEntity = new();
    private readonly List<Entity> _rootEntities = [];
    private readonly List<Entity> _allEntities = [];

    public SceneHierarchyBuilder(World world, Scene scene)
    {
        _world = world;
        _scene = scene;
    }

    public SceneHierarchyResult Build(
        ModelLoadResult model,
        Vector3 rootPosition,
        Quaternion? rootRotation = null,
        Vector3? rootScale = null,
        Action<Entity, GltfNodeInfo, ModelLoadResult>? configureNode = null,
        bool meshNodesOnly = false)
    {
        if (!model.Success)
        {
            return new SceneHierarchyResult
            {
                NodeToEntity = _nodeToEntity,
                RootEntities = _rootEntities,
                AllEntities = _allEntities
            };
        }

        var rotation = rootRotation ?? Quaternion.Identity;
        var scale = rootScale ?? Vector3.One;

        HashSet<int> nodesToCreate;
        if (meshNodesOnly)
        {
            nodesToCreate = GetMeshNodesAndAncestors(model.Nodes);
        }
        else
        {
            nodesToCreate = model.Nodes.Select(n => n.Index).ToHashSet();
        }

        foreach (var node in model.Nodes)
        {
            if (!nodesToCreate.Contains(node.Index))
            {
                continue;
            }

            var entity = _scene.Spawn();
            _nodeToEntity[node.Index] = entity;
            _allEntities.Add(entity);

            if (!node.ParentIndex.HasValue || !nodesToCreate.Contains(node.ParentIndex.Value))
            {
                _rootEntities.Add(entity);
            }
        }

        foreach (var node in model.Nodes)
        {
            if (!nodesToCreate.Contains(node.Index))
            {
                continue;
            }

            var entity = _nodeToEntity[node.Index];

            Matrix4x4 transformToUse;
            var isRootEntity = !node.ParentIndex.HasValue || !nodesToCreate.Contains(node.ParentIndex.Value);

            if (isRootEntity)
            {
                var rootTransform = Matrix4x4.CreateScale(scale) *
                                    Matrix4x4.CreateFromQuaternion(rotation) *
                                    Matrix4x4.CreateTranslation(rootPosition);
                transformToUse = node.WorldTransform * rootTransform;
            }
            else
            {
                transformToUse = node.LocalTransform;
            }

            Matrix4x4.Decompose(transformToUse, out var nodeScale, out var nodeRotation, out var nodePosition);
            _world.AddComponent(entity, new Transform(nodePosition, nodeRotation, nodeScale));

            if (node.MeshIndex.HasValue && node.MeshIndex.Value < model.MeshHandles.Count)
            {
                var meshHandle = model.MeshHandles[node.MeshIndex.Value];
                if (meshHandle.IsValid)
                {
                    _world.AddComponent(entity, new MeshComponent(meshHandle));
                }
            }

            if (node.ParentIndex.HasValue && _nodeToEntity.TryGetValue(node.ParentIndex.Value, out var parentEntity))
            {
                _world.AddComponent(entity, new ParentRef(parentEntity));
            }

            configureNode?.Invoke(entity, node, model);
        }

        Entity? skeletonRootEntity = null;
        var skin = model.Skins.FirstOrDefault();
        if (skin?.SkeletonRoot.HasValue == true && _nodeToEntity.TryGetValue(skin.SkeletonRoot.Value, out var skelRoot))
        {
            skeletonRootEntity = skelRoot;
        }

        return new SceneHierarchyResult
        {
            NodeToEntity = _nodeToEntity,
            RootEntities = _rootEntities,
            AllEntities = _allEntities,
            SkeletonRootEntity = skeletonRootEntity
        };
    }

    public static void TraverseDepthFirst(
        IReadOnlyList<GltfNodeInfo> nodes,
        Action<GltfNodeInfo, int> visitor)
    {
        var rootNodes = nodes.Where(n => !n.ParentIndex.HasValue).ToList();
        foreach (var root in rootNodes)
        {
            TraverseNode(nodes, root, 0, visitor);
        }
    }

    private static void TraverseNode(
        IReadOnlyList<GltfNodeInfo> nodes,
        GltfNodeInfo node,
        int depth,
        Action<GltfNodeInfo, int> visitor)
    {
        visitor(node, depth);

        foreach (var childIndex in node.ChildIndices)
        {
            var child = nodes.FirstOrDefault(n => n.Index == childIndex);
            if (child != null)
            {
                TraverseNode(nodes, child, depth + 1, visitor);
            }
        }
    }

    public static IEnumerable<GltfNodeInfo> GetDescendants(
        IReadOnlyList<GltfNodeInfo> nodes,
        int nodeIndex,
        bool includeSelf = true)
    {
        var node = nodes.FirstOrDefault(n => n.Index == nodeIndex);
        if (node == null) yield break;

        if (includeSelf)
        {
            yield return node;
        }

        foreach (var childIndex in node.ChildIndices)
        {
            foreach (var descendant in GetDescendants(nodes, childIndex, true))
            {
                yield return descendant;
            }
        }
    }

    private static HashSet<int> GetMeshNodesAndAncestors(IReadOnlyList<GltfNodeInfo> nodes)
    {
        var result = new HashSet<int>();
        var nodeLookup = nodes.ToDictionary(n => n.Index);

        foreach (var node in nodes.Where(n => n.MeshIndex.HasValue))
        {
            result.Add(node.Index);

            var currentIndex = node.ParentIndex;
            while (currentIndex.HasValue)
            {
                if (!result.Add(currentIndex.Value))
                {
                    break;
                }

                if (nodeLookup.TryGetValue(currentIndex.Value, out var parentNode))
                {
                    currentIndex = parentNode.ParentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        return result;
    }

    public static IEnumerable<GltfNodeInfo> GetAncestors(
        IReadOnlyList<GltfNodeInfo> nodes,
        int nodeIndex)
    {
        var node = nodes.FirstOrDefault(n => n.Index == nodeIndex);
        if (node == null) yield break;

        var currentIndex = node.ParentIndex;
        while (currentIndex.HasValue)
        {
            var parent = nodes.FirstOrDefault(n => n.Index == currentIndex.Value);
            if (parent == null) yield break;

            yield return parent;
            currentIndex = parent.ParentIndex;
        }
    }

    public static IEnumerable<GltfNodeInfo> FindNodes(
        IReadOnlyList<GltfNodeInfo> nodes,
        Func<GltfNodeInfo, bool> predicate)
    {
        return nodes.Where(predicate);
    }

    public static IEnumerable<GltfNodeInfo> GetMeshNodes(IReadOnlyList<GltfNodeInfo> nodes)
    {
        return nodes.Where(n => n.MeshIndex.HasValue);
    }

    public static Dictionary<int, GltfNodeInfo> BuildNodeLookup(IReadOnlyList<GltfNodeInfo> nodes)
    {
        return nodes.ToDictionary(n => n.Index);
    }
}

public readonly record struct ParentRef(Entity Parent);