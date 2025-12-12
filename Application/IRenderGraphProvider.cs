namespace Application;

/// <summary>
/// Interface for subsystems that provide a render graph for frame rendering.
/// Only one subsystem should implement this interface per application.
/// </summary>
public interface IRenderGraphProvider
{
    /// <summary>
    /// Called at the start of each frame to begin render graph recording.
    /// </summary>
    /// <param name="frameIndex">Current frame index for multi-buffered resources.</param>
    void BeginFrame(uint frameIndex);

    /// <summary>
    /// Called after all render passes are recorded to compile and execute the graph.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Called when the window is resized.
    /// </summary>
    /// <param name="width">New width in pixels.</param>
    /// <param name="height">New height in pixels.</param>
    void OnResize(uint width, uint height);

    /// <summary>
    /// Waits for all GPU work to complete.
    /// </summary>
    void WaitIdle();
}
