using DenOfIz.World.Physics;

namespace DenOfIz.World.SceneManagement;

public class World : IDisposable
{
    private readonly IWorldEventListener[] _worldEventListeners;

    public Scene? CurrentScene { get; private set; }
    public PhysicsWorld? PhysicsWorld { get; set; }
    public Assets.Assets Assets { get; }

    public World(LogicalDevice device)
    {
        Assets = new Assets.Assets(device);
        PhysicsWorld = new PhysicsWorld();

        _worldEventListeners = new IWorldEventListener[3];
        _worldEventListeners[0] = PhysicsWorld;
    }

    public void LoadScene(Scene scene)
    {
        scene.Assets = Assets;
        scene.Load();
        CurrentScene?.Dispose();
        CurrentScene = scene;
        foreach (var listener in _worldEventListeners)
        {
            listener?.SceneReset();
        }
    }

    public void GameObjectCreated(GameObject go)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener?.GameObjectCreated(go);
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener?.GameObjectDestroyed(go);
        }
    }

    public void Dispose()
    {
        CurrentScene?.Dispose();
        PhysicsWorld?.Dispose();
        Assets.Dispose();
    }
}
