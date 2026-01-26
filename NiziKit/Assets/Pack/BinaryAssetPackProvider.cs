using System.Buffers.Binary;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace NiziKit.Assets.Pack;

internal sealed class BinaryAssetPackProvider : IAssetPackProvider
{
    public const uint MagicNumber = 0x4E5A504B;
    public const uint CurrentVersion = 1;
    private const int HeaderSize = 64;

    private readonly SafeFileHandle _fileHandle;
    private readonly string _basePath;
    private readonly Dictionary<string, (long offset, long size)> _index;
    private readonly long _dataOffset;

    public string BasePath => _basePath;

    public BinaryAssetPackProvider(string packPath)
    {
        _basePath = Path.GetDirectoryName(Path.GetFullPath(packPath)) ?? string.Empty;
        _fileHandle = File.OpenHandle(packPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _index = new Dictionary<string, (long, long)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            ReadHeader(out var entryCount, out var indexOffset, out var indexSize, out _dataOffset);
            ReadIndex(indexOffset, indexSize, entryCount);
        }
        catch
        {
            _fileHandle.Dispose();
            throw;
        }
    }

    private void ReadHeader(out uint entryCount, out long indexOffset, out long indexSize, out long dataOffset)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        RandomAccess.Read(_fileHandle, header, 0);

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (magic != MagicNumber)
        {
            throw new InvalidDataException($"Invalid NiziPack magic number: 0x{magic:X8}");
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4));
        if (version != CurrentVersion)
        {
            throw new InvalidDataException($"Unsupported NiziPack version: {version}");
        }

        entryCount = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(12));
        indexOffset = (long)BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(16));
        indexSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(24));
        dataOffset = (long)BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(32));
    }

    private void ReadIndex(long indexOffset, long indexSize, uint entryCount)
    {
        var indexBuffer = new byte[indexSize];
        RandomAccess.Read(_fileHandle, indexBuffer.AsSpan(), indexOffset);

        var offset = 0;
        for (var i = 0; i < entryCount; i++)
        {
            var pathLength = BinaryPrimitives.ReadUInt16LittleEndian(indexBuffer.AsSpan(offset));
            offset += 2;

            var path = Encoding.UTF8.GetString(indexBuffer, offset, pathLength);
            offset += pathLength;

            var dataOffsetRelative = (long)BinaryPrimitives.ReadUInt64LittleEndian(indexBuffer.AsSpan(offset));
            offset += 8;

            var fileSize = (long)BinaryPrimitives.ReadUInt64LittleEndian(indexBuffer.AsSpan(offset));
            offset += 8;

            _index[NormalizePath(path)] = (dataOffsetRelative, fileSize);
        }
    }

    public byte[] ReadBytes(string path)
    {
        var (offset, size) = GetEntry(path);
        var buffer = new byte[size];
        RandomAccess.Read(_fileHandle, buffer.AsSpan(), _dataOffset + offset);
        return buffer;
    }

    public async Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default)
    {
        var (offset, size) = GetEntry(path);
        var buffer = new byte[size];
        await RandomAccess.ReadAsync(_fileHandle, buffer.AsMemory(), _dataOffset + offset, ct);
        return buffer;
    }

    public string ReadText(string path)
    {
        var bytes = ReadBytes(path);
        return BytesToString(bytes);
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken ct = default)
    {
        var bytes = await ReadBytesAsync(path, ct);
        return BytesToString(bytes);
    }

    private static string BytesToString(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        return Encoding.UTF8.GetString(bytes);
    }

    public bool Exists(string path)
    {
        return _index.ContainsKey(NormalizePath(path));
    }

    private (long offset, long size) GetEntry(string path)
    {
        var normalized = NormalizePath(path);
        if (!_index.TryGetValue(normalized, out var entry))
        {
            throw new FileNotFoundException($"Entry not found in pack: {path}");
        }
        return entry;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    public IEnumerable<string> GetFilePaths() => _index.Keys;

    public void Dispose()
    {
        _fileHandle.Dispose();
    }
}
