using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using DenOfIz.World;
using DZForestDemo.RenderPasses;
using Graphics;
using Graphics.Batching;
using Graphics.Binding;
using Graphics.Binding.Data;
using Graphics.Renderer;
using Graphics.RenderGraph;
using RuntimeAssets;
using RuntimeAssets.Store;

namespace DZForestDemo;

public sealed class DemoRenderer(DemoGame game) : IRenderer
{
    private GraphicsContext _ctx = null!;

    private GpuView? _gpuView;
    private GpuDrawBatcher? _drawBatcher;

    private SceneRenderPass? _scenePass;
    private CompositeRenderPass? _compositePass;
    private DebugRenderPass? _debugPass;
    private UiRenderPass? _uiPass;
    private StepTimer _stepTimer = null!;

    private float _totalTime;
    private bool _passesInitialized;

    public void Initialize(GraphicsContext ctx)
    {
        _ctx = ctx;
        _stepTimer = new StepTimer();
    }

    private void EnsurePassesInitialized()
    {
        if (_passesInitialized)
        {
            return;
        }

        _passesInitialized = true;

        _gpuView = new GpuView(_ctx);
        _drawBatcher = new GpuDrawBatcher(_ctx);

        var assets = game.Assets!.Resource;
        _scenePass = new SceneRenderPass(_ctx, assets);
        _compositePass = new CompositeRenderPass(_ctx);
        _debugPass = new DebugRenderPass(_ctx);
        _uiPass = new UiRenderPass(_ctx, _stepTimer);

        _uiPass.OnAddCubeClicked += game.AddCube;
        _uiPass.OnAdd100CubeClicked += game.Add100Cubes;
    }

    public void Render(World world)
    {
        EnsurePassesInitialized();

        _stepTimer.Tick();
        var deltaTime = (float)_stepTimer.GetElapsedSeconds();
        _totalTime += deltaTime;

        var frameIndex = _ctx.FrameIndex;
        var scene = world.CurrentScene;

        if (scene != null)
        {
            // Scene-based update: GpuView queries lights and camera from the scene
            _gpuView!.Update(scene, frameIndex, deltaTime, _totalTime);
        }

        _drawBatcher!.BeginFrame(frameIndex);
        BuildDrawsFromScene();

        var renderGraph = _ctx.RenderGraph;
        var swapchainRt = _ctx.SwapchainRenderTarget;
        var viewport = _ctx.SwapChain.GetViewport();

        var sceneRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = _ctx.Width,
            Height = _ctx.Height,
            Format = _ctx.BackBufferFormat,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = "SceneRT",
            ClearColorHint = new Vector4 { X = 0.02f, Y = 0.02f, Z = 0.04f, W = 1.0f }
        });

        var depthRt = renderGraph.CreateTransientTexture(TransientTextureDesc.DepthStencil(
            _ctx.Width, _ctx.Height, Format.D32Float, "DepthRT"));

        renderGraph.AddPass("Scene",
            (ref RenderPassExecuteContext passCtx) =>
            {
                _scenePass!.Execute(ref passCtx, _gpuView!, _drawBatcher!, game.Assets!.Resource, sceneRt, depthRt, viewport);
            });

        var uiRt = _uiPass!.AddPass(renderGraph);
        var debugRt = _debugPass!.AddPass(renderGraph);
        _compositePass!.AddPass(renderGraph, sceneRt, uiRt, debugRt, swapchainRt, viewport);
    }

    private readonly Dictionary<MeshId, List<GpuInstanceData>> _staticBatches = new();

    private void BuildDrawsFromScene()
    {
        foreach (var list in _staticBatches.Values)
        {
            list.Clear();
        }

        Texture? activeTexture = null;

        foreach (var data in game.RenderObjects)
        {
            if (activeTexture == null && data.Material.AlbedoTexture.IsValid)
            {
                var texHandle = new RuntimeTextureHandle(
                    data.Material.AlbedoTexture.Index,
                    data.Material.AlbedoTexture.Generation);
                if (game.Assets!.Resource.TryGetTexture(texHandle, out var runtimeTex))
                {
                    activeTexture = runtimeTex.Resource;
                }
            }

            var instance = new GpuInstanceData
            {
                Model = data.Transform,
                BaseColor = data.Material.BaseColor,
                Metallic = data.Material.Metallic,
                Roughness = data.Material.Roughness,
                AmbientOcclusion = data.Material.AmbientOcclusion,
                UseAlbedoTexture = data.Material.AlbedoTexture.IsValid ? 1u : 0u
            };

            if (data.Animator != null && (data.Flags & RenderFlags.Skinned) != 0)
            {
                var bones = data.Animator.GetFinalBoneMatrices();
                _drawBatcher!.AddSkinnedDraw(data.Mesh, instance, bones);
            }
            else
            {
                if (!_staticBatches.TryGetValue(data.Mesh, out var list))
                {
                    list = new List<GpuInstanceData>();
                    _staticBatches[data.Mesh] = list;
                }
                list.Add(instance);
            }
        }

        _scenePass!.SetActiveTexture(activeTexture);

        foreach (var (meshId, instances) in _staticBatches)
        {
            if (instances.Count > 0)
            {
                _drawBatcher!.AddStaticDraw(meshId, CollectionsMarshal.AsSpan(instances));
            }
        }
    }

    public void OnResize(uint width, uint height)
    {
        _debugPass?.SetScreenSize(width, height);
        _uiPass?.HandleResize(width, height);
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
        _gpuView?.Dispose();
        _drawBatcher?.Dispose();
        _debugPass?.Dispose();
        _compositePass?.Dispose();
        _scenePass?.Dispose();
        _uiPass?.Dispose();
    }
}
