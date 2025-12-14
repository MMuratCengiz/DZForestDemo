using Graphics;

namespace Application;

/// <summary>
/// Configuration options for creating an application.
/// </summary>
public sealed class ApplicationOptions
{
    /// <summary>Window title.</summary>
    public string Title { get; set; } = "DenOfIz.Application";

    /// <summary>Initial window width in pixels.</summary>
    public uint Width { get; set; } = 1920;

    /// <summary>Initial window height in pixels.</summary>
    public uint Height { get; set; } = 1080;

    /// <summary>Target fixed update rate in Hz. Set to 0 to disable fixed updates.</summary>
    public double FixedUpdateRate { get; set; } = 60.0;

    /// <summary>Graphics configuration options.</summary>
    public GraphicsOptions Graphics { get; set; } = new();
}
