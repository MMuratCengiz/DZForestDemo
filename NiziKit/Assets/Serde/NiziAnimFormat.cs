using System.Text;

namespace NiziKit.Assets.Serde;

public static class NiziAnimWriter
{
    private static readonly byte[] Magic = "NZAN"u8.ToArray();
    private const uint FormatVersion = 1;

    public static void Write(Stream stream, string name, byte[] ozzAnimationData)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Magic);
        writer.Write(FormatVersion);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.Write((ushort)nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write((uint)ozzAnimationData.Length);
        writer.Write(ozzAnimationData);
    }

    public static byte[] WriteToBytes(string name, byte[] ozzAnimationData)
    {
        using var ms = new MemoryStream();
        Write(ms, name, ozzAnimationData);
        return ms.ToArray();
    }
}

public static class NiziAnimReader
{
    private static readonly byte[] ExpectedMagic = "NZAN"u8.ToArray();

    public static (string name, byte[] ozzData) Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(ExpectedMagic))
        {
            throw new InvalidDataException("Invalid .nizianim file: bad magic");
        }

        var version = reader.ReadUInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported .nizianim version: {version}");
        }

        var nameLen = reader.ReadUInt16();
        var nameBytes = reader.ReadBytes(nameLen);
        var name = Encoding.UTF8.GetString(nameBytes);

        var dataLen = reader.ReadUInt32();
        var ozzData = reader.ReadBytes((int)dataLen);

        return (name, ozzData);
    }

    public static (string name, byte[] ozzData) ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return Read(ms);
    }
}
