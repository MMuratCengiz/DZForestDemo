namespace NiziKit.ContentPipeline;

public sealed class FileContentProvider(string root) : IContentProvider
{
    private readonly string _root = Path.GetFullPath(root);

    public ValueTask<Stream> OpenAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        return ValueTask.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public ValueTask<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        return ValueTask.FromResult(File.Exists(fullPath));
    }

    public async ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public async ValueTask<string> ReadAllTextAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        return await File.ReadAllTextAsync(fullPath, ct);
    }

    public string GetFullPath(string path) => ResolvePath(path);

    private string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var normalized = path.Replace('\\', '/').TrimStart('/');
        return Path.Combine(_root, normalized);
    }
}
