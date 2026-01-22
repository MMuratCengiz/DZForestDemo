using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;
using NiziKit.UI;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly RenderFrame _renderFrame;
    private readonly ViewData _viewData;

    private CycledTexture _sceneColor = null!;
    private CycledTexture _sceneDepth = null!;
    private uint _width;
    private uint _height;

    private readonly UiBuildCallback? _uiBuildCallback;

    public ForwardRenderer(UiBuildCallback? uiBuildCallback = null)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);
        _uiBuildCallback = uiBuildCallback;

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

    public void Render()
    {
        var renderWorld = World.RenderWorld;
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;

        _renderFrame.BeginFrame();

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);

        pass.Begin();

        pass.Bind<ViewBinding>(_viewData);

        foreach (var material in renderWorld.GetMaterials())
        {
            var gpuShader = material.GpuShader;
            if (gpuShader == null)
            {
                continue;
            }

            pass.BindShader(gpuShader);
            pass.Bind<MaterialBinding>(material);

            foreach (var batch in renderWorld.GetDrawBatches(material))
            {
                pass.Bind<BatchDrawBinding>(batch);
                pass.DrawMesh(batch.Mesh, (uint)batch.Count);
            }
        }

        pass.End();

        var debugOverlay = _renderFrame.RenderDebugOverlay();
        _renderFrame.AlphaBlit(debugOverlay, _sceneColor);

        if (_uiBuildCallback != null)
        {
            var ui = _renderFrame.RenderUi(_uiBuildCallback);
            _renderFrame.AlphaBlit(ui, _sceneColor);
        }

        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
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
        _renderFrame.SetUiViewportSize(width, height);
    }

    public void Dispose()
    {
        GraphicsContext.WaitIdle();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
    }
}
