using DenOfIz;

namespace NiziKit.Graphics.Graph;

public abstract class PresentPass(GraphicsContext context) : RenderPassBase(context)
{
    public abstract void Execute(ref RenderPassContext ctx, Texture swapChainImage);
}
