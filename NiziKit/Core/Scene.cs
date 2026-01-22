using NiziKit.Components;

namespace NiziKit.Core;

public abstract class Scene(string name = "Scene") : IDisposable
{
    public string Name { get; set; } = name;
    public string? SourcePath { get; set; }
    protected World World => World.Instance;

    private readonly List<GameObject> _rootObjects = [];
    private readonly List<GameObject> _pendingDestroy = [];
    private readonly List<GameObject> _pendingAdd = [];
    private bool _isIterating;
    public IReadOnlyList<GameObject> RootObjects => _rootObjects;
    private readonly Dictionary<Type, List<GameObject>> _objectsByType = new();

    private readonly List<CameraComponent> _cameras = [];

    public abstract void Load();

    public virtual Task LoadAsync(CancellationToken ct = default)
    {
        Load();
        return Task.CompletedTask;
    }

    public void RegisterCamera(CameraComponent camera)
    {
        if (!_cameras.Contains(camera))
        {
            _cameras.Add(camera);
        }
    }

    public void UnregisterCamera(CameraComponent camera)
    {
        _cameras.Remove(camera);
    }

    public CameraComponent? GetActiveCamera()
    {
        CameraComponent? best = null;
        var bestPriority = int.MinValue;

        foreach (var cam in _cameras)
        {
            if (cam.IsActive && cam.Priority > bestPriority)
            {
                best = cam;
                bestPriority = cam.Priority;
            }
        }

        return best;
    }

    public IReadOnlyList<CameraComponent> GetAllCameras() => _cameras;

    internal void UpdateCameras(float deltaTime)
    {
        foreach (var cam in _cameras)
        {
            if (cam is CameraComponent cameraComponent && cameraComponent.Owner != null)
            {
                var freeFly = cameraComponent.Owner.GetComponent<FreeFlyController>();
                if (freeFly != null)
                {
                    freeFly.UpdateCamera(deltaTime);
                    continue;
                }

                var orbit = cameraComponent.Owner.GetComponent<OrbitController>();
                orbit?.UpdateCamera(deltaTime);
            }
        }
    }

    internal bool HandleCameraEvent(in DenOfIz.Event ev)
    {
        foreach (var cam in _cameras)
        {
            if (cam is CameraComponent cameraComponent && cameraComponent.Owner != null)
            {
                var freeFly = cameraComponent.Owner.GetComponent<FreeFlyController>();
                if (freeFly?.HandleEvent(in ev) == true)
                {
                    return true;
                }

                var orbit = cameraComponent.Owner.GetComponent<OrbitController>();
                if (orbit?.HandleEvent(in ev) == true)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal void OnCameraResize(uint width, uint height)
    {
        foreach (var cam in _cameras)
        {
            cam.SetAspectRatio(width, height);
        }
    }

    internal void ProcessGameObjectLifecycle()
    {
        _isIterating = true;
        foreach (var obj in _rootObjects)
        {
            ProcessInitializeRecursive(obj);
        }
        foreach (var obj in _rootObjects)
        {
            ProcessBeginRecursive(obj);
        }
        _isIterating = false;
        ProcessPendingChanges();
    }

    internal void UpdateGameObjects()
    {
        _isIterating = true;
        foreach (var obj in _rootObjects)
        {
            if (!obj.IsDestroying)
            {
                UpdateGameObjectRecursive(obj);
            }
        }
        _isIterating = false;
        ProcessPendingChanges();
    }

    private void ProcessPendingChanges()
    {
        foreach (var obj in _pendingAdd)
        {
            obj.Scene = this;
            World.OnGameObjectCreated(obj);
            _rootObjects.Add(obj);
            RegisterObjectByType(obj);
        }
        _pendingAdd.Clear();

        foreach (var obj in _pendingDestroy)
        {
            World.OnGameObjectDestroyed(obj);
            _rootObjects.Remove(obj);
            UnregisterObjectByType(obj);
        }
        _pendingDestroy.Clear();
    }

    internal void PostUpdateGameObjects()
    {
        foreach (var obj in _rootObjects)
        {
            PostUpdateGameObjectRecursive(obj);
        }
    }

    internal void PhysicsUpdateGameObjects()
    {
        foreach (var obj in _rootObjects)
        {
            PhysicsUpdateGameObjectRecursive(obj);
        }
    }

    private static void ProcessInitializeRecursive(GameObject obj)
    {
        if (!obj.HasInitialized)
        {
            obj.HasInitialized = true;
            foreach (var component in obj.Components)
            {
                component.Initialize();
            }
        }
        foreach (var child in obj.Children)
        {
            ProcessInitializeRecursive(child);
        }
    }

    private static void ProcessBeginRecursive(GameObject obj)
    {
        if (!obj.IsActive)
        {
            return;
        }
        if (!obj.HasBegun)
        {
            obj.HasBegun = true;
            foreach (var component in obj.Components)
            {
                component.Begin();
            }
        }
        foreach (var child in obj.Children)
        {
            ProcessBeginRecursive(child);
        }
    }

    private static void UpdateGameObjectRecursive(GameObject obj)
    {
        if (!obj.IsActive)
        {
            return;
        }
        foreach (var component in obj.Components)
        {
            component.Update();
        }
        foreach (var child in obj.Children)
        {
            UpdateGameObjectRecursive(child);
        }
    }

    private static void PostUpdateGameObjectRecursive(GameObject obj)
    {
        if (!obj.IsActive)
        {
            return;
        }
        foreach (var component in obj.Components)
        {
            component.PostUpdate();
        }
        foreach (var child in obj.Children)
        {
            PostUpdateGameObjectRecursive(child);
        }
    }

    private static void PhysicsUpdateGameObjectRecursive(GameObject obj)
    {
        if (!obj.IsActive)
        {
            return;
        }
        foreach (var component in obj.Components)
        {
            component.PhysicsUpdate();
        }
        foreach (var child in obj.Children)
        {
            PhysicsUpdateGameObjectRecursive(child);
        }
    }

    public virtual void Dispose()
    {
        foreach (var obj in _rootObjects)
        {
            UnregisterObjectByType(obj);
        }
        _rootObjects.Clear();
        _objectsByType.Clear();
        _cameras.Clear();
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
        if (_isIterating)
        {
            _pendingAdd.Add(obj);
            return;
        }
        obj.Scene = this;
        World.OnGameObjectCreated(obj);
        _rootObjects.Add(obj);
        RegisterObjectByType(obj);
    }

    public void Destroy(GameObject obj)
    {
        obj.IsDestroying = true;
        if (_isIterating)
        {
            _pendingDestroy.Add(obj);
            return;
        }
        World.OnGameObjectDestroyed(obj);
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

    public T? FindComponent<T>() where T : class, IComponent
    {
        foreach (var obj in _rootObjects)
        {
            var component = FindComponentRecursive<T>(obj);
            if (component != null)
            {
                return component;
            }
        }
        return null;
    }

    public IEnumerable<T> FindComponents<T>() where T : class, IComponent
    {
        foreach (var obj in _rootObjects)
        {
            foreach (var component in FindComponentsRecursive<T>(obj))
            {
                yield return component;
            }
        }
    }

    private static T? FindComponentRecursive<T>(GameObject obj) where T : class, IComponent
    {
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            return component;
        }

        foreach (var child in obj.Children)
        {
            var found = FindComponentRecursive<T>(child);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private static IEnumerable<T> FindComponentsRecursive<T>(GameObject obj) where T : class, IComponent
    {
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            yield return component;
        }

        foreach (var child in obj.Children)
        {
            foreach (var found in FindComponentsRecursive<T>(child))
            {
                yield return found;
            }
        }
    }
}
