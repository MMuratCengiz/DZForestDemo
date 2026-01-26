using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly ViewData _viewData;

    private CycledTexture _sceneColor = null!;
    private CycledTexture _sceneDepth = null!;
    private uint _width;
    private uint _height;

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set => _viewData.Camera = value;
    }

    public ForwardRenderer()
    {
        _viewData = new ViewData();
        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;
        CreateRenderTargets();
    }

    private void CreateRenderTargets()
    {
        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth");
    }

    public CycledTexture Render(RenderFrame frame)
    {
        var renderWorld = World.RenderWorld;
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return _sceneColor;
        }

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;

        var pass = frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);

        pass.Begin();

        pass.Bind<ViewBinding>(_viewData);

        foreach (var shader in renderWorld.GetShaders())
        {
            pass.BindShader(shader);

            foreach (var surface in renderWorld.GetSurfaces(shader))
            {
                pass.Bind<SurfaceBinding>(surface);
                foreach (var batch in renderWorld.GetDrawBatches(shader, surface))
                {
                    pass.Bind<BatchDrawBinding>(batch);
                    pass.DrawMesh(batch.Mesh, (uint)batch.Count);
                }
            }
        }

        pass.End();

        return _sceneColor;
    }

    public void OnResize(uint width, uint height)
    {
        if (_width == width && _height == height)
        {
            return;
        }

        GraphicsContext.WaitIdle();

        _sceneColor.Dispose();
        _sceneDepth.Dispose();

        _width = width;
        _height = height;
        CreateRenderTargets();
    }

    public void Dispose()
    {
        GraphicsContext.WaitIdle();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
    }
}
