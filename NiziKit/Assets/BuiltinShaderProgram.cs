using System.Reflection;
using DenOfIz;
using BinaryReader = DenOfIz.BinaryReader;

namespace NiziKit.Assets;

public static class BuiltinShaderProgram
{
    private static readonly Assembly Assembly = typeof(BuiltinShaderProgram).Assembly;
    private const string ResourcePrefix = "NiziKit.Graphics.BuiltInShaders.";

    public static ShaderProgram? Load(string name)
    {
        var bytes = LoadResourceBytes($"{ResourcePrefix}{name}");
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
