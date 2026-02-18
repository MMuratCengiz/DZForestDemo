using System.Numerics;
using DenOfIz;
using NiziKit.ContentPipeline;

namespace NiziKit.Assets;

public class Skeleton : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public int JointCount { get; set; }
    public List<Joint> Joints { get; set; } = [];
    public int[] RootJointIndices { get; set; } = [];
    public OzzSkeleton OzzSkeleton { get; set; } = null!;

    private OzzExporterRuntime? _exporter;
    private BinaryContainer? _skeletonData;
    private readonly Dictionary<string, Animation> _animations = new();
    private IReadOnlyList<string>? _animationNames;
    private byte[]? _sourceBytes;

    public IReadOnlyList<string> AnimationNames => _animationNames ??= BuildAnimationNames();
    public uint AnimationCount => (uint)AnimationNames.Count;

    private List<string> BuildAnimationNames()
    {
        if (_exporter != null)
        {
            return _exporter.GetAnimationNames()?.ToList() ?? [];
        }

        return _animations.Keys.ToList();
    }

    public static Skeleton Load(string modelPath)
    {
        var bytes = Content.ReadBytes(modelPath);
        return LoadFromBytes(bytes, modelPath);
    }

    public static async Task<Skeleton> LoadAsync(string modelPath, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync(modelPath, ct);
        return LoadFromBytes(bytes, modelPath);
    }

    public static Skeleton LoadFromBytes(byte[] gltfBytes, string name)
    {
        var exporter = new OzzExporterRuntime();

        if (!exporter.LoadFromMemory(gltfBytes))
        {
            exporter.Dispose();
            throw new InvalidOperationException($"Failed to load glTF for skeleton: {name}");
        }

        if (!exporter.HasSkeleton)
        {
            exporter.Dispose();
            throw new InvalidOperationException($"glTF file does not contain a skeleton: {name}");
        }

        var skeletonData = exporter.BuildSkeleton();
        if (skeletonData == null)
        {
            exporter.Dispose();
            throw new InvalidOperationException($"Failed to build skeleton from glTF: {name}");
        }

        var ozzSkeleton = OzzSkeleton.CreateFromBinaryContainer(skeletonData);

        if (!ozzSkeleton.IsValid())
        {
            skeletonData.Dispose();
            exporter.Dispose();
            ozzSkeleton.Dispose();
            throw new InvalidOperationException($"Failed to create OzzSkeleton from skeleton data: {name}");
        }

        var jointCount = ozzSkeleton.GetNumJoints();
        var joints = ExtractJoints(ozzSkeleton);

        return new Skeleton
        {
            Name = Path.GetFileNameWithoutExtension(name),
            JointCount = jointCount,
            Joints = joints,
            RootJointIndices = [0],
            OzzSkeleton = ozzSkeleton,
            _exporter = exporter,
            _skeletonData = skeletonData,
            _sourceBytes = gltfBytes
        };
    }

    public static Skeleton Load(byte[] ozzSkelData, string? name = null)
    {
        var skeletonContainer = CreateBinaryContainer(ozzSkelData);
        var ozzSkeleton = OzzSkeleton.CreateFromBinaryContainer(skeletonContainer);

        if (!ozzSkeleton.IsValid())
        {
            skeletonContainer.Dispose();
            ozzSkeleton.Dispose();
            throw new InvalidOperationException("Failed to create OzzSkeleton from .ozzskel data");
        }

        var jointCount = ozzSkeleton.GetNumJoints();
        var joints = ExtractJoints(ozzSkeleton);

        return new Skeleton
        {
            Name = name ?? "skeleton",
            JointCount = jointCount,
            Joints = joints,
            RootJointIndices = [0],
            OzzSkeleton = ozzSkeleton,
            _skeletonData = skeletonContainer
        };
    }

    public Animation LoadAnimation(byte[] ozzAnimData, string? animName = null)
    {
        if (!string.IsNullOrEmpty(animName) && _animations.TryGetValue(animName, out var cached))
        {
            return cached;
        }

        var animContainer = CreateBinaryContainer(ozzAnimData);
        var context = OzzSkeleton.NewContext();

        if (!OzzSkeleton.LoadAnimationFromBinaryContainer(animContainer, context))
        {
            animContainer.Dispose();
            OzzSkeleton.DestroyContext(context);
            throw new InvalidOperationException($"Failed to load animation from .ozzanim data");
        }

        if (string.IsNullOrEmpty(animName))
        {
            var nameView = Ozz.GetAnimationName(context);
            animName = nameView.NumChars > 0 ? nameView.ToString() : "animation";

            if (_animations.TryGetValue(animName, out cached))
            {
                animContainer.Dispose();
                OzzSkeleton.DestroyContext(context);
                return cached;
            }
        }

        var duration = Ozz.GetAnimationDuration(context);
        var animation = new Animation(animName, duration, context, animContainer, this);
        _animations[animName] = animation;
        _animationNames = null;
        return animation;
    }

    public static BinaryContainer CreateBinaryContainer(byte[] data)
    {
        var container = new BinaryContainer();
        var writer = DenOfIz.BinaryWriter.CreateFromContainer(container);
        writer.Write(ByteArrayView.Create(data), 0, (uint)data.Length);
        writer.Dispose();
        return container;
    }

    public Animation GetAnimation(string animationName)
    {
        if (_animations.TryGetValue(animationName, out var cached))
        {
            return cached;
        }

        var names = AnimationNames;
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] == animationName)
            {
                return GetAnimation((uint)i);
            }
        }

        throw new InvalidOperationException($"Animation '{animationName}' not found in skeleton '{Name}'");
    }

    public Animation GetAnimation(uint animationIndex)
    {
        var names = AnimationNames;
        if (animationIndex >= names.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(animationIndex),
                $"Animation index {animationIndex} out of range (skeleton has {names.Count} animations)");
        }

        var animationName = names[(int)animationIndex];
        if (_animations.TryGetValue(animationName, out var cached))
        {
            return cached;
        }

        if (_sourceBytes == null)
        {
            throw new InvalidOperationException("Skeleton has been finalized, no new animations can be loaded");
        }

        using var exporter = new OzzExporterRuntime();
        if (!exporter.LoadFromMemory(_sourceBytes))
        {
            throw new InvalidOperationException($"Failed to reload glTF for animation: {animationName}");
        }

        var animationData = exporter.BuildAnimation(OzzSkeleton, animationIndex);
        if (animationData == null)
        {
            throw new InvalidOperationException($"Animation '{animationName}' (index {animationIndex}) failed to build for skeleton '{Name}'");
        }

        var animation = CreateAnimation(animationData, animationName);
        _animations[animationName] = animation;
        return animation;
    }

    private Animation CreateAnimation(BinaryContainer animationData, string name)
    {
        var context = OzzSkeleton.NewContext();

        if (!OzzSkeleton.LoadAnimationFromBinaryContainer(animationData, context))
        {
            animationData.Dispose();
            OzzSkeleton.DestroyContext(context);
            throw new InvalidOperationException($"Failed to load animation '{name}' for skeleton '{Name}'");
        }

        var duration = Ozz.GetAnimationDuration(context);

        return new Animation(name, duration, context, animationData, this);
    }

    public Animation LoadAnimationFromFile(string modelPath, string animationName)
    {
        var cacheKey = $"{modelPath}:{animationName}";
        if (_animations.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var bytes = Content.ReadBytes(modelPath);
        return LoadAnimationFromBytes(bytes, animationName, cacheKey);
    }

    public async Task<Animation> LoadAnimationFromFileAsync(string modelPath, string animationName, CancellationToken ct = default)
    {
        var cacheKey = $"{modelPath}:{animationName}";
        if (_animations.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var bytes = await Content.ReadBytesAsync(modelPath, ct);
        return LoadAnimationFromBytes(bytes, animationName, cacheKey);
    }

    private Animation LoadAnimationFromBytes(byte[] gltfBytes, string animationName, string cacheKey)
    {
        using var exporter = new OzzExporterRuntime();

        if (!exporter.LoadFromMemory(gltfBytes))
        {
            throw new InvalidOperationException($"Failed to load glTF for animation: {cacheKey}");
        }

        var animationData = exporter.BuildAnimation(OzzSkeleton, animationName);
        if (animationData == null)
        {
            throw new InvalidOperationException($"Animation '{animationName}' not found in file");
        }

        var animation = CreateAnimation(animationData, animationName);
        _animations[cacheKey] = animation;
        return animation;
    }

    public Matrix4x4[] ComputeRestPose()
    {
        var restPoses = OzzSkeleton.GetRestPoses().ToArray();
        using var localTransforms = OzzJointTransformArray.Create(restPoses);
        using var modelTransforms = Float4x4Array.Create(new Matrix4x4[JointCount]);

        OzzSkeleton.RunLocalToModelFromTRS(new LocalToModelFromTRSDesc
        {
            LocalTransforms = localTransforms,
            OutTransforms = modelTransforms
        });

        return modelTransforms.Value.AsSpan().ToArray();
    }

    private static List<Joint> ExtractJoints(OzzSkeleton ozzSkeleton)
    {
        var jointCount = ozzSkeleton.GetNumJoints();
        var joints = new List<Joint>(jointCount);
        var ozzJoints = ozzSkeleton.GetJoints().ToArray();
        var parents = ozzSkeleton.GetJointParents().ToArray();

        for (var i = 0; i < jointCount; i++)
        {
            var joint = new Joint
            {
                Index = i,
                ParentIndex = i < parents.Length ? parents[i] : -1,
                Name = i < ozzJoints.Length ? ozzJoints[i].Name.ToString() : $"Joint_{i}"
            };

            if (i < ozzJoints.Length)
            {
                var rp = ozzJoints[i].RestPose;
                var t = rp.Translation;
                var r = rp.Rotation;
                var s = rp.Scale;

                var scale = Matrix4x4.CreateScale(s.X, s.Y, s.Z);
                var rotation = Matrix4x4.CreateFromQuaternion(
                    new Quaternion(r.X, r.Y, r.Z, r.W));
                var translation = Matrix4x4.CreateTranslation(t.X, t.Y, t.Z);

                joint.LocalTransform = scale * rotation * translation;
            }

            joints.Add(joint);
        }

        return joints;
    }

    public void CopyInverseBindMatricesFrom(Skeleton source)
    {
        for (var i = 0; i < Math.Min(Joints.Count, source.Joints.Count); i++)
        {
            var sourceJoint = source.Joints.FirstOrDefault(j => j.Name == Joints[i].Name);
            if (sourceJoint != null)
            {
                Joints[i].InverseBindMatrix = sourceJoint.InverseBindMatrix;
            }
        }
    }

    public void Dispose()
    {
        foreach (var animation in _animations.Values)
        {
            animation.Dispose();
        }
        _animations.Clear();

        OzzSkeleton?.Dispose();
        _skeletonData?.Dispose();
        _exporter?.Dispose();
    }
}
