using NiziKit.Assets;

namespace NiziKit.Core;

public interface IAssetResolver
{
    Mesh? ResolveMesh(string reference, IReadOnlyDictionary<string, object>? parameters = null);

    Material? ResolveMaterial(string reference);

    Texture2d? ResolveTexture(string reference);

    Graphics.GpuShader? ResolveShader(string reference);

    Skeleton? ResolveSkeleton(string reference);

    Assets.Animation? ResolveAnimation(string reference);
}
