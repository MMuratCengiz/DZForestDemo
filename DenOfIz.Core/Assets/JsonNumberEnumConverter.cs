using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DenOfIz.World.Assets;

internal sealed class JsonNumberEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            return Unsafe.As<int, T>(ref value);
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            return default;
        }

        var str = reader.GetString();
        return Enum.TryParse<T>(str, true, out var result) ? result : default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var intValue = Unsafe.As<T, int>(ref value);
        writer.WriteNumberValue(intValue);
    }
}