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
    public List<string> Textures { get; set; } = [];

    [JsonPropertyName("shaders")]
    public List<string> Shaders { get; set; } = [];

    [JsonPropertyName("materials")]
    public List<string> Materials { get; set; } = [];

    [JsonPropertyName("models")]
    public List<string> Models { get; set; } = [];

    public static AssetPackJson FromJson(string json)
        => JsonSerializer.Deserialize<AssetPackJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public static async Task<AssetPackJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<AssetPackJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize asset pack JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);
}
