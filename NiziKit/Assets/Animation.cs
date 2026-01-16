using System.Numerics;
using DenOfIz;

namespace NiziKit.Assets;

public enum AnimationPath
{
    Translation,
    Rotation,
    Scale
}

public class Keyframe
{
    public float Time { get; set; }
    public Vector4 Value { get; set; }
    public Vector4? InTangent { get; set; }
    public Vector4? OutTangent { get; set; }
}

public class AnimationChannel
{
    public int JointIndex { get; set; }
    public string JointName { get; set; } = string.Empty;
    public AnimationPath Path { get; set; }
    public List<Keyframe> Keyframes { get; set; } = [];
}

public class Animation : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public float Duration { get; set; }
    public List<AnimationChannel> Channels { get; set; } = [];
    public OzzContext OzzContext { get; set; }

    public static Animation Load(string path, Skeleton skeleton)
    {
        var resolvedPath = AssetPaths.ResolveAnimation(path);
        var context = skeleton.OzzSkeleton.NewContext();

        if (!skeleton.OzzSkeleton.LoadAnimation(StringView.Create(resolvedPath), context))
        {
            skeleton.OzzSkeleton.DestroyContext(context);
            throw new InvalidOperationException($"Failed to load animation: {path}");
        }

        var duration = OzzAnimation.GetAnimationDuration(context);

        return new Animation
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Duration = duration,
            OzzContext = context
        };
    }

    public void Dispose()
    {
        if ((ulong)OzzContext != 0)
        {
            OzzContext = null;
        }
    }
}
