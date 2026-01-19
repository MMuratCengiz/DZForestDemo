using System.Numerics;
using System.Text.Json;

namespace NiziKit.Core;

/// <summary>
/// Extension methods for parsing JsonElement values with defaults.
/// </summary>
public static class JsonElementExtensions
{
    public static string GetStringOrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, string defaultValue = "")
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? defaultValue : defaultValue;
    }

    public static int GetInt32OrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, int defaultValue = 0)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : defaultValue;
    }

    public static float GetSingleOrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, float defaultValue = 0f)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind == JsonValueKind.Number ? element.GetSingle() : defaultValue;
    }

    public static double GetDoubleOrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, double defaultValue = 0.0)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : defaultValue;
    }

    public static bool GetBooleanOrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, bool defaultValue = false)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind is JsonValueKind.True or JsonValueKind.False && element.GetBoolean();
    }

    public static uint GetUInt32OrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, uint defaultValue = 0)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        return element.ValueKind == JsonValueKind.Number ? element.GetUInt32() : defaultValue;
    }

    public static float[]? GetFloatArrayOrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key)
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var result = new float[element.GetArrayLength()];
        var i = 0;
        foreach (var item in element.EnumerateArray())
        {
            result[i++] = item.GetSingle();
        }
        return result;
    }

    public static Vector3 GetVector3OrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, Vector3 defaultValue = default)
    {
        var arr = properties.GetFloatArrayOrDefault(key);
        if (arr == null || arr.Length < 3)
        {
            return defaultValue;
        }

        return new Vector3(arr[0], arr[1], arr[2]);
    }

    public static Vector4 GetVector4OrDefault(this IReadOnlyDictionary<string, JsonElement>? properties, string key, Vector4 defaultValue = default)
    {
        var arr = properties.GetFloatArrayOrDefault(key);
        if (arr == null || arr.Length < 4)
        {
            return defaultValue;
        }

        return new Vector4(arr[0], arr[1], arr[2], arr[3]);
    }

    public static T GetEnumOrDefault<T>(this IReadOnlyDictionary<string, JsonElement>? properties, string key, T defaultValue = default) where T : struct, Enum
    {
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return defaultValue;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var str = element.GetString();
            if (!string.IsNullOrEmpty(str) && Enum.TryParse<T>(str, true, out var result))
            {
                return result;
            }
        }
        else if (element.ValueKind == JsonValueKind.Number)
        {
            return (T)Enum.ToObject(typeof(T), element.GetInt32());
        }

        return defaultValue;
    }

    public static bool TryGetValue<T>(this IReadOnlyDictionary<string, JsonElement>? properties, string key, out T? value) where T : class
    {
        value = default;
        if (properties == null || !properties.TryGetValue(key, out var element))
        {
            return false;
        }

        try
        {
            value = element.Deserialize<T>();
            return value != null;
        }
        catch
        {
            return false;
        }
    }
}
