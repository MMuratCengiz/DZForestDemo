using System.Numerics;
using RuntimeAssets;

namespace DenOfIz.World.Components;

public class AnimatorComponent : IComponent
{
    private const int MaxBones = 128;

    public GameObject? Owner { get; set; }

    public RuntimeSkeletonHandle Skeleton { get; set; } = RuntimeSkeletonHandle.Invalid;
    public RuntimeAnimationHandle CurrentAnimation { get; set; } = RuntimeAnimationHandle.Invalid;
    public float AnimationTime { get; set; }
    public float PlaybackSpeed { get; set; } = 1.0f;
    public bool IsPlaying { get; set; }

    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    public int BoneCount { get; set; }

    public ReadOnlySpan<Matrix4x4> BoneMatrices => _boneMatrices.AsSpan(0, BoneCount);

    public void UpdateBoneMatrices(ReadOnlySpan<Matrix4x4> bones)
    {
        var count = Math.Min(bones.Length, MaxBones);
        bones.Slice(0, count).CopyTo(_boneMatrices);
        BoneCount = count;
    }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnUpdate(float deltaTime) { }
}
