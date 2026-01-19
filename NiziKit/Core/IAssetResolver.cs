using NiziKit.Assets;

namespace NiziKit.Core;

/// <summary>
/// Interface for resolving assets from references in JSON scene files.
/// </summary>
public interface IAssetResolver
{
    /// <summary>
    /// Resolves a mesh from a reference string.
    /// Supports formats: "geometry:box", "file:path", "packName:assetName"
    /// </summary>
    Mesh? ResolveMesh(string reference, IReadOnlyDictionary<string, object>? parameters = null);

    /// <summary>
    /// Resolves a material from a reference string.
    /// Supports formats: "builtin:name", "file:path", "packName:assetName"
    /// </summary>
    Material? ResolveMaterial(string reference);

    /// <summary>
    /// Resolves a texture from a reference string.
    /// </summary>
    Texture2d? ResolveTexture(string reference);

    /// <summary>
    /// Resolves a shader program from a reference string.
    /// Supports formats: "builtin:name", "file:path", "packName:assetName"
    /// </summary>
    Graphics.GpuShader? ResolveShader(string reference);
}
