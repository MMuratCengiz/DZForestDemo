using DenOfIz.World.Light;

namespace DenOfIz.World;

public class Scene(string name = "Scene")
{
    public string Name { get; set; } = name;

    private readonly List<GameObject> _rootObjects = [];
    public IReadOnlyList<GameObject> RootObjects => _rootObjects;
    private readonly Dictionary<Type, List<GameObject>> _objectsByType = new();
    private readonly List<DirectionalLight> _directionalLights = [];
    private readonly List<PointLight> _pointLights = [];
    private readonly List<SpotLight> _spotLights = [];

    public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;
    public IReadOnlyList<PointLight> PointLights => _pointLights;
    public IReadOnlyList<SpotLight> SpotLights => _spotLights;
    
    public CameraObject? MainCamera { get; set; }

    public Action? OnLoad { get; set; }
    public Action? OnUnload { get; set; }

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
        obj.Scene?.Remove(obj);
        obj.Scene = this;
        _rootObjects.Add(obj);
        RegisterObjectByType(obj);
    }

    public void Remove(GameObject obj)
    {
        if (obj.Scene != this)
        {
            return;
        }

        _rootObjects.Remove(obj);
        UnregisterObjectByType(obj);
        obj.Scene = null;
    }

    public void Clear()
    {
        foreach (var obj in _rootObjects)
        {
            UnregisterObjectByType(obj);
            obj.Scene = null;
        }

        _rootObjects.Clear();
        _objectsByType.Clear();
        _directionalLights.Clear();
        _pointLights.Clear();
        _spotLights.Clear();
        MainCamera = null;
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

        switch (obj)
        {
            case DirectionalLight dl:
                _directionalLights.Add(dl);
                break;
            case PointLight pl:
                _pointLights.Add(pl);
                break;
            case SpotLight sl:
                _spotLights.Add(sl);
                break;
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

        switch (obj)
        {
            case DirectionalLight dl:
                _directionalLights.Remove(dl);
                break;
            case PointLight pl:
                _pointLights.Remove(pl);
                break;
            case SpotLight sl:
                _spotLights.Remove(sl);
                break;
        }
    }

    public IReadOnlyList<T> GetObjectsOfType<T>() where T : GameObject
    {
        if (_objectsByType.TryGetValue(typeof(T), out var list))
        {
            return list.Cast<T>().ToList();
        }

        return [];
    }

    public T? GetObjectOfType<T>() where T : GameObject
    {
        if (_objectsByType.TryGetValue(typeof(T), out var list) && list.Count > 0)
        {
            return (T)list[0];
        }

        return null;
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

    public bool HasObjectsOfType<T>() where T : GameObject
    {
        return _objectsByType.TryGetValue(typeof(T), out var list) && list.Count > 0;
    }

    public int CountObjectsOfType<T>() where T : GameObject
    {
        if (_objectsByType.TryGetValue(typeof(T), out var list))
        {
            return list.Count;
        }

        return 0;
    }
}
