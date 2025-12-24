using DenOfIz;

namespace ECS;

public interface IScene : IDisposable
{
    string Name { get; }

    SceneId SceneId { get; set; }

    void OnRegister(World world, Scene scene);

    void OnLoad();

    void OnUnload();

    void OnActivate();

    void OnDeactivate();

    void OnUpdate(float deltaTime);

    bool OnEvent(ref Event ev);

    void OnRender();
}

public abstract class SceneBase : IScene
{
    private bool _disposed;

    public abstract string Name { get; }

    public SceneId SceneId { get; set; }

    protected World World { get; private set; } = null!;

    protected Scene Scene { get; private set; } = null!;

    public virtual void OnRegister(World world, Scene scene)
    {
        World = world;
        Scene = scene;
        SceneId = scene.Id;
    }

    public virtual void OnLoad() { }

    public virtual void OnUnload() { }

    public virtual void OnActivate() { }

    public virtual void OnDeactivate() { }

    public virtual void OnUpdate(float deltaTime) { }

    public virtual bool OnEvent(ref Event ev) => false;

    public virtual void OnRender() { }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        OnDispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void OnDispose() { }
}
