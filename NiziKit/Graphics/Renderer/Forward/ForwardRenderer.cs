using System.Diagnostics;
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

    private readonly GpuView _gpuView;
    private readonly ForwardScenePass _forwardScenePass;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private float _lastFrameTime;
    private float _totalTime;

    public ForwardRenderer()
    {
        _graph = new RenderGraph();
        _gpuView = new GpuView();

        _forwardScenePass = new ForwardScenePass(_gpuView);
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

        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        _totalTime = currentTime;

        _gpuView.Update(scene, _graph.FrameIndex, deltaTime, _totalTime);
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
        _gpuView.Dispose();
    }
}