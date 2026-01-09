namespace DenOfIz.World.Graphics;

public sealed class GraphicsDesc
{
    public uint NumFrames { get; set; } = 3;
    public Format BackBufferFormat { get; set; } = Format.B8G8R8A8Unorm;
    public Format DepthBufferFormat { get; set; } = Format.D32Float;
    public bool AllowTearing { get; set; } = true;

    public APIPreference ApiPreference { get; set; } = new()
    {
        Windows = APIPreferenceWindows.Directx12,
        Linux = APIPreferenceLinux.Vulkan,
        OSX = APIPreferenceOSX.Metal,
        Web = APIPreferenceWeb.Webgpu
    };
}