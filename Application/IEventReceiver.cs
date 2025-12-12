using DenOfIz;

namespace Application;

/// <summary>
/// Interface for objects that can receive and handle input events.
/// Implement this on subsystems that need to process input.
/// </summary>
public interface IEventReceiver
{
    /// <summary>
    /// Called for each polled event. Return true to consume the event
    /// and prevent it from propagating to subsequent receivers.
    /// </summary>
    /// <param name="ev">The event to handle. Passed by ref for zero-allocation.</param>
    /// <returns>True if the event was consumed, false to continue propagation.</returns>
    bool OnEvent(ref Event ev);
}
