namespace NiziKit.Graphics.Graph;

public abstract class RenderPass(GraphicsContext context) : RenderPassBase(context)
{
    public abstract void Execute(ref RenderPassContext ctx);
}
