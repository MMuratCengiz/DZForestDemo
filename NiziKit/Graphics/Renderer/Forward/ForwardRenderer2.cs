using System.Diagnostics;
using System.Numerics;
using DenOfIz;
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

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private float _lastFrameTime;
    private float _totalTime;

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
        _sceneColor = new CycledTexture(new TextureDesc
        {
            Width = _width,
            Height = _height,
            Depth = 1,
            Format = GraphicsContext.BackBufferFormat,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            HeapType = HeapType.Gpu,
            DebugName = StringView.Intern("SceneColor")
        });

        _sceneDepth = new CycledTexture(new TextureDesc
        {
            Width = _width,
            Height = _height,
            Depth = 1,
            Format = Format.D32Float,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            HeapType = HeapType.Gpu,
            DebugName = StringView.Intern("SceneDepth"),
            ClearDepthStencilHint = new Vector2(1, 0)
        });
    }

    public void Render()
    {
        var renderWorld = World.RenderWorld;
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return;
        }

        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        _totalTime = currentTime;

        _frameCount++;
        _fpsAccumulator += deltaTime;
        if (currentTime - _lastFpsPrintTime >= 1.0f)
        {
            var fps = _frameCount / _fpsAccumulator;
            Console.WriteLine($"ForwardRenderer2 FPS: {fps:F1}");
            _frameCount = 0;
            _fpsAccumulator = 0;
            _lastFpsPrintTime = currentTime;
        }

        _viewData.Scene = scene;
        _viewData.DeltaTime = deltaTime;
        _viewData.TotalTime = _totalTime;

        var viewBinding = GpuBinding.Get<ViewBinding>(_viewData);
        viewBinding.Update(_viewData);

        _renderFrame.BeginFrame();

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);

        pass.Begin();

        pass.Bind(viewBinding);

        foreach (var material in renderWorld.GetMaterials())
        {
            var gpuShader = material.GpuShader;
            if (gpuShader == null)
            {
                continue;
            }

            pass.BindPipeline(gpuShader.Pipeline);

            var materialBinding = GpuBinding.Get<MaterialBinding>(material);
            materialBinding.Update(material);
            pass.Bind(materialBinding);

            foreach (var draw in renderWorld.GetObjects(material))
            {
                var drawBinding = GpuBinding.Get<DrawBinding>(draw.Owner);
                drawBinding.Update(draw.Owner);
                pass.Bind(drawBinding);

                var mesh = draw.Mesh;

                pass.BindVertexBuffer(mesh.VertexBuffer.View.Buffer, mesh.VertexBuffer.View.Offset, mesh.VertexBuffer.Stride, 0);
                pass.BindIndexBuffer(mesh.IndexBuffer.View.Buffer, mesh.IndexBuffer.IndexType, mesh.IndexBuffer.View.Offset);

                pass.DrawIndexed((uint)mesh.NumIndices, 1, 0, 0, 0);
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
