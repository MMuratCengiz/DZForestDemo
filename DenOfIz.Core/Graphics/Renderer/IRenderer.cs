namespace DenOfIz.World.Graphics.Renderer;

public interface IRenderer : IDisposable
{
    void Render(SceneManagement.World world);
    void OnResize(uint width, uint height) { }
}
