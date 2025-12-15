using System.Numerics;
using DenOfIz;
using DZForestDemo.RenderPasses;
using ECS;
using ECS.Components;
using Graphics;
using Graphics.RenderGraph;
using Physics;
using Physics.Components;
using RuntimeAssets;

namespace DZForestDemo;

public sealed class GameSystem : ISystem
{
    private World _world = null!;
    private GraphicsContext _ctx = null!;
    private AssetContext _assets = null!;
    private PhysicsContext _physics = null!;

    private StepTimer _stepTimer = null!;
    private Camera _camera = null!;
    private float _totalTime;

    private SceneRenderPass _scenePass = null!;
    private UiRenderPass _uiPass = null!;
    private CompositeRenderPass _compositePass = null!;
    private DebugRenderPass _debugPass = null!;

    private ResourceHandle _sceneRt;
    private ResourceHandle _depthRt;
    private ResourceHandle _uiRt;

    private RuntimeMeshHandle _cubeMesh;
    private RuntimeMeshHandle _platformMesh;
    private RuntimeMeshHandle _sphereMesh;
    private Random _random = new();
    private int _cubeCount;

    // Material palette for spawned objects
    private StandardMaterial[] _materialPalette = null!;

    private bool _disposed;

    public void Initialize(World world)
    {
        _world = world;
        _ctx = world.GetContext<GraphicsContext>();
        _assets = world.GetContext<AssetContext>();
        _physics = world.GetContext<PhysicsContext>();

        _stepTimer = new StepTimer();

        _camera = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _camera.SetAspectRatio(_ctx.Width, _ctx.Height);

        _uiPass = new UiRenderPass(_ctx, _stepTimer);
        _scenePass = new SceneRenderPass(_ctx, _assets, _world);
        _compositePass = new CompositeRenderPass(_ctx);
        _debugPass = new DebugRenderPass(_ctx);

        _uiPass.OnAddCubeClicked += AddCube;

        // Initialize material palette for varied object colors
        _materialPalette =
        [
            Materials.Red,
            Materials.Green,
            Materials.Blue,
            Materials.Yellow,
            Materials.Orange,
            Materials.Purple,
            Materials.Cyan,
            Materials.Wood,
            Materials.Metal,
            Materials.Copper
        ];

        CreateLights();
        CreateScene();
    }

    private void CreateLights()
    {
        // Main directional light (sun)
        var sunEntity = _world.Spawn();
        _world.AddComponent(sunEntity, new DirectionalLight(
            new Vector3(0.4f, -0.8f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.9f),
            1.2f
        ));

        // Ambient light settings
        var ambientEntity = _world.Spawn();
        _world.AddComponent(ambientEntity, new AmbientLight(
            new Vector3(0.5f, 0.6f, 0.7f),  // Sky color
            new Vector3(0.25f, 0.2f, 0.15f), // Ground color
            0.35f
        ));

        // Add a warm point light
        var pointLight1 = _world.Spawn();
        _world.AddComponent(pointLight1, new Transform(new Vector3(5, 5, 5)));
        _world.AddComponent(pointLight1, new PointLight(
            new Vector3(1.0f, 0.7f, 0.4f), // Warm orange
            2.0f,
            15.0f
        ));

        // Add a cool point light
        var pointLight2 = _world.Spawn();
        _world.AddComponent(pointLight2, new Transform(new Vector3(-5, 4, -3)));
        _world.AddComponent(pointLight2, new PointLight(
            new Vector3(0.4f, 0.6f, 1.0f), // Cool blue
            1.5f,
            12.0f
        ));
    }

    public bool OnEvent(ref Event ev)
    {
        _uiPass.HandleEvent(ev);
        _camera.HandleEvent(ev);

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
        }

        return false;
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

    public void Run()
    {
        _stepTimer.Tick();
        _totalTime += 0.016f; // Approximate frame time

        var renderGraph = _ctx.RenderGraph;
        var swapchainRt = _ctx.SwapchainRenderTarget;
        var viewport = _ctx.SwapChain.GetViewport();

        _sceneRt = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = _ctx.Width,
            Height = _ctx.Height,
            Format = _ctx.BackBufferFormat,
            Usages = (uint)(ResourceUsageFlagBits.RenderTarget | ResourceUsageFlagBits.ShaderResource),
            Descriptor = (uint)(ResourceDescriptorFlagBits.RenderTarget | ResourceDescriptorFlagBits.Texture),
            DebugName = "SceneRT"
        });

        _depthRt = renderGraph.CreateTransientTexture(TransientTextureDesc.DepthStencil(
            _ctx.Width, _ctx.Height, Format.D32Float, "DepthRT"));

        var viewProjection = _camera.ViewProjectionMatrix;
        var cameraPosition = _camera.Position;

        AddScenePass(renderGraph, viewport, viewProjection, cameraPosition);
        _uiRt = _uiPass.AddPass(renderGraph);
        var debugRt = _debugPass.AddPass(renderGraph);
        _compositePass.AddPass(renderGraph, _sceneRt, _uiRt, debugRt, swapchainRt, viewport);
    }

    private void AddScenePass(RenderGraph renderGraph, Viewport viewport, Matrix4x4 viewProjection, Vector3 cameraPosition)
    {
        var time = _totalTime;

        renderGraph.AddPass("Scene",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(_sceneRt, (uint)ResourceUsageFlagBits.RenderTarget);
                builder.WriteTexture(_depthRt, (uint)ResourceUsageFlagBits.DepthWrite);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                _scenePass.Execute(ref ctx, _sceneRt, _depthRt, viewport, viewProjection, cameraPosition, time);
            });
    }

    private void CreateScene()
    {
        _assets.BeginUpload();
        _cubeMesh = _assets.AddBox(1.0f, 1.0f, 1.0f);
        _platformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        _sphereMesh = _assets.AddSphere(1.0f, 16);
        _assets.EndUpload();

        // Create static platform with concrete material
        SpawnStaticBox(new Vector3(0, -2, 0), new Vector3(20f, 1f, 20f), _platformMesh, Materials.Concrete);

        // Spawn initial falling cubes with varied materials
        for (var i = 0; i < 5; i++)
        {
            var position = new Vector3(
                (_random.NextSingle() - 0.5f) * 4f,
                5f + i * 2f,
                (_random.NextSingle() - 0.5f) * 4f
            );

            var material = _materialPalette[_random.Next(_materialPalette.Length)];
            SpawnDynamicBox(position, Vector3.One, _cubeMesh, material);
            _cubeCount++;
        }
    }

    private void AddCube()
    {
        var position = new Vector3(
            (_random.NextSingle() - 0.5f) * 6f,
            10f + _random.NextSingle() * 5f,
            (_random.NextSingle() - 0.5f) * 6f
        );

        var rotation = Quaternion.CreateFromYawPitchRoll(
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2
        );

        var material = _materialPalette[_random.Next(_materialPalette.Length)];

        // Randomly spawn cubes or spheres
        if (_random.NextSingle() > 0.5f)
        {
            SpawnDynamicBox(position, Vector3.One, _cubeMesh, material, rotation);
        }
        else
        {
            // Spheres get a shinier material
            var sphereMaterial = material with { Roughness = 0.2f, Metallic = 0.3f };
            SpawnDynamicSphere(position, 1f, _sphereMesh, sphereMaterial);
        }

        _cubeCount++;
    }

    private Entity SpawnStaticBox(Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material)
    {
        var entity = _world.Spawn();
        _world.AddComponent(entity, new MeshComponent(mesh));
        _world.AddComponent(entity, new Transform(position, Quaternion.Identity, Vector3.One));
        _world.AddComponent(entity, material);

        var handle = _physics.CreateStaticBody(entity, position, Quaternion.Identity, PhysicsShape.Box(size));
        _world.AddComponent(entity, new StaticBody(handle));

        return entity;
    }

    private Entity SpawnDynamicBox(Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material, Quaternion? rotation = null, float mass = 1f)
    {
        var rot = rotation ?? Quaternion.Identity;
        var entity = _world.Spawn();
        _world.AddComponent(entity, new MeshComponent(mesh));
        _world.AddComponent(entity, new Transform(position, rot, Vector3.One));
        _world.AddComponent(entity, material);

        var handle = _physics.CreateBody(entity, position, rot, PhysicsBodyDesc.Dynamic(PhysicsShape.Box(size), mass));
        _world.AddComponent(entity, new RigidBody(handle));

        return entity;
    }

    private Entity SpawnDynamicSphere(Vector3 position, float diameter, RuntimeMeshHandle mesh, StandardMaterial material, float mass = 1f)
    {
        var entity = _world.Spawn();
        _world.AddComponent(entity, new MeshComponent(mesh));
        _world.AddComponent(entity, new Transform(position, Quaternion.Identity, Vector3.One));
        _world.AddComponent(entity, material);

        var handle = _physics.CreateBody(entity, position, Quaternion.Identity, PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(diameter), mass));
        _world.AddComponent(entity, new RigidBody(handle));

        return entity;
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
        _uiPass.Dispose();

        GC.SuppressFinalize(this);
    }
}
