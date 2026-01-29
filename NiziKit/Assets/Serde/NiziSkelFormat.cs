using System.Text;

namespace NiziKit.Assets.Serde;

public static class NiziSkelWriter
{
    private static readonly byte[] Magic = "NZSK"u8.ToArray();
    private const uint FormatVersion = 1;

    public static void Write(Stream stream, string name, byte[] ozzSkeletonData)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Magic);
        writer.Write(FormatVersion);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.Write((ushort)nameBytes.Length);
        writer.Write(nameBytes);

        writer.Write((uint)ozzSkeletonData.Length);
        writer.Write(ozzSkeletonData);
    }

    public static byte[] WriteToBytes(string name, byte[] ozzSkeletonData)
    {
        using var ms = new MemoryStream();
        Write(ms, name, ozzSkeletonData);
        return ms.ToArray();
    }
}

public static class NiziSkelReader
{
    private static readonly byte[] ExpectedMagic = "NZSK"u8.ToArray();

    public static (string name, byte[] ozzData) Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(ExpectedMagic))
        {
            throw new InvalidDataException("Invalid .niziskel file: bad magic");
        }

        var version = reader.ReadUInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported .niziskel version: {version}");
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
