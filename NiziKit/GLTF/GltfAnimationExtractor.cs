using System.Numerics;
using NiziKit.Assets;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public sealed class GltfAnimationData
{
    public string Name { get; set; } = string.Empty;
    public float Duration { get; set; }
    public List<GltfAnimationChannelData> Channels { get; set; } = [];
}

public sealed class GltfAnimationChannelData
{
    public int NodeIndex { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public AnimationPath Path { get; set; }
    public string Interpolation { get; set; } = GltfInterpolation.Linear;
    public List<GltfKeyframeData> Keyframes { get; set; } = [];
}

public sealed class GltfKeyframeData
{
    public float Time { get; set; }
    public Vector4 Value { get; set; }
    public Vector4? InTangent { get; set; }
    public Vector4? OutTangent { get; set; }
}

public static class GltfAnimationExtractor
{
    public static List<GltfAnimationData> ExtractAnimations(GltfDocument document)
    {
        var result = new List<GltfAnimationData>();
        var root = document.Root;

        if (root.Animations == null)
        {
            return result;
        }

        foreach (var animation in root.Animations)
        {
            var animData = ExtractAnimation(document, animation);
            result.Add(animData);
        }

        return result;
    }

    private static GltfAnimationData ExtractAnimation(GltfDocument document, GltfAnimation animation)
    {
        var root = document.Root;
        var channels = new List<GltfAnimationChannelData>();
        var duration = 0f;

        foreach (var channel in animation.Channels)
        {
            if (!channel.Target.Node.HasValue)
            {
                continue;
            }

            var sampler = animation.Samplers[channel.Sampler];
            var inputReader = new GltfAccessorReader(document, sampler.Input);
            var outputReader = new GltfAccessorReader(document, sampler.Output);

            var nodeIndex = channel.Target.Node.Value;
            var nodeName = root.Nodes?[nodeIndex].Name ?? $"Node_{nodeIndex}";

            var path = channel.Target.Path switch
            {
                GltfAnimationPath.Translation => AnimationPath.Translation,
                GltfAnimationPath.Rotation => AnimationPath.Rotation,
                GltfAnimationPath.Scale => AnimationPath.Scale,
                _ => AnimationPath.Translation
            };

            var keyframes = new List<GltfKeyframeData>();
            var isCubic = sampler.Interpolation == GltfInterpolation.CubicSpline;

            for (var i = 0; i < inputReader.Count; i++)
            {
                var time = inputReader.ReadFloat(i);
                duration = MathF.Max(duration, time);

                var keyframe = new GltfKeyframeData { Time = time };

                if (isCubic)
                {
                    keyframe.InTangent = ReadOutputValue(outputReader, i * 3, path);
                    keyframe.Value = ReadOutputValue(outputReader, i * 3 + 1, path);
                    keyframe.OutTangent = ReadOutputValue(outputReader, i * 3 + 2, path);
                }
                else
                {
                    keyframe.Value = ReadOutputValue(outputReader, i, path);
                }

                keyframes.Add(keyframe);
            }

            channels.Add(new GltfAnimationChannelData
            {
                NodeIndex = nodeIndex,
                NodeName = nodeName,
                Path = path,
                Interpolation = sampler.Interpolation,
                Keyframes = keyframes
            });
        }

        return new GltfAnimationData
        {
            Name = animation.Name ?? "Animation",
            Duration = duration,
            Channels = channels
        };
    }

    private static Vector4 ReadOutputValue(GltfAccessorReader reader, int index, AnimationPath path)
    {
        return path switch
        {
            AnimationPath.Translation => ToVector4(reader.ReadVector3(index)),
            AnimationPath.Rotation => reader.ReadVector4(index),
            AnimationPath.Scale => ToVector4(reader.ReadVector3(index)),
            _ => Vector4.Zero
        };
    }

    private static Vector4 ToVector4(Vector3 v) => new(v.X, v.Y, v.Z, 0);

    public static Assets.Animation ToAnimation(GltfAnimationData data, Dictionary<int, int>? nodeToJointIndex = null, bool convertToLeftHanded = true)
    {
        var channels = new List<AnimationChannel>();

        foreach (var channelData in data.Channels)
        {
            var jointIndex = nodeToJointIndex?.GetValueOrDefault(channelData.NodeIndex, -1) ?? channelData.NodeIndex;

            var keyframes = new List<Keyframe>();
            foreach (var kf in channelData.Keyframes)
            {
                var value = kf.Value;
                var inTangent = kf.InTangent ?? Vector4.Zero;
                var outTangent = kf.OutTangent ?? Vector4.Zero;
                var hasIn = kf.InTangent.HasValue;
                var hasOut = kf.OutTangent.HasValue;

                if (convertToLeftHanded)
                {
                    if (channelData.Path == AnimationPath.Translation)
                    {
                        value = new Vector4(value.X, value.Y, -value.Z, 0);
                        if (hasIn)
                        {
                            inTangent = new Vector4(inTangent.X, inTangent.Y, -inTangent.Z, 0);
                        }
                        if (hasOut)
                        {
                            outTangent = new Vector4(outTangent.X, outTangent.Y, -outTangent.Z, 0);
                        }
                    }
                    else if (channelData.Path == AnimationPath.Rotation)
                    {
                        value = new Vector4(-value.X, -value.Y, value.Z, value.W);
                        if (hasIn)
                        {
                            inTangent = new Vector4(-inTangent.X, -inTangent.Y, inTangent.Z, inTangent.W);
                        }
                        if (hasOut)
                        {
                            outTangent = new Vector4(-outTangent.X, -outTangent.Y, outTangent.Z, outTangent.W);
                        }
                    }
                }

                keyframes.Add(new Keyframe
                {
                    Time = kf.Time,
                    Value = value,
                    InTangent = inTangent,
                    OutTangent = outTangent,
                    HasInTangent = hasIn,
                    HasOutTangent = hasOut
                });
            }

            channels.Add(new AnimationChannel
            {
                JointIndex = jointIndex,
                JointName = channelData.NodeName,
                Path = channelData.Path,
                Keyframes = keyframes
            });
        }

        return new Assets.Animation(data.Name, data.Duration, channels);
    }
}
