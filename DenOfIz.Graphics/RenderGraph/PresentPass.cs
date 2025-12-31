using DenOfIz;
using Graphics;

namespace Graphics.RenderGraph;

public abstract class PresentPass(GraphicsContext context) : RenderPassBase(context)
{
    internal void Execute(ref RenderPassContext ctx, Texture swapChainImage)
    {
        OnExecute(ref ctx, swapChainImage);
    }

    protected abstract void OnExecute(ref RenderPassContext ctx, Texture swapChainImage);
}
