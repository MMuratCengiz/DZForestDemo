namespace Graphics.Renderer;

public interface IRendererProvider
{
    public IRenderer Create(GraphicsContext ctx);
}