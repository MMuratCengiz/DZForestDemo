using Graphics;

namespace Graphics.RenderGraph;

public abstract class RenderPassV2(GraphicsContext context) : RenderPassBase(context)
{
    internal void Execute(ref RenderPassContext ctx)
    {
        OnExecute(ref ctx);
    }

    protected abstract void OnExecute(ref RenderPassContext ctx);
}
