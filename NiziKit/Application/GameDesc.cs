using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Application;

public sealed class GameDesc
{
    public string Title { get; init; } = "DenOfIz Game";
    public uint Width { get; init; } = 0;
    public uint Height { get; init; } = 0;
    public bool Resizable { get; init; } = false;
    public bool Maximized { get; init; } = false;
    public bool Borderless { get; init; } = false;
    public bool Fullscreen { get; init; } = false;
    public double FixedUpdateRate { get; init; } = 60.0;
    public GraphicsDesc Graphics { get; init; } = new();
}
