using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuBoneTransforms
{
    public const int MaxBones = 256;

    private static readonly GpuBoneTransforms CachedIdentity = CreateIdentity();

    [InlineArray(MaxBones)]
    public struct BoneArray
    {
        private Matrix4x4 _element0;
    }

    public BoneArray Bones;

    private static GpuBoneTransforms CreateIdentity()
    {
        var result = new GpuBoneTransforms();
        for (var i = 0; i < MaxBones; i++)
        {
            result.Bones[i] = Matrix4x4.Identity;
        }
        return result;
    }

    public static GpuBoneTransforms Identity() => CachedIdentity;

    public void ResetToIdentity()
    {
        this = CachedIdentity;
    }

    public void CopyFrom(ReadOnlySpan<Matrix4x4> source)
    {
        var count = Math.Min(source.Length, MaxBones);
        source.Slice(0, count).CopyTo(MemoryMarshal.CreateSpan(ref Bones[0], MaxBones));
    }
}
