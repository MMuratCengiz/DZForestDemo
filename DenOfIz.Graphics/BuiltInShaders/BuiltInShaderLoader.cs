using System.Reflection;
using System.Runtime.InteropServices;
using DenOfIz;
using BinaryReader = DenOfIz.BinaryReader;

namespace Graphics.BuiltInShaders;

/// <summary>
/// Loads pre-compiled shaders that are embedded as resources in DenOfIz.Graphics.
/// </summary>
public static class BuiltInShaderLoader
{
    private static readonly Assembly Assembly = typeof(BuiltInShaderLoader).Assembly;
    private const string ResourcePrefix = "Graphics.BuiltInShaders.";

    /// <summary>
    /// Loads a built-in shader by name.
    /// </summary>
    /// <param name="name">The shader name without extension (e.g., "FullscreenQuad")</param>
    /// <returns>A ShaderProgram ready to use, or null if the shader is not found</returns>
    public static ShaderProgram? Load(string name)
    {
        var resourceName = $"{ResourcePrefix}{name}.dzshader";
        var bytes = LoadResourceBytes(resourceName);

        if (bytes == null)
        {
            return null;
        }

        return LoadFromBytes(bytes);
    }

    /// <summary>
    /// Loads a built-in shader by name, throwing if not found.
    /// </summary>
    /// <param name="name">The shader name without extension (e.g., "FullscreenQuad")</param>
    /// <returns>A ShaderProgram ready to use</returns>
    /// <exception cref="FileNotFoundException">If the shader is not found</exception>
    public static ShaderProgram LoadRequired(string name)
    {
        var program = Load(name);
        if (program == null)
        {
            throw new FileNotFoundException($"Built-in shader not found: {name}");
        }

        return program;
    }

    /// <summary>
    /// Checks if a built-in shader exists.
    /// </summary>
    /// <param name="name">The shader name without extension</param>
    /// <returns>True if the shader exists</returns>
    public static bool Exists(string name)
    {
        var resourceName = $"{ResourcePrefix}{name}.dzshader";
        return Assembly.GetManifestResourceInfo(resourceName) != null;
    }

    /// <summary>
    /// Lists all available built-in shaders.
    /// </summary>
    /// <returns>Names of all embedded shaders (without extension)</returns>
    public static IEnumerable<string> ListAvailable()
    {
        return Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix) && n.EndsWith(".dzshader"))
            .Select(n => n[ResourcePrefix.Length..^".dzshader".Length]);
    }

    private static byte[]? LoadResourceBytes(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static ShaderProgram LoadFromBytes(byte[] bytes)
    {
        using var container = new BinaryContainer();
        var writer = BinaryWriter.CreateFromContainer(container);

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            writer.Write(handle.AddrOfPinnedObject(), (ulong)bytes.Length);
        }
        finally
        {
            handle.Free();
            writer.Dispose();
        }

        var readerDesc = new BinaryReaderDesc
        {
            NumBytes = 0 // Read everything
        };
        var reader = BinaryReader.CreateFromContainer(container, readerDesc);

        var assetReaderDesc = new ShaderAssetReaderDesc
        {
            Reader = reader
        };
        var assetReader = new ShaderAssetReader(assetReaderDesc);

        var program = ShaderProgram.CreateFromAsset(assetReader);

        assetReader.Dispose();
        reader.Dispose();

        return program;
    }
}
