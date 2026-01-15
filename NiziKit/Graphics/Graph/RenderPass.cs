namespace NiziKit.Graphics.Graph;

public abstract class RenderPass() : RenderPassBase()
{
    public abstract void Execute(ref RenderPassContext ctx);
}
