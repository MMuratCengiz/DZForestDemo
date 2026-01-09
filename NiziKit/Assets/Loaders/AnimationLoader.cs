using DenOfIz;

namespace NiziKit.Assets.Loaders;

internal static class AnimationLoader
{
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
}
