using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuBoneTransforms
{
    public const int MaxBones = 256;

    [InlineArray(MaxBones)]
    public struct BoneArray
    {
        private Matrix4x4 _element0;
    }

    public BoneArray Bones;

    public static GpuBoneTransforms Identity()
    {
        var result = new GpuBoneTransforms();
        for (var i = 0; i < MaxBones; i++)
        {
            result.Bones[i] = Matrix4x4.Identity;
        }
        return result;
    }

    public void CopyFrom(ReadOnlySpan<Matrix4x4> source)
    {
        var count = Math.Min(source.Length, MaxBones);
        for (var i = 0; i < count; i++)
        {
            Bones[i] = source[i];
        }
    }
}
