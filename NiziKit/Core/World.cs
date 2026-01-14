using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Graphics;
using NiziKit.Physics;

namespace NiziKit.Core;

public class World : IWorldEventListener, IDisposable
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
        CurrentScene?.Dispose();
        foreach (var listener in _worldEventListeners)
        {
            listener.SceneReset();
        }

        CurrentScene = scene;
        scene.Assets = Assets;
        scene.Load();
    }

    public void SceneReset()
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.SceneReset();
        }
    }

    public void GameObjectCreated(GameObject go)
    {
        go.WorldEventListener = this;

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

        go.WorldEventListener = null;
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.ComponentAdded(go, component);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        foreach (var listener in _worldEventListeners)
        {
            listener.ComponentRemoved(go, component);
        }
    }

    public void Dispose()
    {
        CurrentScene?.Dispose();
        PhysicsWorld.Dispose();
        Assets.Dispose();
    }
}
