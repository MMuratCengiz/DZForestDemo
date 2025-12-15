using RuntimeAssets;

namespace ECS.Components;

public struct SkeletonComponent(RuntimeSkeletonHandle skeleton)
{
    public RuntimeSkeletonHandle Skeleton = skeleton;

    public bool IsValid => Skeleton.IsValid;
}

public struct AnimatorComponent(RuntimeSkeletonHandle skeleton)
{
    public RuntimeSkeletonHandle Skeleton = skeleton;
    public RuntimeAnimationHandle CurrentAnimation = RuntimeAnimationHandle.Invalid;
    public float PlaybackSpeed = 1.0f;
    public float CurrentTime = 0.0f;
    public bool IsPlaying = false;
    public bool Loop = true;

    public bool IsValid => Skeleton.IsValid;
}
