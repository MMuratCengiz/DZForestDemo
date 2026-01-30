using NiziKit.Components;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer;

public interface IRenderer : IDisposable
{
    CameraComponent? Camera { get; set; }

    CycledTexture Render(RenderFrame frame);

    void OnResize(uint width, uint height);
}
