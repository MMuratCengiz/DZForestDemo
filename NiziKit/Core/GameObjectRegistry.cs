using System.Text.Json;

namespace NiziKit.Core;

public static class GameObjectRegistry
{
    private static readonly Dictionary<string, Func<JsonElement?, GameObject>> _factories = new();

    static GameObjectRegistry()
    {
        Register("GameObject", _ => new GameObject());
    }

    public static void Register<T>(string typeName) where T : GameObject, new()
    {
        _factories[typeName] = _ => new T();
    }

    public static void Register(string typeName, Func<JsonElement?, GameObject> factory)
    {
        _factories[typeName] = factory;
    }

    public static void Register(string typeName, Func<GameObject> factory)
    {
        _factories[typeName] = _ => factory();
    }

    public static GameObject Create(string typeName, JsonElement? properties = null)
    {
        if (!_factories.TryGetValue(typeName, out var factory))
        {
            throw new InvalidOperationException($"Unknown GameObject type: {typeName}");
        }
        return factory(properties);
    }

    public static bool TryCreate(string typeName, out GameObject? gameObject, JsonElement? properties = null)
    {
        if (_factories.TryGetValue(typeName, out var factory))
        {
            gameObject = factory(properties);
            return true;
        }
        gameObject = null;
        return false;
    }

    public static bool IsRegistered(string typeName) => _factories.ContainsKey(typeName);

    public static void Unregister(string typeName) => _factories.Remove(typeName);

    public static void Clear()
    {
        _factories.Clear();
        Register("GameObject", _ => new GameObject());
    }
}
