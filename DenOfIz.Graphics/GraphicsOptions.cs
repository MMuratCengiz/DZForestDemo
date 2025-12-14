using DenOfIz;

namespace Graphics;

/// <summary>
/// Configuration options for graphics initialization.
/// </summary>
public sealed class GraphicsOptions
{
    /// <summary>Number of back buffers for swapchain (typically 2 or 3).</summary>
    public uint NumFrames { get; set; } = 3;

    /// <summary>Back buffer format.</summary>
    public Format BackBufferFormat { get; set; } = Format.B8G8R8A8Unorm;

    /// <summary>Depth buffer format. Set to Format.Unknown to disable depth buffer.</summary>
    public Format DepthBufferFormat { get; set; } = Format.D32Float;

    /// <summary>Allow tearing for variable refresh rate displays.</summary>
    public bool AllowTearing { get; set; } = true;

    /// <summary>Preferred graphics API.</summary>
    public APIPreference ApiPreference { get; set; } = new()
    {
        Windows = APIPreferenceWindows.Directx12,
        Linux = APIPreferenceLinux.Vulkan,
        OSX = APIPreferenceOSX.Metal,
        Web = APIPreferenceWeb.Webgpu
    };
}
