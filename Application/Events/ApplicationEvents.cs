namespace Application.Events;

/// <summary>
/// Application lifecycle event types.
/// </summary>
public enum ApplicationEventType
{
    /// <summary>Application is about to quit.</summary>
    Quit,

    /// <summary>Window was resized.</summary>
    Resized,

    /// <summary>Window gained focus.</summary>
    FocusGained,

    /// <summary>Window lost focus.</summary>
    FocusLost,

    /// <summary>Window was minimized.</summary>
    Minimized,

    /// <summary>Window was restored from minimized state.</summary>
    Restored
}

/// <summary>
/// Application-level event data.
/// </summary>
public readonly struct ApplicationEvent(ApplicationEventType type, uint width = 0, uint height = 0)
{
    /// <summary>The type of application event.</summary>
    public readonly ApplicationEventType Type = type;

    /// <summary>New width for resize events.</summary>
    public readonly uint Width = width;

    /// <summary>New height for resize events.</summary>
    public readonly uint Height = height;
}

/// <summary>
/// Interface for receiving application lifecycle events.
/// </summary>
public interface IApplicationEventReceiver
{
    /// <summary>
    /// Called when an application event occurs.
    /// </summary>
    /// <param name="ev">The application event.</param>
    void OnApplicationEvent(in ApplicationEvent ev);
}
