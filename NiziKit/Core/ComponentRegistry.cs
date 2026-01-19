using System.Text.Json;
using NiziKit.Components;

namespace NiziKit.Core;

/// <summary>
/// Registry for component factories. Components register their factories here
/// to enable JSON-based scene loading.
/// </summary>
public static class ComponentRegistry
{
    private static readonly Dictionary<string, IComponentFactory> _factories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a component factory.
    /// </summary>
    public static void Register(IComponentFactory factory)
    {
        _factories[factory.TypeName] = factory;
    }

    /// <summary>
    /// Registers a component factory with a custom type name.
    /// </summary>
    public static void Register(string typeName, IComponentFactory factory)
    {
        _factories[typeName] = factory;
    }

    /// <summary>
    /// Registers a simple factory using a delegate.
    /// </summary>
    public static void Register(string typeName, Func<IReadOnlyDictionary<string, JsonElement>?, IAssetResolver, IComponent> factory)
    {
        _factories[typeName] = new DelegateComponentFactory(typeName, factory);
    }

    /// <summary>
    /// Creates a component from a type name and properties.
    /// </summary>
    public static IComponent? Create(string typeName, IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)
    {
        if (_factories.TryGetValue(typeName, out var factory))
        {
            return factory.Create(properties, resolver);
        }
        return null;
    }

    /// <summary>
    /// Tries to create a component from a type name and properties.
    /// </summary>
    public static bool TryCreate(string typeName, IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver, out IComponent? component)
    {
        if (_factories.TryGetValue(typeName, out var factory))
        {
            component = factory.Create(properties, resolver);
            return true;
        }
        component = null;
        return false;
    }

    /// <summary>
    /// Checks if a component type is registered.
    /// </summary>
    public static bool IsRegistered(string typeName) => _factories.ContainsKey(typeName);

    /// <summary>
    /// Gets all registered type names.
    /// </summary>
    public static IEnumerable<string> GetRegisteredTypes() => _factories.Keys;

    /// <summary>
    /// Unregisters a component factory.
    /// </summary>
    public static void Unregister(string typeName) => _factories.Remove(typeName);

    /// <summary>
    /// Clears all registered factories.
    /// </summary>
    public static void Clear() => _factories.Clear();

    private sealed class DelegateComponentFactory(string typeName, Func<IReadOnlyDictionary<string, JsonElement>?, IAssetResolver, IComponent> factory) : IComponentFactory
    {
        public string TypeName => typeName;

        public IComponent Create(IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)
            => factory(properties, resolver);
    }
}
