namespace DenOfIz.World;

public class Scene(string name = "Scene")
{
    public string Name { get; set; } = name;

    private readonly List<SceneObject> _rootObjects = new();
    public IReadOnlyList<SceneObject> RootObjects => _rootObjects;

    public Action? OnLoad { get; set; }
    public Action? OnUnload { get; set; }

    public SceneObject CreateObject(string name = "SceneObject")
    {
        var obj = new SceneObject(name);
        Add(obj);
        return obj;
    }

    public void Add(SceneObject obj)
    {
        obj.Scene?.Remove(obj);
        obj.Scene = this;
        _rootObjects.Add(obj);
    }

    public void Remove(SceneObject obj)
    {
        if (obj.Scene != this)
        {
            return;
        }

        _rootObjects.Remove(obj);
        obj.Scene = null;
    }

    public void Clear()
    {
        foreach (var obj in _rootObjects)
        {
            obj.Scene = null;
        }
        _rootObjects.Clear();
    }
}
