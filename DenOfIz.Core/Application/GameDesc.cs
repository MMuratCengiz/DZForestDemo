using DenOfIz.World.Graphics;

namespace DenOfIz.World.Application;

public sealed class GameDesc
{
    public string Title { get; init; } = "DenOfIz Game";
    public uint Width { get; init; } = 1920;
    public uint Height { get; init; } = 1080;
    public double FixedUpdateRate { get; init; } = 60.0;
    public GraphicsDesc Graphics { get; init; } = new();
}
