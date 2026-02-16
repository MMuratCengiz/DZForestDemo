using System.Numerics;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

/// <summary>
/// Fixes broken skeleton transforms in glTF documents by deriving correct joint
/// node transforms from the inverse bind matrices. Assimp's FBX→glTF exporter
/// can produce inconsistent data where node transforms don't match IBMs, causing
/// collapsed/distorted meshes. The IBMs are the ground truth (they match the mesh
/// vertex positions and skin weights), so we invert them to recover correct world
/// poses and rebuild the node hierarchy from those.
/// </summary>
public static class SkeletonNormalizer
{
    /// <summary>
    /// Parses GLB bytes, fixes skeleton transforms, and writes back to GLB.
    /// </summary>
    public static byte[] NormalizeGlb(byte[] glbBytes)
    {
        var document = GltfReader.ReadGlb(glbBytes);
        NormalizeSkeletonTransforms(document);
        return GlbWriter.WriteGlb(document);
    }

    /// <summary>
    /// Fixes skeleton transforms in-place on a parsed GltfDocument.
    /// For each skin, derives correct world bind poses from the IBMs and
    /// recomputes node local transforms to match.
    /// </summary>
    public static void NormalizeSkeletonTransforms(GltfDocument document)
    {
        var root = document.Root;
        if (root.Skins == null || root.Nodes == null)
        {
            return;
        }

        foreach (var skin in root.Skins)
        {
            NormalizeSkin(document, skin);
        }
    }

    private static void NormalizeSkin(GltfDocument document, GltfSkin skin)
    {
        var root = document.Root;
        var nodes = root.Nodes!;

        if (skin.Joints.Count == 0 || !skin.InverseBindMatrices.HasValue)
        {
            return;
        }

        var ibmReader = new GltfAccessorReader(document, skin.InverseBindMatrices.Value);
        if (ibmReader.Count < skin.Joints.Count)
        {
            return;
        }

        var nodeToJointIndex = new Dictionary<int, int>();
        for (var i = 0; i < skin.Joints.Count; i++)
        {
            nodeToJointIndex[skin.Joints[i]] = i;
        }

        var parentJointIndex = new int[skin.Joints.Count];
        for (var i = 0; i < skin.Joints.Count; i++)
        {
            parentJointIndex[i] = -1;
        }

        for (var i = 0; i < skin.Joints.Count; i++)
        {
            var nodeIdx = skin.Joints[i];
            var parentIdx = FindParentNode(nodes, nodeIdx);
            if (parentIdx.HasValue && nodeToJointIndex.TryGetValue(parentIdx.Value, out var pj))
            {
                parentJointIndex[i] = pj;
            }
        }

        var order = TopologicalSort(skin.Joints.Count, parentJointIndex);

        var worldBindPose = new Matrix4x4[skin.Joints.Count];
        for (var i = 0; i < skin.Joints.Count; i++)
        {
            var ibm = ibmReader.ReadMatrix4x4(i);
            if (!Matrix4x4.Invert(ibm, out worldBindPose[i]))
            {
                worldBindPose[i] = Matrix4x4.Identity;
            }
        }

        foreach (var ji in order)
        {
            var node = nodes[skin.Joints[ji]];

            Matrix4x4 newLocal;
            if (parentJointIndex[ji] >= 0)
            {
                Matrix4x4.Invert(worldBindPose[parentJointIndex[ji]], out var invParent);
                newLocal = worldBindPose[ji] * invParent;
            }
            else
            {
                var ancestorWorld = ComputeAncestorWorldTransform(nodes, skin.Joints[ji]);
                if (Matrix4x4.Invert(ancestorWorld, out var invAncestor))
                {
                    newLocal = worldBindPose[ji] * invAncestor;
                }
                else
                {
                    newLocal = worldBindPose[ji];
                }
            }

            if (!Matrix4x4.Decompose(newLocal, out var scale, out var rotation, out var translation))
            {
                scale = Vector3.One;
                rotation = Quaternion.Identity;
                translation = newLocal.Translation;
            }

            node.Translation = [translation.X, translation.Y, translation.Z];
            node.Rotation = [rotation.X, rotation.Y, rotation.Z, rotation.W];
            node.Scale = [scale.X, scale.Y, scale.Z];
            node.Matrix = null;
        }
    }

    /// <summary>
    /// Computes the accumulated world transform of all ancestors of a node
    /// (not including the node itself). Returns identity if the node is a scene root.
    /// </summary>
    private static Matrix4x4 ComputeAncestorWorldTransform(List<GltfNode> nodes, int nodeIndex)
    {
        var chain = new List<int>();
        var current = nodeIndex;

        while (true)
        {
            var parent = FindParentNode(nodes, current);
            if (!parent.HasValue)
            {
                break;
            }

            chain.Add(parent.Value);
            current = parent.Value;
        }

        var world = Matrix4x4.Identity;
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            world = GetNodeLocalTransform(nodes[chain[i]]) * world;
        }

        return world;
    }

    private static Matrix4x4 GetNodeLocalTransform(GltfNode node)
    {
        if (node.Matrix is { Length: 16 })
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

        return Matrix4x4.CreateScale(scale) *
               Matrix4x4.CreateFromQuaternion(rotation) *
               Matrix4x4.CreateTranslation(translation);
    }

    private static int? FindParentNode(List<GltfNode> nodes, int childIndex)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Children != null && nodes[i].Children!.Contains(childIndex))
            {
                return i;
            }
        }
        return null;
    }

    private static int[] TopologicalSort(int count, int[] parentIndex)
    {
        var order = new List<int>(count);
        var visited = new bool[count];

        void Visit(int ji)
        {
            if (visited[ji])
            {
                return;
            }

            if (parentIndex[ji] >= 0)
            {
                Visit(parentIndex[ji]);
            }

            visited[ji] = true;
            order.Add(ji);
        }

        for (var i = 0; i < count; i++)
        {
            Visit(i);
        }

        return order.ToArray();
    }
}
