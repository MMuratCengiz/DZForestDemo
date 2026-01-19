using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Assets.Serde;

public sealed class TexturesJson
{
    [JsonPropertyName("albedo")]
    public string? Albedo { get; set; }

    [JsonPropertyName("normal")]
    public string? Normal { get; set; }

    [JsonPropertyName("metallic")]
    public string? Metallic { get; set; }

    [JsonPropertyName("roughness")]
    public string? Roughness { get; set; }
}

public sealed class MaterialJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("shader")]
    public string Shader { get; set; } = string.Empty;

    [JsonPropertyName("variants")]
    public Dictionary<string, string>? Variants { get; set; }

    [JsonPropertyName("textures")]
    public TexturesJson Textures { get; set; } = new();

    public static MaterialJson FromJson(string json)
        => JsonSerializer.Deserialize<MaterialJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize material JSON");

    public static async Task<MaterialJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<MaterialJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize material JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);

    public IReadOnlyDictionary<string, string?>? GetVariants()
    {
        if (Variants == null || Variants.Count == 0)
        {
            return null;
        }

        return Variants.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);
    }
}
