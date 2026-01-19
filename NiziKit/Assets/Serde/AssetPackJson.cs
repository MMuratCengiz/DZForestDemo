using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Assets.Serde;

public sealed class AssetPackJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("textures")]
    public Dictionary<string, string> Textures { get; set; } = new();

    [JsonPropertyName("materials")]
    public Dictionary<string, string> Materials { get; set; } = new();

    [JsonPropertyName("models")]
    public Dictionary<string, string> Models { get; set; } = new();

    public static AssetPackJson FromJson(string json)
        => JsonSerializer.Deserialize<AssetPackJson>(json, AssetJsonDesc.Default)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public static async Task<AssetPackJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<AssetPackJson>(stream, AssetJsonDesc.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public string ToJson() => JsonSerializer.Serialize(this, AssetJsonDesc.Default);
}
