using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Assets.Serde;

public static class NiziJsonSerializationOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new CompactFloatArrayConverter(),
            new CompactIntArrayConverter()
        }
    };
}

public class CompactFloatArrayConverter : JsonConverter<float[]>
{
    public override float[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<float>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(reader.GetSingle());
        }
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, float[] value, JsonSerializerOptions options)
    {
        var formatted = string.Join(", ", value.Select(f => f.ToString("G", CultureInfo.InvariantCulture)));
        writer.WriteRawValue("[" + formatted + "]");
    }
}

public class CompactIntArrayConverter : JsonConverter<int[]>
{
    public override int[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<int>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(reader.GetInt32());
        }
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, int[] value, JsonSerializerOptions options)
    {
        var formatted = string.Join(", ", value.Select(i => i.ToString(CultureInfo.InvariantCulture)));
        writer.WriteRawValue("[" + formatted + "]");
    }
}
