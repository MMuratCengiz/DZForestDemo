using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Graph;

public struct RenderPassContext
{
    public GraphicsContext GraphicsContext;
    public FrameResources Resources;
    public CommandList CommandList;
    public uint FrameIndex;
    public uint Width;
    public uint Height;

    public ResourceTracking ResourceTracking => GraphicsContext.ResourceTracking;
    public LogicalDevice LogicalDevice => GraphicsContext.LogicalDevice;

    public Texture GetTexture(string name) => Resources.GetTexture(name);
    public Buffer GetBuffer(string name) => Resources.GetBuffer(name);
}
