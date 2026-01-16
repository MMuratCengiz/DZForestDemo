namespace NiziKit.ContentPIpeline;

public sealed class HttpContentProvider(HttpClient http, string baseUrl = "Assets/") : IContentProvider
{
    private readonly string _baseUrl = baseUrl.TrimEnd('/') + '/';

    public async ValueTask<Stream> OpenAsync(string path, CancellationToken ct = default)
    {
        var url = ResolveUrl(path);
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async ValueTask<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var url = ResolveUrl(path);
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        return response.IsSuccessStatusCode;
    }

    public async ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        var url = ResolveUrl(path);
        return await http.GetByteArrayAsync(url, ct);
    }

    public async ValueTask<string> ReadAllTextAsync(string path, CancellationToken ct = default)
    {
        var url = ResolveUrl(path);
        return await http.GetStringAsync(url, ct);
    }

    private string ResolveUrl(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        return _baseUrl + normalized;
    }
}
