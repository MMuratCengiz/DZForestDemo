namespace Graphics;

public interface IRenderer
{
    void Initialize(GraphicsContext ctx) { }
    void Render(GraphicsContext ctx);
    void OnResize(GraphicsContext ctx, uint width, uint height) { }
    void Shutdown(GraphicsContext ctx) { }
}
