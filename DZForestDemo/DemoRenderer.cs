using System.Numerics;
using DenOfIz;
using DZForestDemo.RenderPasses;
using Graphics;
using Graphics.Batching;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo;

public sealed class DemoRenderer(DemoGame game) : IRenderer
{
    private GraphicsContext _ctx = null!;

    private RenderScene _renderScene = null!;
    private SceneRenderPass? _scenePass;
    private CompositeRenderPass? _compositePass;
    private DebugRenderPass? _debugPass;
    private UiRenderPass? _uiPass;
    private StepTimer _stepTimer = null!;

    private readonly Dictionary<int, RenderObjectHandle> _renderHandles = new();
    private float _totalTime;
    private bool _passesInitialized;

    public void Initialize(GraphicsContext ctx)
    {
        _ctx = ctx;
        _stepTimer = new StepTimer();
        _renderScene = new RenderScene();
    }

    private void EnsurePassesInitialized()
    {
        if (_passesInitialized)
        {
            return;
        }

        _passesInitialized = true;

        var assets = game.Assets!.Resource;
        _scenePass = new SceneRenderPass(_ctx, assets);
        _compositePass = new CompositeRenderPass(_ctx);
        _debugPass = new DebugRenderPass(_ctx);
        _uiPass = new UiRenderPass(_ctx, _stepTimer);

        _uiPass.OnAddCubeClicked += game.AddCube;
        _uiPass.OnAdd100CubeClicked += game.Add100Cubes;
    }

    public void Render(GraphicsContext ctx)
    {
        EnsurePassesInitialized();

        _stepTimer.Tick();
        var deltaTime = (float)_stepTimer.GetElapsedSeconds();
        _totalTime += deltaTime;

        _renderScene.BeginFrame();

        SyncToRenderScene();
        SyncLightsToRenderScene();

        _renderScene.SetMainView(new RenderView
        {
            View = game.Camera.ViewMatrix,
            Projection = game.Camera.ProjectionMatrix,
            ViewProjection = game.Camera.ViewProjectionMatrix,
            Position = game.Camera.Position,
            NearPlane = 0.1f,
            FarPlane = 1000f
        });

        _renderScene.CommitFrame();

        var renderGraph = ctx.RenderGraph;
        var swapchainRt = ctx.SwapchainRenderTarget;
        var viewport = ctx.SwapChain.GetViewport();

        var sceneRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = ctx.Width,
            Height = ctx.Height,
            Format = ctx.BackBufferFormat,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = "SceneRT",
            ClearColorHint = new Vector4 { X = 0.02f, Y = 0.02f, Z = 0.04f, W = 1.0f }
        });

        var depthRt = renderGraph.CreateTransientTexture(TransientTextureDesc.DepthStencil(
            ctx.Width, ctx.Height, Format.D32Float, "DepthRT"));


        renderGraph.AddPass("Scene",
            (ref RenderPassExecuteContext passCtx) =>
            {
                _scenePass!.Execute(ref passCtx, _renderScene, sceneRt, depthRt, viewport, _totalTime);
            });

        var uiRt = _uiPass!.AddPass(renderGraph);
        var debugRt = _debugPass!.AddPass(renderGraph);
        _compositePass!.AddPass(renderGraph, sceneRt, uiRt, debugRt, swapchainRt, viewport);
    }

    private void SyncToRenderScene()
    {
        var seenIds = new HashSet<int>();

        foreach (var data in game.RenderObjects)
        {
            seenIds.Add(data.SceneObjectId);

            if (_renderHandles.TryGetValue(data.SceneObjectId, out var handle))
            {
                if (_renderScene.IsValid(handle))
                {
                    _renderScene.SetTransform(handle, data.Transform);
                    _renderScene.SetMaterial(handle, data.Material);

                    if (data.Animator != null)
                    {
                        _renderScene.SetBoneMatrices(handle, data.Animator.GetFinalBoneMatrices());
                    }
                }
                else
                {
                    _renderHandles.Remove(data.SceneObjectId);
                }
            }

            if (!_renderHandles.ContainsKey(data.SceneObjectId))
            {
                var newHandle = _renderScene.Add(new RenderObjectDesc
                {
                    Mesh = data.Mesh,
                    Transform = data.Transform,
                    Material = data.Material,
                    Flags = data.Flags
                });

                if (newHandle.IsValid())
                {
                    _renderHandles[data.SceneObjectId] = newHandle;

                    if (data.Animator != null)
                    {
                        _renderScene.SetBoneMatrices(newHandle, data.Animator.GetFinalBoneMatrices());
                    }
                }
            }
        }

        var toRemove = new List<int>();
        foreach (var (id, handle) in _renderHandles)
        {
            if (!seenIds.Contains(id))
            {
                _renderScene.Remove(handle);
                toRemove.Add(id);
            }
        }

        foreach (var id in toRemove)
        {
            _renderHandles.Remove(id);
        }
    }

    private void SyncLightsToRenderScene()
    {
        _renderScene.ClearLights();

        foreach (var light in game.Lights)
        {
            _renderScene.AddLight(light);
        }
    }

    public void OnResize(GraphicsContext ctx, uint width, uint height)
    {
        _debugPass?.SetScreenSize(width, height);
        _uiPass?.HandleResize(width, height);
        game.Camera.SetAspectRatio(width, height);
    }

    public void HandleEvent(Event ev)
    {
        _uiPass?.HandleEvent(ev);
    }

    public void Shutdown(GraphicsContext ctx)
    {
        ctx.WaitIdle();
    }

    public void Dispose()
    {
        _debugPass?.Dispose();
        _compositePass?.Dispose();
        _scenePass?.Dispose();
        _uiPass?.Dispose();
        _renderScene?.Dispose();
    }
}
