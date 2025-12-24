using System.Numerics;
using DenOfIz;
using DZForestDemo.RenderPasses;
using DZForestDemo.Scenes;
using ECS;
using ECS.Components;
using Flecs.NET.Core;
using Graphics;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo.Systems;

public sealed class SceneRenderSystem : IDisposable
{
    private const float LightMoveSpeed = 10f;

    private readonly List<ShadowPass.ShadowData> _shadowData = [];
    private readonly World _world;
    private AssetResource _assets;
    private Camera _camera;
    private CompositeRenderPass _compositePass;
    private GraphicsResource _ctx;
    private DebugRenderPass _debugPass;
    private ResourceHandle _depthRt;
    private bool _disposed;
    private Vector3 _lightCameraPosition;
    private Matrix4x4 _lightViewProjection;
    private SceneRenderPass _scenePass;
    private ResourceHandle _sceneRt;
    private ResourceHandle _shadowAtlas;
    private ShadowPass _shadowPass;
    private MyRenderBatcher _batcher;
    private RgCommandList _rgCommandList;
    private StepTimer _stepTimer;
    private float _totalTime;
    private UiRenderPass _uiPass;
    private ResourceHandle _uiRt;
    private bool _useLightCamera;
    private Vector3 _debugLightPosition = new(0, 10, 0);

    public SceneRenderSystem(World world)
    {
        _world = world;
        _ctx = world.Get<GraphicsResource>();
        _assets = world.Get<AssetResource>();

        _stepTimer = new StepTimer();

        _camera = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _camera.SetAspectRatio(_ctx.Width, _ctx.Height);

        _batcher = new MyRenderBatcher(_world);
        _rgCommandList = new RgCommandList(_ctx.LogicalDevice);
        _shadowPass = new ShadowPass(_ctx, _assets, _world, _batcher, _rgCommandList);
        _scenePass = new SceneRenderPass(_ctx, _assets, _world, _batcher, _rgCommandList);
        _uiPass = new UiRenderPass(_ctx, _stepTimer);
        _compositePass = new CompositeRenderPass(_ctx);
        _debugPass = new DebugRenderPass(_ctx);

        _uiPass.OnAddCubeClicked += OnAddCubeClicked;
        _uiPass.OnAdd100CubeClicked += OnAdd100CubesClicked;
    }

    public void Register()
    {
        _world.System("SceneRender")
            .Kind<Render>()
            .Run((Iter _) => Run());
    }

    private void OnAddCubeClicked()
    {
        if (_world.Has<ActiveScene, FoxSceneTag>())
        {
            FoxScene.AddCube(_world);
        }
    }

    private void OnAdd100CubesClicked()
    {
        if (_world.Has<ActiveScene, FoxSceneTag>())
        {
            FoxScene.Add100Cubes(_world);
        }
    }

    public bool HandleEvent(ref Event ev)
    {
        _uiPass.HandleEvent(ev);
        _camera.HandleEvent(ev);

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
        }

        if (ev.Type == EventType.KeyDown)
        {
            const float delta = LightMoveSpeed * 0.016f;
            switch (ev.Key.KeyCode)
            {
                case KeyCode.Up: _debugLightPosition.Z -= delta; break;
                case KeyCode.Down: _debugLightPosition.Z += delta; break;
                case KeyCode.Left: _debugLightPosition.X -= delta; break;
                case KeyCode.Right: _debugLightPosition.X += delta; break;
                case KeyCode.Pagedown: _debugLightPosition.Y -= delta; break;
                case KeyCode.Pageup: _debugLightPosition.Y += delta; break;
                case KeyCode.L:
                    _useLightCamera = !_useLightCamera;
                    Console.WriteLine($"Light camera: {(_useLightCamera ? "ON" : "OFF")}");
                    break;
                case KeyCode.F1:
                    SwitchToScene<FoxSceneTag>();
                    break;
                case KeyCode.F2:
                    SwitchToScene<VikingSceneTag>();
                    break;
            }

            UpdateDebugLight();
        }

        return false;
    }

    private void SwitchToScene<TScene>()
    {
        if (_world.Has<ActiveScene, TScene>())
        {
            return;
        }

        _ctx.RenderGraph.WaitIdle();
        _world.Add<ActiveScene, TScene>();
        Console.WriteLine($"Switching to {typeof(TScene).Name}...");
    }

    private void Run()
    {
        _stepTimer.Tick();
        var deltaTime = (float)_stepTimer.GetElapsedSeconds();
        _totalTime += deltaTime;

        _camera.Update(deltaTime);

        _batcher.BuildBatches();

        var renderGraph = _ctx.RenderGraph;
        var swapchainRt = _ctx.SwapchainRenderTarget;
        var viewport = _ctx.SwapChain.GetViewport();

        _sceneRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = _ctx.Width,
            Height = _ctx.Height,
            Format = _ctx.BackBufferFormat,
            Usage = (uint)(TextureUsageFlagBits.RenderAttachment | TextureUsageFlagBits.TextureBinding),
            DebugName = "SceneRT"
        });

        _depthRt = renderGraph.CreateTransientTexture(TransientTextureDesc.DepthStencil(
            _ctx.Width, _ctx.Height, Format.D32Float, "DepthRT"));
        UpdateLightCamera();
        var viewProjection = _useLightCamera ? _lightViewProjection : _camera.ViewProjectionMatrix;
        var cameraPosition = _useLightCamera ? _lightCameraPosition : _camera.Position;

        AddScenePass(renderGraph, viewport, viewProjection, cameraPosition);
        _uiRt = _uiPass.AddPass(renderGraph);
        var debugRt = _debugPass.AddPass(renderGraph);
        _compositePass.AddPass(renderGraph, _sceneRt, _uiRt, debugRt, swapchainRt, viewport);
    }

    public void Shutdown()
    {
        _ctx.RenderGraph.WaitIdle();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _debugPass.Dispose();
        _compositePass.Dispose();
        _scenePass.Dispose();
        _shadowPass.Dispose();
        _uiPass.Dispose();
        _batcher.Dispose();

        GC.SuppressFinalize(this);
    }

    private void UpdateDebugLight()
    {
        Entity debugLightEntity = default;

        if (_world.Has<FoxSceneAssets>())
        {
            debugLightEntity = _world.Get<FoxSceneAssets>().DebugLightEntity;
        }
        else if (_world.Has<VikingSceneAssets>())
        {
            debugLightEntity = _world.Get<VikingSceneAssets>().DebugLightEntity;
        }

        if (debugLightEntity.IsValid() && debugLightEntity.Has<Transform>())
        {
            ref var transform = ref debugLightEntity.GetMut<Transform>();
            transform.Position = _debugLightPosition;
        }
    }

    private void UpdateLightCamera()
    {
        _world.Query<DirectionalLight>().Each((ref DirectionalLight light) =>
        {
            if (!light.CastShadows)
            {
                return;
            }

            var sceneCenter = new Vector3(0, 5, 0);
            var sceneRadius = 15f;

            var lightDir = Vector3.Normalize(light.Direction);
            var lightDistance = sceneRadius * 1.5f;
            _lightCameraPosition = sceneCenter - lightDir * lightDistance;

            var up = MathF.Abs(lightDir.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
            var view = Matrix4x4.CreateLookAtLeftHanded(_lightCameraPosition, sceneCenter, up);

            var size = sceneRadius * 2.0f;
            var nearPlane = MathF.Max(0.1f, lightDistance - sceneRadius);
            var farPlane = lightDistance + sceneRadius;
            var proj = Matrix4x4.CreateOrthographicLeftHanded(size, size, nearPlane, farPlane);

            _lightViewProjection = view * proj;
        });
    }

    private void HandleResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _debugPass.SetScreenSize(width, height);
        _uiPass.HandleResize(width, height);
        _camera.SetAspectRatio(width, height);
    }

    private void AddScenePass(RenderGraph renderGraph, Viewport viewport, Matrix4x4 viewProjection, Vector3 cameraPosition)
    {
        var time = _totalTime;

        _shadowAtlas = _shadowPass.CreateShadowAtlas(renderGraph);
        _shadowPass.AddPasses(renderGraph, _shadowAtlas, _shadowData, new Vector3(0, 5, 0), 15f);

        renderGraph.AddPass("Scene",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.ReadTexture(_shadowAtlas);
                builder.WriteTexture(_sceneRt);
                builder.WriteTexture(_depthRt, (uint)ResourceUsageFlagBits.DepthWrite);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                _scenePass.Execute(ref ctx, _sceneRt, _depthRt, _shadowAtlas, _shadowData, viewport,
                    viewProjection, cameraPosition, time);
            });
    }

    public void SetActiveTexture(Texture? texture)
    {
        _scenePass.SetActiveTexture(texture);
    }
}
