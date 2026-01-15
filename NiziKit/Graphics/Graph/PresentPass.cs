using DenOfIz;

namespace NiziKit.Graphics.Graph;

public abstract class PresentPass() : RenderPassBase()
{
    public abstract void Execute(ref RenderPassContext ctx, Texture swapChainImage);
}
