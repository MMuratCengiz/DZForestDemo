using NiziKit.Components;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer;

public interface IRenderer : IDisposable
{
    /// <summary>
    /// Optional camera override. If null, uses scene's active camera.
    /// </summary>
    CameraComponent? Camera { get; set; }

    /// <summary>
    /// Render the scene and return the result texture.
    /// Caller is responsible for presenting/compositing.
    /// </summary>
    CycledTexture Render(RenderFrame frame);

    void OnResize(uint width, uint height);
}
