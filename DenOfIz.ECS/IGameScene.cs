using DenOfIz;

namespace ECS;

public interface IGameScene : IDisposable
{
    string Name { get; }

    Scene Scene { get; }

    void OnRegister(World world, Scene scene);

    void OnEnter();

    void OnExit();

    void OnUpdate(float deltaTime);

    bool OnEvent(ref Event ev);

    void OnRender();
}

public abstract class GameSceneBase : IGameScene
{
    private bool _disposed;

    public abstract string Name { get; }

    public Scene Scene { get; private set; } = null!;

    protected World World { get; private set; } = null!;

    public virtual void OnRegister(World world, Scene scene)
    {
        World = world;
        Scene = scene;
    }

    public virtual void OnEnter() { }

    public virtual void OnExit() { }

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
