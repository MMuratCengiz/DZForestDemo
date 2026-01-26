using NiziKit.Assets;

namespace NiziKit.Core;

public interface IAssetResolver
{
    Mesh? ResolveMesh(string reference, IReadOnlyDictionary<string, object>? parameters = null);

    Texture2d? ResolveTexture(string reference);

    Graphics.GpuShader? ResolveShader(string reference);

    Skeleton? ResolveSkeleton(string reference);

    Assets.Animation? ResolveAnimation(string reference);
}
