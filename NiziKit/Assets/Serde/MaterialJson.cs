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
    public List<string>? Variants { get; set; }

    [JsonPropertyName("textures")]
    public TexturesJson Textures { get; set; } = new();

    [JsonPropertyName("parameters")]
    public Dictionary<string, JsonElement>? Parameters { get; set; }

    [JsonPropertyName("color")]
    public float[]? Color { get; set; }

    public static MaterialJson FromJson(string json)
        => JsonSerializer.Deserialize<MaterialJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize material JSON");

    public static async Task<MaterialJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<MaterialJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize material JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);

    public ReadOnlySpan<string> GetVariants()
    {
        if (Variants == null || Variants.Count == 0)
        {
            return new ReadOnlySpan<string>();
        }

        return Variants.ToArray();
    }
}
