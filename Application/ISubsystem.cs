namespace Application;

/// <summary>
/// Represents a subsystem that can be registered with an application.
/// Subsystems receive lifecycle callbacks and events from the application.
/// </summary>
public interface ISubsystem : IDisposable
{
    /// <summary>
    /// Called once after all subsystems are registered but before the first frame.
    /// Use for initialization that depends on other subsystems being present.
    /// </summary>
    void Initialize() { }

    /// <summary>
    /// Called once per frame before rendering. Delta time is in seconds.
    /// </summary>
    void Update(double deltaTime) { }

    /// <summary>
    /// Called once per frame after Update but before the frame ends.
    /// Use for late-update logic that needs to run after all Update calls.
    /// </summary>
    void LateUpdate(double deltaTime) { }

    /// <summary>
    /// Called at a fixed timestep, independent of frame rate.
    /// Use for physics or other time-sensitive logic.
    /// </summary>
    void FixedUpdate(double fixedDeltaTime) { }

    /// <summary>
    /// Called when the application is shutting down, before Dispose.
    /// </summary>
    void Shutdown() { }
}
