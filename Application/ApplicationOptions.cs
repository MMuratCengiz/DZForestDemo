using DenOfIz;

namespace Application;

/// <summary>
/// Configuration options for creating an application.
/// </summary>
public sealed class ApplicationOptions
{
    /// <summary>Window title.</summary>
    public string Title { get; set; } = "Application";

    /// <summary>Initial window width in pixels.</summary>
    public uint Width { get; set; } = 1920;

    /// <summary>Initial window height in pixels.</summary>
    public uint Height { get; set; } = 1080;

    /// <summary>Number of back buffers for swapchain (typically 2 or 3).</summary>
    public uint NumFrames { get; set; } = 3;

    /// <summary>Target fixed update rate in Hz. Set to 0 to disable fixed updates.</summary>
    public double FixedUpdateRate { get; set; } = 60.0;

    /// <summary>Back buffer format.</summary>
    public Format BackBufferFormat { get; set; } = Format.B8G8R8A8Unorm;

    /// <summary>Depth buffer format. Set to Format.Unknown to disable depth buffer.</summary>
    public Format DepthBufferFormat { get; set; } = Format.D32Float;

    /// <summary>Allow tearing for variable refresh rate displays.</summary>
    public bool AllowTearing { get; set; } = true;

    /// <summary>Preferred graphics API.</summary>
    public APIPreference ApiPreference { get; set; } = new()
    {
        Windows = APIPreferenceWindows.Directx12
    };
}
