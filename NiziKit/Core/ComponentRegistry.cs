using System.Reflection;
using System.Text.Json;
using NiziKit.Components;

namespace NiziKit.Core;

public static class ComponentRegistry
{
    private static readonly Dictionary<string, IComponentFactory> _factories = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ComponentTypeInfo> _typeCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;
    private static readonly Lock _lock = new();

    static ComponentRegistry()
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

                if (!typeof(IComponent).IsAssignableFrom(type))
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

    private static ComponentTypeInfo CreateTypeInfo(Type type)
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

        return new ComponentTypeInfo(type, properties);
    }

    public static void Register(IComponentFactory factory)
    {
        lock (_lock)
        {
            _factories[factory.TypeName] = factory;
        }
    }

    public static void Register(string typeName, IComponentFactory factory)
    {
        lock (_lock)
        {
            _factories[typeName] = factory;
        }
    }

    public static void Register(string typeName, Func<IReadOnlyDictionary<string, JsonElement>?, IAssetResolver, IComponent> factory)
    {
        lock (_lock)
        {
            _factories[typeName] = new DelegateComponentFactory(typeName, factory);
        }
    }

    public static IComponent? Create(string typeName, IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)
    {
        TryCreate(typeName, properties, resolver, out var component);
        return component;
    }

    public static bool TryCreate(string typeName, IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver, out IComponent? component)
    {
        lock (_lock)
        {
            if (_factories.TryGetValue(typeName, out var factory))
            {
                component = factory.Create(properties, resolver);
                return true;
            }
        }

        ComponentTypeInfo? typeInfo;
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
            component = null;
            return false;
        }

        component = CreateFromTypeInfo(typeInfo, properties, resolver);
        return true;
    }

    private static IComponent CreateFromTypeInfo(ComponentTypeInfo typeInfo, IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)
    {
        var instance = (IComponent)Activator.CreateInstance(typeInfo.Type)!;

        if (properties != null && typeInfo.Properties.Count > 0)
        {
            foreach (var mapping in typeInfo.Properties)
            {
                if (properties.TryGetValue(mapping.JsonName, out var value))
                {
                    SetPropertyValue(instance, mapping.Property, value, resolver);
                }
            }
        }

        return instance;
    }

    private static void SetPropertyValue(object instance, PropertyInfo property, JsonElement value, IAssetResolver resolver)
    {
        var targetType = property.PropertyType;

        try
        {
            var assetRefAttr = property.GetCustomAttribute<AssetRefAttribute>();
            if (assetRefAttr != null)
            {
                var reference = value.GetString();
                if (!string.IsNullOrEmpty(reference))
                {
                    object? asset = assetRefAttr.AssetType switch
                    {
                        AssetRefType.Mesh => resolver.ResolveMesh(reference),
                        AssetRefType.Material => resolver.ResolveMaterial(reference),
                        AssetRefType.Texture => resolver.ResolveTexture(reference),
                        AssetRefType.Shader => resolver.ResolveShader(reference),
                        _ => null
                    };
                    property.SetValue(instance, asset);
                }
                return;
            }

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
            return _factories.ContainsKey(typeName) || _typeCache.ContainsKey(typeName);
        }
    }

    public static IEnumerable<string> GetRegisteredTypes()
    {
        lock (_lock)
        {
            return _factories.Keys.Concat(_typeCache.Keys).Distinct().ToList();
        }
    }

    public static void Unregister(string typeName)
    {
        lock (_lock)
        {
            _factories.Remove(typeName);
            _typeCache.Remove(typeName);
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _factories.Clear();
            _typeCache.Clear();
        }
    }

    private sealed class DelegateComponentFactory(string typeName, Func<IReadOnlyDictionary<string, JsonElement>?, IAssetResolver, IComponent> factory) : IComponentFactory
    {
        public string TypeName => typeName;

        public IComponent Create(IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)
            => factory(properties, resolver);
    }

    private sealed class ComponentTypeInfo(Type type, List<PropertyMapping> properties)
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
