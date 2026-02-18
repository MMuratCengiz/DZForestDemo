using System.Numerics;
using DenOfIz;

namespace NiziKit.Assets;

public enum AnimationPath
{
    Translation,
    Rotation,
    Scale
}

public struct Keyframe
{
    public float Time { get; set; }
    public Vector4 Value { get; set; }
    public Vector4 InTangent { get; set; }
    public Vector4 OutTangent { get; set; }
    public bool HasInTangent { get; set; }
    public bool HasOutTangent { get; set; }
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
    public string Name { get; }
    public float Duration { get; }
    public List<AnimationChannel> Channels { get; set; } = [];
    public OzzContext OzzContext { get; private set; }

    private readonly Skeleton? _skeleton;
    private BinaryContainer? _animationData;

    internal Animation(string name, float duration, OzzContext context, BinaryContainer? animationData, Skeleton skeleton)
    {
        Name = name;
        Duration = duration;
        OzzContext = context;
        _animationData = animationData;
        _skeleton = skeleton;
    }

    internal Animation(string name, float duration, List<AnimationChannel> channels)
    {
        Name = name;
        Duration = duration;
        Channels = channels;
    }

    internal OzzContext CreateSamplingContext()
    {
        if (_skeleton?.OzzSkeleton == null || _animationData == null)
        {
            return default;
        }

        var context = _skeleton.OzzSkeleton.NewContext();
        if (!_skeleton.OzzSkeleton.LoadAnimationFromBinaryContainer(_animationData, context))
        {
            _skeleton.OzzSkeleton.DestroyContext(context);
            return default;
        }

        return context;
    }

    internal void DestroySamplingContext(OzzContext context)
    {
        if ((ulong)context != 0 && _skeleton?.OzzSkeleton?.IsValid() == true)
        {
            _skeleton.OzzSkeleton.DestroyContext(context);
        }
    }

    public void Dispose()
    {
        if ((ulong)OzzContext != 0 && _skeleton?.OzzSkeleton?.IsValid() == true)
        {
            _skeleton.OzzSkeleton.DestroyContext(OzzContext);
        }

        _animationData?.Dispose();
        OzzContext = default;
        _animationData = null;
    }
}
