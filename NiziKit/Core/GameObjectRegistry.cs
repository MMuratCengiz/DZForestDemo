using System.Reflection;
using System.Text.Json;
using NiziKit.Components;

namespace NiziKit.Core;

public static class GameObjectRegistry
{
    private static readonly Dictionary<string, GameObjectTypeInfo> _typeCache = new();
    private static readonly Dictionary<string, Func<JsonElement?, GameObject>> _customFactories = new();
    private static bool _initialized;
    private static readonly Lock _lock = new();

    static GameObjectRegistry()
    {
        Initialize();
    }

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                ScanAssembly(assembly);
            }

            _initialized = true;
        }
    }

    private static void OnAssemblyLoaded(object? sender, AssemblyLoadEventArgs args)
    {
        ScanAssembly(args.LoadedAssembly);
    }

    private static void ScanAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null ||
            name.StartsWith("System", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft", StringComparison.Ordinal) ||
            name.StartsWith("netstandard", StringComparison.Ordinal) ||
            name.StartsWith("mscorlib", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (!typeof(GameObject).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                var typeInfo = CreateTypeInfo(type);

                lock (_lock)
                {
                    _typeCache[type.FullName ?? type.Name] = typeInfo;
                }
            }
        }
        catch (ReflectionTypeLoadException)
        {
        }
    }

    private static GameObjectTypeInfo CreateTypeInfo(Type type)
    {
        var properties = new List<PropertyMapping>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonPropAttr != null)
            {
                properties.Add(new PropertyMapping(jsonPropAttr.Name, prop));
            }
        }

        return new GameObjectTypeInfo(type, properties);
    }

    public static void Register(string typeName, Func<JsonElement?, GameObject> factory)
    {
        lock (_lock)
        {
            _customFactories[typeName] = factory;
        }
    }

    public static void Register<T>(string typeName) where T : GameObject, new()
    {
        Register(typeName, _ => new T());
    }

    public static void Register(string typeName, Func<GameObject> factory)
    {
        Register(typeName, _ => factory());
    }

    public static GameObject Create(string typeName, JsonElement? properties = null)
    {
        if (!TryCreate(typeName, out var gameObject, properties))
        {
            throw new InvalidOperationException($"Unknown GameObject type: {typeName}");
        }
        return gameObject!;
    }

    public static bool TryCreate(string typeName, out GameObject? gameObject, JsonElement? properties = null)
    {
        lock (_lock)
        {
            if (_customFactories.TryGetValue(typeName, out var factory))
            {
                gameObject = factory(properties);
                return true;
            }
        }

        GameObjectTypeInfo? typeInfo;
        lock (_lock)
        {
            if (!_typeCache.TryGetValue(typeName, out typeInfo))
            {
                typeInfo = _typeCache.Values.FirstOrDefault(t =>
                    t.Type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (typeInfo == null)
        {
            gameObject = null;
            return false;
        }

        gameObject = CreateFromTypeInfo(typeInfo, properties);
        return true;
    }

    private static GameObject CreateFromTypeInfo(GameObjectTypeInfo typeInfo, JsonElement? properties)
    {
        var instance = (GameObject)Activator.CreateInstance(typeInfo.Type)!;

        if (properties.HasValue && typeInfo.Properties.Count > 0)
        {
            foreach (var mapping in typeInfo.Properties)
            {
                if (properties.Value.TryGetProperty(mapping.JsonName, out var value))
                {
                    SetPropertyValue(instance, mapping.Property, value);
                }
            }
        }

        return instance;
    }

    private static void SetPropertyValue(object instance, PropertyInfo property, JsonElement value)
    {
        var targetType = property.PropertyType;

        try
        {
            object? convertedValue = ConvertJsonValue(value, targetType);
            property.SetValue(instance, convertedValue);
        }
        catch (Exception)
        {
        }
    }

    private static object? ConvertJsonValue(JsonElement value, Type targetType)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            targetType = underlyingType;
        }

        return targetType switch
        {
            _ when targetType == typeof(string) => value.GetString(),
            _ when targetType == typeof(int) => value.GetInt32(),
            _ when targetType == typeof(float) => value.GetSingle(),
            _ when targetType == typeof(double) => value.GetDouble(),
            _ when targetType == typeof(bool) => value.GetBoolean(),
            _ when targetType == typeof(long) => value.GetInt64(),
            _ when targetType == typeof(uint) => value.GetUInt32(),
            _ when targetType == typeof(ulong) => value.GetUInt64(),
            _ when targetType == typeof(short) => value.GetInt16(),
            _ when targetType == typeof(ushort) => value.GetUInt16(),
            _ when targetType == typeof(byte) => value.GetByte(),
            _ when targetType == typeof(sbyte) => value.GetSByte(),
            _ when targetType == typeof(decimal) => value.GetDecimal(),
            _ when targetType.IsEnum => Enum.Parse(targetType, value.GetString() ?? ""),
            _ => JsonSerializer.Deserialize(value.GetRawText(), targetType)
        };
    }

    public static bool IsRegistered(string typeName)
    {
        lock (_lock)
        {
            return _customFactories.ContainsKey(typeName) || _typeCache.ContainsKey(typeName);
        }
    }

    public static void Unregister(string typeName)
    {
        lock (_lock)
        {
            _customFactories.Remove(typeName);
            _typeCache.Remove(typeName);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _customFactories.Clear();
            _typeCache.Clear();
        }
    }

    public static IEnumerable<string> GetRegisteredTypes()
    {
        lock (_lock)
        {
            return _customFactories.Keys.Concat(_typeCache.Keys).Distinct().ToList();
        }
    }

    private sealed class GameObjectTypeInfo(Type type, List<PropertyMapping> properties)
    {
        public Type Type { get; } = type;
        public List<PropertyMapping> Properties { get; } = properties;
    }

    private sealed class PropertyMapping(string jsonName, PropertyInfo property)
    {
        public string JsonName { get; } = jsonName;
        public PropertyInfo Property { get; } = property;
    }
}
