namespace NiziKit.AssetPacks;

internal sealed class FolderAssetPackProvider : IAssetPackProvider
{
    private readonly string _rootPath;

    public FolderAssetPackProvider(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    public string ReadText(string path) => File.ReadAllText(ResolvePath(path));

    public byte[] ReadBytes(string path) => File.ReadAllBytes(ResolvePath(path));

    public Task<string> ReadTextAsync(string path, CancellationToken ct)
        => File.ReadAllTextAsync(ResolvePath(path), ct);

    public Task<byte[]> ReadBytesAsync(string path, CancellationToken ct)
        => File.ReadAllBytesAsync(ResolvePath(path), ct);

    public bool Exists(string path) => File.Exists(ResolvePath(path));

    private string ResolvePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return Path.Combine(_rootPath, normalized);
    }

    public void Dispose()
    {
    }
}
