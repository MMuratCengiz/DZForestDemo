using System.Numerics;
using NiziKit.Assets;
using NiziKit.Core;

namespace NiziKit.Components;

public class AnimatorComponent : IComponent
{
    private const int MaxBones = 128;

    public GameObject? Owner { get; set; }

    public Skeleton? Skeleton { get; set; }
    public Animation? CurrentAnimation { get; set; }
    public float AnimationTime { get; set; }
    public float PlaybackSpeed { get; set; } = 1.0f;
    public bool IsPlaying { get; set; }
    public bool Loop { get; set; }

    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    public int BoneCount { get; set; }

    public ReadOnlySpan<Matrix4x4> BoneMatrices => _boneMatrices.AsSpan(0, BoneCount);

    public void UpdateBoneMatrices(ReadOnlySpan<Matrix4x4> bones)
    {
        var count = Math.Min(bones.Length, MaxBones);
        bones[..count].CopyTo(_boneMatrices);
        BoneCount = count;
    }
}
