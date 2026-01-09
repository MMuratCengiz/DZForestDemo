using System.Reflection;
using System.Runtime.InteropServices;
using DenOfIz;
using BinaryReader = DenOfIz.BinaryReader;

namespace NiziKit.Assets;

public static class BuiltInShaders
{
    private static readonly Assembly Assembly = typeof(BuiltInShaders).Assembly;
    private const string ResourcePrefix = "NiziKit.BuiltInShaders.";

    /// <summary>
    /// Loads a built-in shader by name.
    /// </summary>
    /// <param name="name">The shader name without extension (e.g., "FullscreenQuad")</param>
    /// <returns>ShaderProgram, or null if the shader is not found</returns>
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
        var writer = DenOfIz.BinaryWriter.CreateFromContainer(container);
        writer.Write(ByteArrayView.Create(bytes), 0, (uint)bytes.Length);
        writer.Dispose();
        var readerDesc = new BinaryReaderDesc
        {
            NumBytes = 0
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
