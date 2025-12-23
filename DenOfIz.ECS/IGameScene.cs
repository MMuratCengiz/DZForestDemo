using Flecs.NET.Core;

namespace ECS;

/// <summary>
/// Interface for game scenes that respond to enter/exit events.
/// Scenes are registered with world.RegisterScene(state, scene) and their
/// OnEnter/OnExit methods are called during state transitions.
/// </summary>
public interface IGameScene : IDisposable
{
    /// <summary>
    /// Called when entering this scene. Use world.SpawnInScene() to create entities.
    /// </summary>
    void OnEnter(World world);

    /// <summary>
    /// Called when exiting this scene. Entities spawned with SpawnInScene are auto-deleted.
    /// </summary>
    void OnExit(World world);
}

/// <summary>
/// Base class for game scenes with common dispose pattern.
/// </summary>
public abstract class GameSceneBase : IGameScene
{
    private bool _disposed;

    public virtual void OnEnter(World world) { }
    public virtual void OnExit(World world) { }

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
