using Graphics;

namespace Application;

public sealed class ApplicationOptions
{
    public string Title { get; set; } = "DenOfIz.Application";
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double FixedUpdateRate { get; set; } = 60.0;
    public GraphicsDesc Graphics { get; set; } = new();
}