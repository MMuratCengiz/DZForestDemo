namespace NiziKit.AssetPacks;

internal interface IAssetPackProvider : IDisposable
{
    string ReadText(string path);
    byte[] ReadBytes(string path);
    Task<string> ReadTextAsync(string path, CancellationToken ct = default);
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);
    bool Exists(string path);
}
