using NiziKit.Assets;
using NiziKit.Graphics;
using NiziKit.Physics;

namespace NiziKit.Core;

public class World : IDisposable
{
    private readonly IWorldEventListener[] _worldEventListeners;

    public Scene? CurrentScene { get; private set; }
    public PhysicsWorld PhysicsWorld { get; }
    public RenderWorld RenderWorld { get; }
    public AssetWorld AssetWorld { get; }
    public Assets.Assets Assets { get; }

    public World(GraphicsContext context)
    {
        Assets = new Assets.Assets(context);
        PhysicsWorld = new PhysicsWorld();
        RenderWorld = new RenderWorld();
        AssetWorld = new AssetWorld(Assets);

        _worldEventListeners = [AssetWorld, PhysicsWorld, RenderWorld];
    }

    public void LoadScene(Scene scene)
    {
        scene.Assets = Assets;
        scene.Load();
        CurrentScene?.Dispose();
        CurrentScene = scene;
        foreach (var listener in _worldEventListeners)
        {
            listener.SceneReset();
        }
    }

    public void GameObjectCreated(GameObject go)
    {
        foreach (var child in go.Children)
        {
            GameObjectCreated(child);
        }
        
        foreach (var listener in _worldEventListeners)
        {
            listener.GameObjectCreated(go);
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
        foreach (var child in go.Children)
        {
            GameObjectDestroyed(child);            
        }

        foreach (var listener in _worldEventListeners)
        {
            listener.GameObjectDestroyed(go);
        }
    }

    public void Dispose()
    {
        CurrentScene?.Dispose();
        PhysicsWorld.Dispose();
        Assets.Dispose();
    }
}
