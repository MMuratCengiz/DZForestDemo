using DenOfIz.World;

namespace Graphics.Renderer;

public class ForwardRenderer : IRenderer
{
    private readonly GraphicsContext _ctx;

    public ForwardRenderer(GraphicsContext ctx)
    {
        _ctx = ctx;
    }

    public void Render(World world)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
