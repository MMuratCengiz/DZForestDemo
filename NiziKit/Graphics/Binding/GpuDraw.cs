using NiziKit.Graphics.Batching;

namespace NiziKit.Graphics.Binding;

public enum GpuDrawType : byte
{
    Static,
    Skinned
}

public struct GpuDraw
{
    public GpuDrawType Type;
    public MeshId Mesh;
    public int InstanceOffset;
    public int InstanceCount;
    public int BoneOffset;
    public int BoneCount;
    public int MaterialIndex;

    public static GpuDraw CreateStatic(MeshId mesh, int instanceOffset, int instanceCount, int materialIndex = 0)
    {
        return new GpuDraw
        {
            Type = GpuDrawType.Static,
            Mesh = mesh,
            InstanceOffset = instanceOffset,
            InstanceCount = instanceCount,
            BoneOffset = 0,
            BoneCount = 0,
            MaterialIndex = materialIndex
        };
    }

    public static GpuDraw CreateSkinned(MeshId mesh, int instanceOffset, int boneOffset, int boneCount, int materialIndex = 0)
    {
        return new GpuDraw
        {
            Type = GpuDrawType.Skinned,
            Mesh = mesh,
            InstanceOffset = instanceOffset,
            InstanceCount = 1,
            BoneOffset = boneOffset,
            BoneCount = boneCount,
            MaterialIndex = materialIndex
        };
    }
}
