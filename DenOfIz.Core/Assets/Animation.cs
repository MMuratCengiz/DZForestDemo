using System.Numerics;

namespace DenOfIz.World.Assets;

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

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if ((ulong)OzzContext != 0)
        {
            OzzContext = default;
        }
    }
}
