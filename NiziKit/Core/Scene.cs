namespace NiziKit.Core;

public abstract class Scene(World world, Assets.Assets assets, string name = "Scene") : IDisposable
{
    public string Name { get; set; } = name;
    protected World World { get; } = world;
    public Assets.Assets Assets { get; internal set; } = assets;

    private readonly List<GameObject> _rootObjects = [];
    public IReadOnlyList<GameObject> RootObjects => _rootObjects;
    private readonly Dictionary<Type, List<GameObject>> _objectsByType = new();

    public CameraObject? MainCamera { get; set; }

    public abstract void Load();

    public virtual void Dispose()
    {
        foreach (var obj in _rootObjects)
        {
            UnregisterObjectByType(obj);
        }
        _rootObjects.Clear();
        _objectsByType.Clear();
        MainCamera = null;
    }

    public GameObject CreateObject(string name = "SceneObject")
    {
        var obj = new GameObject(name);
        Add(obj);
        return obj;
    }

    public T CreateObject<T>(string? name = null) where T : GameObject, new()
    {
        var obj = new T();
        if (name != null)
        {
            obj.Name = name;
        }

        Add(obj);
        return obj;
    }

    public void Add(GameObject obj)
    {
        World.GameObjectCreated(obj);
        _rootObjects.Add(obj);
        RegisterObjectByType(obj);
    }

    public T Add<T>(IPrefab<T> prefab) where T : GameObject
    {
        var obj = prefab.Instantiate();
        Add(obj);
        return (T)obj;
    }

    public void Destroy(GameObject obj)
    {
        World.GameObjectDestroyed(obj);
        _rootObjects.Remove(obj);
        UnregisterObjectByType(obj);
    }

    public void Clear()
    {
        foreach (var obj in _rootObjects)
        {
            UnregisterObjectByType(obj);
        }
        _rootObjects.Clear();
    }

    private void RegisterObjectByType(GameObject obj)
    {
        var type = obj.GetType();
        while (type != null && type != typeof(object))
        {
            if (!_objectsByType.TryGetValue(type, out var list))
            {
                list = [];
                _objectsByType[type] = list;
            }

            list.Add(obj);
            type = type.BaseType;
        }
    }

    private void UnregisterObjectByType(GameObject obj)
    {
        var type = obj.GetType();

        while (type != null && type != typeof(object))
        {
            if (_objectsByType.TryGetValue(type, out var list))
            {
                list.Remove(obj);
            }

            type = type.BaseType;
        }
    }

    public IReadOnlyList<T> GetObjectsOfType<T>() where T : GameObject
    {
        return _objectsByType.TryGetValue(typeof(T), out var list) ? list.Cast<T>().ToList() : [];
    }

    public IEnumerable<T> FindObjects<T>(Func<T, bool> predicate) where T : GameObject
    {
        if (!_objectsByType.TryGetValue(typeof(T), out var list))
        {
            yield break;
        }

        foreach (var obj in list)
        {
            if (obj is T typed && predicate(typed))
            {
                yield return typed;
            }
        }
    }
}
