namespace NiziKit.Graphics.Renderer;

public interface IRendererProvider
{
    public IRenderer Create(GraphicsContext ctx);
}