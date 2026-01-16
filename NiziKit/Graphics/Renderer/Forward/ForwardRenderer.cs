using NiziKit.Application.Timing;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Graph;
using NiziKit.Graphics.Renderer.Common;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly RenderGraph _graph;
    private readonly RenderPass[] _passes;
    private readonly PresentPass _presentPass;

    private readonly ViewData _viewData;
    private readonly ForwardScenePass _forwardScenePass;

    private int _frameCount;
    private float _fpsAccumulator;
    private float _lastFpsPrintTime;

    public ForwardRenderer()
    {
        _graph = new RenderGraph();
        _viewData = new ViewData();

        _forwardScenePass = new ForwardScenePass(_viewData);
        _passes = [_forwardScenePass];
        _presentPass = new BlittingPresentPass();
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
            Console.WriteLine($"ForwardRenderer FPS: {fps:F1}");
            _frameCount = 0;
            _fpsAccumulator = 0;
            _lastFpsPrintTime = Time.TotalTime;
        }

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;

        var viewBinding = GpuBinding.Get<ViewBinding>(_viewData);
        viewBinding.Update(_viewData);

        _graph.Execute(_passes.AsSpan(), _presentPass);
    }

    public void OnResize(uint width, uint height)
    {
        _graph.Resize(width, height);
    }

    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }

        _presentPass.Dispose();
        _graph.Dispose();
    }
}
