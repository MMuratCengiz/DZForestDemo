using NiziKit.Core;

namespace NiziKit.Graphics.Renderer;

public interface IRenderer : IDisposable
{
    void Render(World world);
    void OnResize(uint width, uint height) { }
}
