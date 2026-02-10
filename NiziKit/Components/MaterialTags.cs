using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Components;

[JsonConverter(typeof(MaterialTagsJsonConverter))]
public class MaterialTags : IDictionary<string, string>, IReadOnlyDictionary<string, string>, IDictionary
{
    private static readonly HashSet<string> TruthyValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "true", "1", "yes", "on"
    };

    private static readonly HashSet<string> FalsyValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "false", "0", "no", "off"
    };

    private readonly Dictionary<string, string> _data = new(StringComparer.OrdinalIgnoreCase);

    public MaterialTags() { }

    public MaterialTags(IEnumerable<KeyValuePair<string, string>> source)
    {
        foreach (var kvp in source)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!_data.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (TruthyValues.Contains(value))
        {
            return true;
        }

        if (FalsyValues.Contains(value))
        {
            return false;
        }
        return true;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        if (_data.TryGetValue(key, out var value) && int.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (_data.TryGetValue(key, out var value) && float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        return _data.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public bool Has(string key) => _data.ContainsKey(key);

    public string this[string key]
    {
        get => _data[key];
        set => _data[key] = value;
    }

    public void Set(string key, string value) => _data[key] = value;
    public void Set(string key, bool value) => _data[key] = value ? "True" : "False";
    public void Set(string key, int value) => _data[key] = value.ToString();
    public void Set(string key, float value) => _data[key] = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public bool Remove(string key) => _data.Remove(key);

    public int Count => _data.Count;
    public IEnumerable<string> Keys => _data.Keys;
    public IEnumerable<string> Values => _data.Values;

    string IReadOnlyDictionary<string, string>.this[string key] => _data[key];

    public bool ContainsKey(string key) => _data.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _data.TryGetValue(key, out value!);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

    ICollection<string> IDictionary<string, string>.Keys => _data.Keys;
    ICollection<string> IDictionary<string, string>.Values => _data.Values;
    public void Add(string key, string value) => _data.Add(key, value);
    public void Add(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_data).Add(item);
    public void Clear() => _data.Clear();
    public bool Contains(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_data).Contains(item);
    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, string>>)_data).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, string> item) => ((ICollection<KeyValuePair<string, string>>)_data).Remove(item);
    bool ICollection<KeyValuePair<string, string>>.IsReadOnly => false;

    object? IDictionary.this[object key]
    {
        get => _data[(string)key];
        set => _data[(string)key] = (string)value!;
    }
    ICollection IDictionary.Keys => _data.Keys;
    ICollection IDictionary.Values => _data.Values;
    bool IDictionary.IsReadOnly => false;
    bool IDictionary.IsFixedSize => false;
    int ICollection.Count => _data.Count;
    object ICollection.SyncRoot => ((ICollection)_data).SyncRoot;
    bool ICollection.IsSynchronized => false;
    bool IDictionary.Contains(object key) => key is string s && _data.ContainsKey(s);
    void IDictionary.Add(object key, object? value) => _data.Add((string)key, (string)value!);
    void IDictionary.Remove(object key) => _data.Remove((string)key);
    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_data).GetEnumerator();
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_data).CopyTo(array, index);
}

public class MaterialTagsJsonConverter : JsonConverter<MaterialTags>
{
    public override MaterialTags Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
        return dict != null ? new MaterialTags(dict) : new MaterialTags();
    }

    public override void Write(Utf8JsonWriter writer, MaterialTags value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WriteString(kvp.Key, kvp.Value);
        }
        writer.WriteEndObject();
    }
}
