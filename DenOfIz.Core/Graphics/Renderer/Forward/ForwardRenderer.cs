using DenOfIz.World.Graphics.Graph;
using DenOfIz.World.Graphics.Renderer.Common;

namespace DenOfIz.World.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly RenderGraph _graph;
    private RenderPass[] _passes;
    private PresentPass _presentPass;

    public ForwardRenderer(GraphicsContext ctx)
    {
        _graph = new RenderGraph(ctx);
        _passes =
        [
            new ForwardScenePass(ctx)
        ];
        _presentPass = new BlittingPresentPass(ctx);
    }

    public void Render(SceneManagement.World world)
    {
        _graph.Execute(_passes.AsSpan(), _presentPass);
    }

    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }
        _presentPass.Dispose();
        _graph.Dispose();
    }
}
