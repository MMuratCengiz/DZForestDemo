using DenOfIz.World.Graphics.Graph;

namespace DenOfIz.World.Graphics.Renderer.Common;

public class BlittingPresentPass(GraphicsContext context) : PresentPass(context)
{
    public override string Name { get; }
    public override void Execute(ref RenderPassContext ctx, Texture swapChainImage)
    {
        throw new NotImplementedException();
    }
}