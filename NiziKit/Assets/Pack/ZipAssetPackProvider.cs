using System.IO.Compression;

namespace NiziKit.Assets.Pack;

internal sealed class ZipAssetPackProvider : IAssetPackProvider
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entryLookup;
    private readonly string _basePath;

    public ZipAssetPackProvider(string zipPath)
    {
        _archive = ZipFile.OpenRead(zipPath);
        _basePath = Path.GetDirectoryName(Path.GetFullPath(zipPath)) ?? string.Empty;
        _entryLookup = _archive.Entries.ToDictionary(
            e => NormalizePath(e.FullName),
            StringComparer.OrdinalIgnoreCase);
    }

    public string BasePath => _basePath;

    public string ReadText(string path)
    {
        using var stream = OpenEntry(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public byte[] ReadBytes(string path)
    {
        using var stream = OpenEntry(path);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public async Task<string> ReadTextAsync(string path, CancellationToken ct)
    {
        await using var stream = OpenEntry(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task<byte[]> ReadBytesAsync(string path, CancellationToken ct)
    {
        await using var stream = OpenEntry(path);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    public bool Exists(string path) => _entryLookup.ContainsKey(NormalizePath(path));

    private Stream OpenEntry(string path)
    {
        var normalized = NormalizePath(path);
        if (!_entryLookup.TryGetValue(normalized, out var entry))
        {
            throw new FileNotFoundException($"Entry not found in archive: {path}");
        }
        return entry.Open();
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    public void Dispose() => _archive.Dispose();
}
