namespace Application;

/// <summary>
/// Interface for subsystems that need to render.
/// Implement this on subsystems that participate in rendering.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Called during the render phase of each frame.
    /// </summary>
    /// <param name="context">Render context containing frame information.</param>
    void Render(ref RenderContext context);
}

/// <summary>
/// Context passed to renderables during the render phase.
/// </summary>
public ref struct RenderContext
{
    /// <summary>Current frame index for multi-buffered resources.</summary>
    public uint FrameIndex;

    /// <summary>Screen/window width in pixels.</summary>
    public uint Width;

    /// <summary>Screen/window height in pixels.</summary>
    public uint Height;

    /// <summary>Time elapsed since last frame in seconds.</summary>
    public double DeltaTime;

    /// <summary>Total elapsed time since application start in seconds.</summary>
    public double TotalTime;
}
