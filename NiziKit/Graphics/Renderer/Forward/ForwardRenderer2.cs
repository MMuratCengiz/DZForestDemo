using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;
using RenderFrame = NiziKit.Graphics.Renderer.RenderFrame;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer2 : IRenderer
{
    private readonly RenderFrame _renderFrame;
    private readonly ViewData _viewData;

    private CycledTexture _sceneColor = null!;
    private CycledTexture _sceneDepth = null!;
    private uint _width;
    private uint _height;

    private int _frameCount;
    private float _fpsAccumulator;
    private float _lastFpsPrintTime;

    public ForwardRenderer2()
    {
        _renderFrame = new RenderFrame();
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

        _frameCount++;
        _fpsAccumulator += Time.DeltaTime;
        if (Time.TotalTime - _lastFpsPrintTime >= 1.0f)
        {
            var fps = _frameCount / _fpsAccumulator;
            Console.WriteLine($"ForwardRenderer2 FPS: {fps:F1}");
            _frameCount = 0;
            _fpsAccumulator = 0;
            _lastFpsPrintTime = Time.TotalTime;
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

            pass.BindPipeline(gpuShader.Pipeline);
            pass.Bind<MaterialBinding>(material);

            foreach (var draw in renderWorld.GetObjects(material))
            {
                pass.Bind<DrawBinding>(draw.Owner);
                pass.DrawMesh(draw.Mesh);
            }
        }

        pass.End();

        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
    }

    public void OnResize(uint width, uint height)
    {
        if (_width == width && _height == height)
        {
            return;
        }

        GraphicsContext.GraphicsCommandQueue.WaitIdle();

        _sceneColor.Dispose();
        _sceneDepth.Dispose();

        _width = width;
        _height = height;
        CreateRenderTargets();
    }

    public void Dispose()
    {
        GraphicsContext.GraphicsCommandQueue.WaitIdle();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
    }
}
