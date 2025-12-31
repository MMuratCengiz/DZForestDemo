using Graphics;

namespace Application;

public sealed class GameDesc
{
    public string Title { get; set; } = "DenOfIz Game";
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double FixedUpdateRate { get; set; } = 60.0;
    public GraphicsDesc Graphics { get; set; } = new();
}
