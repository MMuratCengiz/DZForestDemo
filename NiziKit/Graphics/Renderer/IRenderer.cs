namespace NiziKit.Graphics.Renderer;

public interface IRenderer : IDisposable
{
    void Render();
    void OnResize(uint width, uint height) { }
}
