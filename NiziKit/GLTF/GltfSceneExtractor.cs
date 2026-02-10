using System.Numerics;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public sealed class GltfSceneData
{
    public string Name { get; set; } = string.Empty;
    public List<GltfNodeData> Nodes { get; set; } = [];
    public List<int> RootNodeIndices { get; set; } = [];
}

public sealed class GltfNodeData
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentIndex { get; set; }
    public List<int> ChildIndices { get; set; } = [];
    public int? MeshIndex { get; set; }
    public int? SkinIndex { get; set; }
    public int? CameraIndex { get; set; }
    public Matrix4x4 LocalTransform { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 WorldTransform { get; set; } = Matrix4x4.Identity;
}

public static class GltfSceneExtractor
{
    public static GltfSceneData ExtractDefaultScene(GltfDocument document, bool convertToLeftHanded = true)
    {
        var root = document.Root;
        var sceneIndex = root.Scene ?? 0;

        if (root.Scenes == null || sceneIndex >= root.Scenes.Count)
        {
            return new GltfSceneData { Name = "Empty" };
        }

        return ExtractScene(document, sceneIndex, convertToLeftHanded);
    }

    public static GltfSceneData ExtractScene(GltfDocument document, int sceneIndex, bool convertToLeftHanded = true)
    {
        var root = document.Root;

        if (root.Scenes == null || sceneIndex >= root.Scenes.Count)
        {
            return new GltfSceneData { Name = "Empty" };
        }

        var scene = root.Scenes[sceneIndex];
        var nodes = new List<GltfNodeData>();
        var nodeParents = BuildParentMap(root.Nodes);

        if (root.Nodes != null)
        {
            for (var i = 0; i < root.Nodes.Count; i++)
            {
                var node = root.Nodes[i];
                var localTransform = GetNodeLocalTransform(node, convertToLeftHanded);

                var nodeData = new GltfNodeData
                {
                    Index = i,
                    Name = node.Name ?? $"Node_{i}",
                    MeshIndex = node.Mesh,
                    SkinIndex = node.Skin,
                    CameraIndex = node.Camera,
                    LocalTransform = localTransform,
                    ChildIndices = node.Children?.ToList() ?? []
                };

                if (nodeParents.TryGetValue(i, out var parent))
                {
                    nodeData.ParentIndex = parent;
                }

                nodes.Add(nodeData);
            }

            ComputeWorldTransforms(nodes);
        }

        return new GltfSceneData
        {
            Name = scene.Name ?? $"Scene_{sceneIndex}",
            Nodes = nodes,
            RootNodeIndices = scene.Nodes?.ToList() ?? []
        };
    }

    private static Dictionary<int, int> BuildParentMap(List<GltfNode>? nodes)
    {
        var result = new Dictionary<int, int>();

        if (nodes == null)
        {
            return result;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    result[childIndex] = i;
                }
            }
        }

        return result;
    }

    private static Matrix4x4 GetNodeLocalTransform(GltfNode node, bool convertToLeftHanded)
    {
        Matrix4x4 result;

        if (node.Matrix is { Length: 16 })
        {
            var m = node.Matrix;
            result = new Matrix4x4(
                m[0], m[1], m[2], m[3],
                m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11],
                m[12], m[13], m[14], m[15]);
        }
        else
        {
            var translation = Vector3.Zero;
            var rotation = Quaternion.Identity;
            var scale = Vector3.One;

            if (node.Translation is { Length: >= 3 })
            {
                translation = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            }

            if (node.Rotation is { Length: >= 4 })
            {
                rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            }

            if (node.Scale is { Length: >= 3 })
            {
                scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
            }

            if (convertToLeftHanded)
            {
                translation = GltfSkeletonExtractor.ConvertPositionToLeftHanded(translation);
                rotation = GltfSkeletonExtractor.ConvertRotationToLeftHanded(rotation);
            }

            result = Matrix4x4.CreateScale(scale) *
                     Matrix4x4.CreateFromQuaternion(rotation) *
                     Matrix4x4.CreateTranslation(translation);
        }

        if (convertToLeftHanded && node.Matrix != null)
        {
            result = GltfSkeletonExtractor.ConvertMatrixToLeftHanded(result);
        }

        return result;
    }

    private static void ComputeWorldTransforms(List<GltfNodeData> nodes)
    {
        var computed = new bool[nodes.Count];

        void ComputeRecursive(int index)
        {
            if (computed[index])
            {
                return;
            }

            var node = nodes[index];

            if (node.ParentIndex.HasValue)
            {
                ComputeRecursive(node.ParentIndex.Value);
                node.WorldTransform = node.LocalTransform * nodes[node.ParentIndex.Value].WorldTransform;
            }
            else
            {
                node.WorldTransform = node.LocalTransform;
            }

            computed[index] = true;
        }

        for (var i = 0; i < nodes.Count; i++)
        {
            ComputeRecursive(i);
        }
    }
}
