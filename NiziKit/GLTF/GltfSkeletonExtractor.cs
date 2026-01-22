using System.Numerics;
using NiziKit.Assets;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public sealed class GltfSkeletonData
{
    public string Name { get; set; } = string.Empty;
    public List<GltfJointData> Joints { get; set; } = [];
    public int[] RootJointIndices { get; set; } = [];
}

public sealed class GltfJointData
{
    public string Name { get; set; } = string.Empty;
    public int Index { get; set; }
    public int ParentIndex { get; set; } = -1;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 LocalTransform { get; set; } = Matrix4x4.Identity;
    public List<int> ChildIndices { get; set; } = [];
}

public static class GltfSkeletonExtractor
{
    public static List<GltfSkeletonData> ExtractSkeletons(GltfDocument document)
    {
        var result = new List<GltfSkeletonData>();
        var root = document.Root;

        if (root.Skins == null || root.Nodes == null)
        {
            return result;
        }

        foreach (var skin in root.Skins)
        {
            var skeleton = ExtractSkeleton(document, skin);
            result.Add(skeleton);
        }

        return result;
    }

    private static GltfSkeletonData ExtractSkeleton(GltfDocument document, GltfSkin skin)
    {
        var root = document.Root;
        var joints = new List<GltfJointData>();
        var jointNodeToIndex = new Dictionary<int, int>();

        for (var i = 0; i < skin.Joints.Count; i++)
        {
            jointNodeToIndex[skin.Joints[i]] = i;
        }

        Matrix4x4[]? inverseBindMatrices = null;
        if (skin.InverseBindMatrices.HasValue)
        {
            var reader = new GltfAccessorReader(document, skin.InverseBindMatrices.Value);
            inverseBindMatrices = new Matrix4x4[reader.Count];
            for (var i = 0; i < reader.Count; i++)
            {
                inverseBindMatrices[i] = reader.ReadMatrix4x4(i);
            }
        }

        for (var i = 0; i < skin.Joints.Count; i++)
        {
            var nodeIndex = skin.Joints[i];
            var node = root.Nodes![nodeIndex];

            var joint = new GltfJointData
            {
                Name = node.Name ?? $"Joint_{i}",
                Index = i,
                InverseBindMatrix = inverseBindMatrices?[i] ?? Matrix4x4.Identity,
                LocalTransform = GetNodeLocalTransformRaw(node)
            };

            var parentNodeIndex = FindParentNode(root.Nodes, nodeIndex);
            if (parentNodeIndex.HasValue && jointNodeToIndex.TryGetValue(parentNodeIndex.Value, out var parentJointIndex))
            {
                joint.ParentIndex = parentJointIndex;
            }

            if (node.Children != null)
            {
                foreach (var childNodeIndex in node.Children)
                {
                    if (jointNodeToIndex.TryGetValue(childNodeIndex, out var childJointIndex))
                    {
                        joint.ChildIndices.Add(childJointIndex);
                    }
                }
            }

            joints.Add(joint);
        }

        var rootJoints = joints
            .Where(j => j.ParentIndex == -1)
            .Select(j => j.Index)
            .ToArray();

        return new GltfSkeletonData
        {
            Name = skin.Name ?? "Skeleton",
            Joints = joints,
            RootJointIndices = rootJoints
        };
    }

    private static Matrix4x4 GetNodeLocalTransformRaw(GltfNode node)
    {
        if (node.Matrix != null && node.Matrix.Length == 16)
        {
            var m = node.Matrix;
            return new Matrix4x4(
                m[0], m[1], m[2], m[3],
                m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11],
                m[12], m[13], m[14], m[15]);
        }

        var translation = Vector3.Zero;
        var rotation = Quaternion.Identity;
        var scale = Vector3.One;

        if (node.Translation != null && node.Translation.Length >= 3)
        {
            translation = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
        }

        if (node.Rotation != null && node.Rotation.Length >= 4)
        {
            rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
        }

        if (node.Scale != null && node.Scale.Length >= 3)
        {
            scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
        }

        return Matrix4x4.CreateScale(scale) *
               Matrix4x4.CreateFromQuaternion(rotation) *
               Matrix4x4.CreateTranslation(translation);
    }

    private static int? FindParentNode(List<GltfNode> nodes, int childIndex)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.Children != null && node.Children.Contains(childIndex))
            {
                return i;
            }
        }
        return null;
    }

    public static Skeleton ToSkeleton(GltfSkeletonData data, bool convertToLeftHanded = true)
    {
        var joints = new List<Joint>();

        foreach (var jointData in data.Joints)
        {
            var inverseBindMatrix = jointData.InverseBindMatrix;
            var localTransform = jointData.LocalTransform;

            if (convertToLeftHanded)
            {
                inverseBindMatrix = ConvertMatrixToLeftHanded(inverseBindMatrix);
                localTransform = ConvertMatrixToLeftHanded(localTransform);
            }

            joints.Add(new Joint
            {
                Name = jointData.Name,
                Index = jointData.Index,
                ParentIndex = jointData.ParentIndex,
                InverseBindMatrix = inverseBindMatrix,
                LocalTransform = localTransform,
                ChildIndices = [.. jointData.ChildIndices]
            });
        }

        return new Skeleton
        {
            Name = data.Name,
            JointCount = data.Joints.Count,
            Joints = joints,
            RootJointIndices = data.RootJointIndices
        };
    }

    public static Matrix4x4 ConvertMatrixToLeftHanded(Matrix4x4 m)
    {
        return new Matrix4x4(
            m.M11, m.M12, -m.M13, m.M14,
            m.M21, m.M22, -m.M23, m.M24,
            -m.M31, -m.M32, m.M33, m.M34,
            m.M41, m.M42, -m.M43, m.M44);
    }

    public static Vector3 ConvertPositionToLeftHanded(Vector3 v)
    {
        return new Vector3(v.X, v.Y, -v.Z);
    }

    public static Quaternion ConvertRotationToLeftHanded(Quaternion q)
    {
        return new Quaternion(-q.X, -q.Y, q.Z, q.W);
    }
}
