using RuntimeAssets;

namespace ECS.Components;

public struct SkeletonComponent
{
    public RuntimeSkeletonHandle Skeleton;

    public SkeletonComponent(RuntimeSkeletonHandle skeleton)
    {
        Skeleton = skeleton;
    }

    public bool IsValid => Skeleton.IsValid;
}

public struct AnimatorComponent
{
    public RuntimeSkeletonHandle Skeleton;
    public RuntimeAnimationHandle CurrentAnimation;
    public float PlaybackSpeed;
    public float CurrentTime;
    public bool IsPlaying;
    public bool Loop;

    public AnimatorComponent(RuntimeSkeletonHandle skeleton)
    {
        Skeleton = skeleton;
        CurrentAnimation = RuntimeAnimationHandle.Invalid;
        PlaybackSpeed = 1.0f;
        CurrentTime = 0.0f;
        IsPlaying = false;
        Loop = true;
    }

    public bool IsValid => Skeleton.IsValid;
}
