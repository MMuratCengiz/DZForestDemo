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
    private const float LightMoveSpeed = 10f;
    private readonly List<ShadowPass.ShadowData> _shadowData = [];
    private AssetContext _assets = null!;
    private AnimationContext _animation = null!;
    private Camera _camera = null!;
    private CompositeRenderPass _compositePass = null!;
    private GraphicsContext _ctx = null!;
    private int _cubeCount;

    private RuntimeMeshHandle _cubeMesh;
    private Entity _debugLightEntity;
    private Vector3 _debugLightPosition = new(0, 10, 0);
    private DebugRenderPass _debugPass = null!;
    private ResourceHandle _depthRt;

    private bool _disposed;
    private Vector3 _lightCameraPosition;
    private Matrix4x4 _lightViewProjection;
    private StandardMaterial[] _materialPalette = null!;
    private PhysicsContext _physics = null!;
    private RuntimeMeshHandle _platformMesh;
    private readonly Random _random = new();
    private SceneRenderPass _scenePass = null!;

    private ResourceHandle _sceneRt;
    private ResourceHandle _shadowAtlas;
    private ResourceBindGroup? _shadowBindGroup;

    private ShadowPass _shadowPass = null!;
    private RuntimeMeshHandle _smallSphereMesh;
    private RuntimeMeshHandle _sphereMesh;
    private RenderBatcher _batcher = null!;
    private ModelLoadResult? _vikingModel;
    private RuntimeTextureHandle _vikingTexture;
    private RuntimeSkeletonHandle _vikingSkeleton;
    private RuntimeAnimationHandle _vikingAnimation;

    private StepTimer _stepTimer = null!;
    private float _totalTime;
    private UiRenderPass _uiPass = null!;
    private ResourceHandle _uiRt;
    private bool _useLightCamera;
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
        _ctx = world.GetContext<GraphicsContext>();
        _assets = world.GetContext<AssetContext>();
        _physics = world.GetContext<PhysicsContext>();
        _animation = world.GetContext<AnimationContext>();

        _stepTimer = new StepTimer();

        _camera = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _camera.SetAspectRatio(_ctx.Width, _ctx.Height);

        _batcher = new RenderBatcher(_world);
        _shadowPass = new ShadowPass(_ctx, _assets, _world, _batcher);
        _scenePass = new SceneRenderPass(_ctx, _assets, _world, _batcher);
        _uiPass = new UiRenderPass(_ctx, _stepTimer);
        _compositePass = new CompositeRenderPass(_ctx);
        _debugPass = new DebugRenderPass(_ctx);

        _uiPass.OnAddCubeClicked += AddCube;
        _uiPass.OnAdd100CubeClicked += Add100Cubes;
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

    public bool OnEvent(ref Event ev)
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
                case KeyCode.W: _debugLightPosition.Z -= delta; break;
                case KeyCode.S: _debugLightPosition.Z += delta; break;
                case KeyCode.A: _debugLightPosition.X -= delta; break;
                case KeyCode.D: _debugLightPosition.X += delta; break;
                case KeyCode.Q: _debugLightPosition.Y -= delta; break;
                case KeyCode.E: _debugLightPosition.Y += delta; break;
                case KeyCode.L:
                    _useLightCamera = !_useLightCamera;
                    Console.WriteLine($"Light camera: {(_useLightCamera ? "ON" : "OFF")}");
                    break;
            }

            UpdateDebugLight();
        }

        return false;
    }

    public void Run()
    {
        _stepTimer.Tick();
        _totalTime += 0.016f;

        // Build batches once per frame for all render passes
        _batcher.BuildBatches();

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

        _shadowBindGroup?.Dispose();
        _debugPass.Dispose();
        _compositePass.Dispose();
        _scenePass.Dispose();
        _shadowPass.Dispose();
        _uiPass.Dispose();
        _batcher.Dispose();

        GC.SuppressFinalize(this);
    }

    private void CreateLights()
    {
        var sunEntity = _world.Spawn();
        _world.AddComponent(sunEntity, new DirectionalLight(
            new Vector3(0.4f, -0.8f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.9f),
            0.6f
        ));
        
        var ambientEntity = _world.Spawn();
        _world.AddComponent(ambientEntity, new AmbientLight(
            new Vector3(0.5f, 0.6f, 0.7f),
            new Vector3(0.25f, 0.2f, 0.15f),
            0.4f
        ));
        
        var pointLight1 = _world.Spawn();
        _world.AddComponent(pointLight1, new Transform(new Vector3(6, 6, 6)));
        _world.AddComponent(pointLight1, new PointLight(
            new Vector3(1.0f, 0.7f, 0.4f),
            2.5f,
            18.0f
        ));
        
        var pointLight2 = _world.Spawn();
        _world.AddComponent(pointLight2, new Transform(new Vector3(-6, 5, -4)));
        _world.AddComponent(pointLight2, new PointLight(
            new Vector3(0.4f, 0.6f, 1.0f),
            2.0f,
            15.0f
        ));
        
        var pointLight3 = _world.Spawn();
        _world.AddComponent(pointLight3, new Transform(new Vector3(-5, 4, 6)));
        _world.AddComponent(pointLight3, new PointLight(
            new Vector3(0.9f, 0.3f, 0.5f),
            1.8f,
            14.0f
        ));
        
        var pointLight4 = _world.Spawn();
        _world.AddComponent(pointLight4, new Transform(new Vector3(5, 3, -5)));
        _world.AddComponent(pointLight4, new PointLight(
            new Vector3(0.4f, 0.9f, 0.5f),
            1.6f,
            12.0f
        ));
        _debugLightEntity = _world.Spawn();
        _world.AddComponent(_debugLightEntity, new Transform(_debugLightPosition));
        _world.AddComponent(_debugLightEntity, new PointLight(
            new Vector3(1.0f, 1.0f, 0.8f),
            5.0f,
            30.0f
        ));
    }

    private void UpdateDebugLight()
    {
        ref var transform = ref _world.GetComponent<Transform>(_debugLightEntity);
        transform.Position = _debugLightPosition;
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

    private void AddScenePass(RenderGraph renderGraph, Viewport viewport, Matrix4x4 viewProjection,
        Vector3 cameraPosition)
    {
        var time = _totalTime;

        _shadowAtlas = _shadowPass.CreateShadowAtlas(renderGraph);

        // Add separate shadow passes for each light (clear pass + one pass per light)
        _shadowPass.AddPasses(renderGraph, _shadowAtlas, _shadowData, new Vector3(0, 5, 0), 15f);

        // Finalize pass to create the shadow bind group after all shadow rendering is complete
        renderGraph.AddPass("ShadowFinalize",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.ReadTexture(_shadowAtlas);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                var atlas = ctx.GetTexture(_shadowAtlas);
                _shadowBindGroup?.Dispose();
                _shadowBindGroup = _scenePass.CreateShadowBindGroup(atlas);
            });

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
                _scenePass.Execute(ref ctx, _sceneRt, _depthRt, _shadowAtlas, _shadowBindGroup, _shadowData, viewport,
                    viewProjection, cameraPosition, time);
            });
    }

    private void CreateScene()
    {
        _assets.BeginUpload();
        _cubeMesh = _assets.AddBox(1.0f, 1.0f, 1.0f);
        _platformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        _sphereMesh = _assets.AddSphere(1.0f);
        _smallSphereMesh = _assets.AddSphere(0.3f, 8);

        // Load Viking characters model
        _vikingModel = _assets.AddModel("VikingRealm_Characters.glb");
        if (!_vikingModel.Success)
        {
            Console.WriteLine($"Failed to load Viking model: {_vikingModel.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"Loaded Viking model: {_vikingModel.MeshHandles.Count} meshes, {_vikingModel.Materials.Count} materials");
        }

        // Load Viking texture (for future texture support)
        _vikingTexture = _assets.AddTexture("VikingRealm_Texture_01_A_PolygonVikingRealm_Texture_01_A.dztex");
        if (!_vikingTexture.IsValid)
        {
            Console.WriteLine("Failed to load Viking texture");
        }
        else
        {
            Console.WriteLine("Loaded Viking texture");
        }

        _assets.EndUpload();

        // Load Viking skeleton and animation
        _vikingSkeleton = _animation.LoadSkeleton("VikingRealm_Characters_skeleton.ozz");
        if (_vikingSkeleton.IsValid)
        {
            Console.WriteLine("Loaded Viking skeleton");
            _vikingAnimation = _animation.LoadAnimation(_vikingSkeleton, "VikingRealm_Characters_Take 001.ozz");
            if (_vikingAnimation.IsValid)
            {
                Console.WriteLine("Loaded Viking animation");
            }
            else
            {
                Console.WriteLine("Failed to load Viking animation");
            }
        }
        else
        {
            Console.WriteLine("Failed to load Viking skeleton");
        }
        SpawnStaticBox(new Vector3(0, -2, 0), new Vector3(20f, 1f, 20f), _platformMesh, Materials.Concrete);

        // Spawn Viking model meshes
        if (_vikingModel is { Success: true })
        {
            SpawnVikingModel();
        }
        _world.AddComponent(_debugLightEntity, new MeshComponent(_smallSphereMesh));
        _world.AddComponent(_debugLightEntity, new StandardMaterial
        {
            BaseColor = new Vector4(1f, 1f, 0.5f, 1f),
            Metallic = 0f,
            Roughness = 1f,
            AmbientOcclusion = 1f
        });
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
        if (_random.NextSingle() > 0.5f)
        {
            SpawnDynamicBox(position, Vector3.One, _cubeMesh, material, rotation);
        }
        else
        {
            var sphereMaterial = material with { Roughness = 0.2f, Metallic = 0.3f };
            SpawnDynamicSphere(position, 1f, _sphereMesh, sphereMaterial);
        }
        _cubeCount++;
    }

    private void Add100Cubes()
    {
        for (var i = 0; i < 100; i++)
        {
            AddCube();
        }
    }

    private void SpawnVikingModel()
    {
        if (_vikingModel == null || !_vikingModel.Success)
        {
            return;
        }

        // Hand-placed positions for each mesh (character) in the scene
        // Arranged in a semi-circle formation
        var meshPositions = new[]
        {
            new Vector3(-8f, -1.5f, -6f),
            new Vector3(-4f, -1.5f, -8f),
            new Vector3(0f, -1.5f, -9f),
            new Vector3(4f, -1.5f, -8f),
            new Vector3(8f, -1.5f, -6f),
            new Vector3(-6f, -1.5f, 2f),
            new Vector3(-2f, -1.5f, 4f),
            new Vector3(2f, -1.5f, 4f),
            new Vector3(6f, -1.5f, 2f),
            new Vector3(0f, -1.5f, 0f),
        };

        // Rotations to face roughly towards center
        var meshRotations = new[]
        {
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.15f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.08f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI * 0.08f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI * 0.15f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.85f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI * 0.85f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.5f),
        };

        var modelScale = Vector3.One;

        // Set the Viking texture as active for rendering if available
        if (_vikingTexture.IsValid && _assets.TryGetTexture(_vikingTexture, out var texture))
        {
            _scenePass.SetActiveTexture(texture.Resource);
            Console.WriteLine("Set Viking texture as active texture");
        }

        // Viking material with texture (use white base color to show texture as-is)
        var vikingMaterial = new StandardMaterial
        {
            BaseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            Metallic = 0.0f,
            Roughness = 0.7f,
            AmbientOcclusion = 1.0f,
            AlbedoTexture = _vikingTexture
        };

        for (var i = 0; i < _vikingModel.MeshHandles.Count; i++)
        {
            var meshHandle = _vikingModel.MeshHandles[i];

            // Get position - cycle through available positions if we have more meshes than positions
            var positionIndex = i % meshPositions.Length;
            var position = meshPositions[positionIndex];
            var rotation = meshRotations[positionIndex];

            // If we have more meshes than positions, offset them in Z
            if (i >= meshPositions.Length)
            {
                var row = i / meshPositions.Length;
                position += new Vector3(0f, 0f, row * 6f);
            }

            // Get material from the model if available, with texture
            StandardMaterial material;
            if (i < _vikingModel.Materials.Count)
            {
                var matData = _vikingModel.Materials[i];
                material = new StandardMaterial
                {
                    BaseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f), // White to show texture
                    Metallic = matData.Metallic,
                    Roughness = matData.Roughness,
                    AmbientOcclusion = 1.0f,
                    AlbedoTexture = _vikingTexture
                };
            }
            else
            {
                material = vikingMaterial;
            }

            var entity = _world.Spawn();
            _world.AddComponent(entity, new MeshComponent(meshHandle));
            _world.AddComponent(entity, new Transform(position, rotation, modelScale));
            _world.AddComponent(entity, material);

            // Add animation components if skeleton is valid
            if (_vikingSkeleton.IsValid && _animation.TryGetSkeleton(_vikingSkeleton, out var skeleton))
            {
                var animator = new AnimatorComponent(_vikingSkeleton)
                {
                    CurrentAnimation = _vikingAnimation,
                    IsPlaying = true,
                    Loop = true,
                    PlaybackSpeed = 1.0f + (i * 0.1f) // Slightly different speeds for variety
                };
                _world.AddComponent(entity, animator);

                var numJoints = skeleton.NumJoints;
                var boneMatrices = new BoneMatricesComponent(numJoints, _vikingModel.InverseBindMatrices);
                _world.AddComponent(entity, boneMatrices);

                Console.WriteLine($"Added animation to Viking mesh {i} with {numJoints} joints");
            }

            Console.WriteLine($"Spawned Viking mesh {i} at {position}");
        }

        Console.WriteLine($"Spawned {_vikingModel.MeshHandles.Count} Viking mesh entities");
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

    private Entity SpawnDynamicBox(Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material,
        Quaternion? rotation = null, float mass = 1f)
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

    private Entity SpawnDynamicSphere(Vector3 position, float diameter, RuntimeMeshHandle mesh,
        StandardMaterial material, float mass = 1f)
    {
        var entity = _world.Spawn();
        _world.AddComponent(entity, new MeshComponent(mesh));
        _world.AddComponent(entity, new Transform(position, Quaternion.Identity, Vector3.One));
        _world.AddComponent(entity, material);

        var handle = _physics.CreateBody(entity, position, Quaternion.Identity,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(diameter), mass));
        _world.AddComponent(entity, new RigidBody(handle));

        return entity;
    }
}