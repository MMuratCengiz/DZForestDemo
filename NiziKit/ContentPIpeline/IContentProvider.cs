namespace NiziKit.ContentPipeline;

public interface IContentProvider
{
    ValueTask<Stream> OpenAsync(string path, CancellationToken ct = default);
    ValueTask<bool> ExistsAsync(string path, CancellationToken ct = default);
    ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);
    ValueTask<string> ReadAllTextAsync(string path, CancellationToken ct = default);
}
