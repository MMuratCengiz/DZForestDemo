namespace DenOfIz.World.Assets.Loaders;

internal static class SkeletonLoader
{
    public static Skeleton Load(string path)
    {
        var resolvedPath = AssetPaths.ResolveSkeleton(path);
        var ozzSkeleton = new OzzAnimation(StringView.Create(resolvedPath));

        if (!ozzSkeleton.IsValid())
        {
            ozzSkeleton.Dispose();
            throw new InvalidOperationException($"Failed to load skeleton: {path}");
        }

        var jointCount = ozzSkeleton.GetNumJoints();
        var joints = ExtractJointNames(ozzSkeleton);

        return new Skeleton
        {
            Name = Path.GetFileNameWithoutExtension(path),
            JointCount = jointCount,
            Joints = joints,
            RootJointIndices = [0],
            OzzSkeleton = ozzSkeleton
        };
    }

    private static List<Joint> ExtractJointNames(OzzAnimation ozzSkeleton)
    {
        var jointCount = ozzSkeleton.GetNumJoints();
        var joints = new List<Joint>(jointCount);
        var jointNames = ozzSkeleton.GetJointNames().ToArray();

        for (var i = 0; i < jointCount; i++)
        {
            var joint = new Joint
            {
                Index = i,
                ParentIndex = -1,
                Name = i < jointNames.Length ? jointNames[i].ToString() : $"Joint_{i}"
            };
            joints.Add(joint);
        }

        return joints;
    }
}
