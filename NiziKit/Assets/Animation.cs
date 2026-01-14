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

    public void Dispose()
    {
        if ((ulong)OzzContext != 0)
        {
            OzzContext = default;
        }
    }
}
