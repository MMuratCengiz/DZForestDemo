using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Graphics;
using NiziKit.Physics;

namespace NiziKit.Core;

public class World : IDisposable
{
    private static World? _instance;
    public static World Instance => _instance ?? throw new InvalidOperationException("World not initialized");

    public static Scene? CurrentScene => Instance._currentScene;
    public static PhysicsWorld PhysicsWorld => Instance._physicsWorld;
    public static RenderWorld RenderWorld => Instance._renderWorld;
    public static AssetWorld AssetWorld => Instance._assetWorld;
    public static AnimationWorld AnimationWorld => Instance._animationWorld;

    public static void LoadScene(Scene scene) => Instance._LoadScene(scene);

    public static void LoadScene(string scenePath)
    {
        if (scenePath.EndsWith(".niziscene.json", StringComparison.OrdinalIgnoreCase) ||
            scenePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            LoadScene(new JsonScene(scenePath));
        }
        else
        {
            throw new ArgumentException($"Unsupported scene format: {scenePath}. Expected .niziscene.json");
        }
    }

    public static T? FindObjectOfType<T>() where T : GameObject
    {
        return CurrentScene?.GetObjectsOfType<T>().FirstOrDefault();
    }

    public static IReadOnlyList<T> FindObjectsOfType<T>() where T : GameObject
    {
        return CurrentScene?.GetObjectsOfType<T>() ?? [];
    }

    public static GameObject? FindObjectWithTag(string tag)
    {
        if (CurrentScene == null) return null;
        foreach (var obj in CurrentScene.RootObjects)
        {
            var found = FindObjectWithTagRecursive(obj, tag);
            if (found != null) return found;
        }
        return null;
    }

    public static List<GameObject> FindObjectsWithTag(string tag)
    {
        var results = new List<GameObject>();
        if (CurrentScene == null) return results;
        foreach (var obj in CurrentScene.RootObjects)
        {
            FindObjectsWithTagRecursive(obj, tag, results);
        }
        return results;
    }

    private static GameObject? FindObjectWithTagRecursive(GameObject obj, string tag)
    {
        if (obj.Tag == tag) return obj;
        foreach (var child in obj.Children)
        {
            var found = FindObjectWithTagRecursive(child, tag);
            if (found != null) return found;
        }
        return null;
    }

    private static void FindObjectsWithTagRecursive(GameObject obj, string tag, List<GameObject> results)
    {
        if (obj.Tag == tag) results.Add(obj);
        foreach (var child in obj.Children)
        {
            FindObjectsWithTagRecursive(child, tag, results);
        }
    }

    internal static void OnGameObjectCreated(GameObject go) => Instance._OnGameObjectCreated(go);
    internal static void OnGameObjectDestroyed(GameObject go) => Instance._OnGameObjectDestroyed(go);
    internal static void OnComponentAdded(GameObject go, IComponent component) => Instance._OnComponentAdded(go, component);
    internal static void OnComponentRemoved(GameObject go, IComponent component) => Instance._OnComponentRemoved(go, component);
    internal static void OnComponentChanged(GameObject go, IComponent component) => Instance._OnComponentChanged(go, component);

    private readonly IWorldEventListener[] _worldEventListeners;

    private Scene? _currentScene;
    private readonly PhysicsWorld _physicsWorld;
    private readonly RenderWorld _renderWorld;
    private readonly AssetWorld _assetWorld;
    private readonly AnimationWorld _animationWorld;

    public World()
    {
        _physicsWorld = new PhysicsWorld();
        _renderWorld = new RenderWorld();
        _assetWorld = new AssetWorld();
        _animationWorld = new AnimationWorld();

        _worldEventListeners = [_assetWorld, _physicsWorld, _animationWorld, _renderWorld];

        _instance = this;
    }

    private void _LoadScene(Scene scene)
    {
        _currentScene?.Dispose();
        foreach (var listener in _worldEventListeners)
        {
            listener.SceneReset();
        }

        _currentScene = scene;
        scene.Load();
    }

    private void _OnGameObjectCreated(GameObject go)
    {
        go.IsInWorld = true;

        foreach (var child in go.Children)
        {
            _OnGameObjectCreated(child);
        }

        foreach (var listener in _worldEventListeners)
        {
            listener.GameObjectCreated(go);
        }
    }

    private void _OnGameObjectDestroyed(GameObject go)
    {
        foreach (var child in go.Children)
        {
            _OnGameObjectDestroyed(child);
        }

        foreach (var listener in _worldEventListeners)
        {
            listener.GameObjectDestroyed(go);
        }

        go.IsInWorld = false;
    }

    private void _OnComponentAdded(GameObject go, IComponent component)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.ComponentAdded(go, component);
        }
    }

    private void _OnComponentRemoved(GameObject go, IComponent component)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.ComponentRemoved(go, component);
        }
    }

    private void _OnComponentChanged(GameObject go, IComponent component)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.ComponentChanged(go, component);
        }
    }

    public void Dispose()
    {
        _currentScene?.Dispose();
        foreach (var listener in _worldEventListeners)
        {
            listener.SceneReset();
        }

        _physicsWorld.Dispose();
    }
}