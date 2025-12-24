using System.Numerics;
using DenOfIz;
using DZForestDemo.RenderPasses;
using DZForestDemo.Scenes;
using ECS;
using ECS.Components;
using Graphics;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo.Systems;

public sealed class SceneRenderSystem : ISystem
{
    private const float LightMoveSpeed = 10f;

    private readonly List<ShadowPass.ShadowData> _shadowData = [];
    private AssetResource _assets = null!;
    private Camera _camera = null!;
    private CompositeRenderPass _compositePass = null!;
    private GraphicsResource _ctx = null!;
    private DebugRenderPass _debugPass = null!;
    private ResourceHandle _depthRt;
    private bool _disposed;
    private Vector3 _lightCameraPosition;
    private Matrix4x4 _lightViewProjection;
    private SceneRenderPass _scenePass = null!;
    private ResourceHandle _sceneRt;
    private ResourceHandle _shadowAtlas;
    private ShadowPass _shadowPass = null!;
    private MyRenderBatcher _batcher = null!;
    private RgCommandList _rgCommandList = null!;
    private StepTimer _stepTimer = null!;
    private float _totalTime;
    private UiRenderPass _uiPass = null!;
    private ResourceHandle _uiRt;
    private bool _useLightCamera;
    private World _world = null!;
    private Vector3 _debugLightPosition = new(0, 10, 0);

    public void Initialize(World world)
    {
        _world = world;
        _ctx = world.GetResource<GraphicsResource>();
        _assets = world.GetResource<AssetResource>();

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

    private void OnAddCubeClicked()
    {
        var registry = _world.TryGetResource<SceneRegistry<DemoGameState>>();
        if (registry?.ActiveScene is FoxScene foxScene)
        {
            foxScene.AddCube();
        }
    }

    private void OnAdd100CubesClicked()
    {
        var registry = _world.TryGetResource<SceneRegistry<DemoGameState>>();
        if (registry?.ActiveScene is FoxScene foxScene)
        {
            foxScene.Add100Cubes();
        }
    }

    public bool OnEvent(ref Event ev)
    {
        _uiPass.HandleEvent(ev);
        _camera.HandleEvent(ev);

        var registry = _world.TryGetResource<SceneRegistry<DemoGameState>>();
        var activeScene = registry?.ActiveScene;
        activeScene?.OnEvent(ref ev);

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
                    SwitchToScene(DemoGameState.Fox);
                    break;
                case KeyCode.F2:
                    SwitchToScene(DemoGameState.Viking);
                    break;
            }

            UpdateDebugLight();
        }

        return false;
    }

    private void SwitchToScene(DemoGameState state)
    {
        var currentState = _world.GetCurrentState<DemoGameState>();
        if (currentState == state)
        {
            return;
        }

        _ctx.RenderGraph.WaitIdle();
        _world.SetNextState(state);
        Console.WriteLine($"Switching to {state.State} scene...");
    }

    public void Run()
    {
        _stepTimer.Tick();
        var deltaTime = (float)_stepTimer.GetElapsedSeconds();
        _totalTime += deltaTime;

        _camera.Update(deltaTime);

        var registry = _world.TryGetResource<SceneRegistry<DemoGameState>>();
        var activeScene = registry?.ActiveScene;

        if (activeScene != null)
        {
            _batcher.ActiveSceneFilter = activeScene.Scene.Id;
            activeScene.OnUpdate(deltaTime);
        }
        else
        {
            _batcher.ActiveSceneFilter = SceneId.Invalid;
        }

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

        activeScene?.OnRender();
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
        var registry = _world.TryGetResource<SceneRegistry<DemoGameState>>();
        var activeScene = registry?.ActiveScene;

        Entity debugLightEntity = default;
        if (activeScene is FoxScene foxScene)
        {
            debugLightEntity = foxScene.DebugLightEntity;
        }
        else if (activeScene is VikingScene vikingScene)
        {
            debugLightEntity = vikingScene.DebugLightEntity;
        }

        if (debugLightEntity.Index != 0 && _world.Entities.IsAlive(debugLightEntity))
        {
            ref var transform = ref _world.GetComponent<Transform>(debugLightEntity);
            transform.Position = _debugLightPosition;
        }
    }

    private void UpdateLightCamera()
    {
        foreach (var (_, light) in _world.Query<DirectionalLight>())
        {
            if (!light.CastShadows)
            {
                continue;
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
            break;
        }
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
