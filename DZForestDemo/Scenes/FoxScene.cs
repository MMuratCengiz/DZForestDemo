using System.Numerics;
using ECS;
using ECS.Components;
using Graphics;
using Physics;
using Physics.Components;
using RuntimeAssets;
using RuntimeAssets.Components;
using RuntimeAssets.GltfModels;

namespace DZForestDemo.Scenes;

public sealed class FoxScene : GameSceneBase
{
    private AssetResource _assets = null!;
    private AnimationResource _animation = null!;
    private PhysicsResource _physics = null!;
    private GraphicsResource _graphics = null!;

    private RuntimeMeshHandle _cubeMesh;
    private RuntimeMeshHandle _platformMesh;
    private RuntimeMeshHandle _sphereMesh;
    private RuntimeMeshHandle _smallSphereMesh;
    private ModelLoadResult? _foxModel;
    private RuntimeTextureHandle _foxTexture;
    private RuntimeSkeletonHandle _foxSkeleton;
    private RuntimeAnimationHandle _foxAnimation;

    private SceneHierarchyResult? _foxHierarchy;

    private readonly StandardMaterial[] _materialPalette =
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

    private readonly Random _random = new();
    private int _cubeCount;
    private bool _assetsLoaded;

    public override string Name => "FoxScene";

    public Action? OnAddCubeRequested { get; set; }

    public override void OnRegister(World world, Scene scene)
    {
        base.OnRegister(world, scene);
        _assets = world.GetResource<AssetResource>();
        _animation = world.GetResource<AnimationResource>();
        _physics = world.GetResource<PhysicsResource>();
        _graphics = world.GetResource<GraphicsResource>();
    }

    public override void OnEnter()
    {
        if (!_assetsLoaded)
        {
            LoadAssets();
        }
        CreateLights();
        CreateEntities();
    }

    public override void OnExit()
    {
        DisposeAssets();
    }

    private void DisposeAssets()
    {
        if (!_assetsLoaded)
        {
            return;
        }

        _graphics.WaitIdle();

        if (_cubeMesh.IsValid)
        {
            _assets.RemoveMesh(_cubeMesh);
            _cubeMesh = default;
        }
        if (_platformMesh.IsValid)
        {
            _assets.RemoveMesh(_platformMesh);
            _platformMesh = default;
        }
        if (_sphereMesh.IsValid)
        {
            _assets.RemoveMesh(_sphereMesh);
            _sphereMesh = default;
        }
        if (_smallSphereMesh.IsValid)
        {
            _assets.RemoveMesh(_smallSphereMesh);
            _smallSphereMesh = default;
        }
        if (_foxModel is { Success: true })
        {
            foreach (var meshHandle in _foxModel.MeshHandles)
            {
                if (meshHandle.IsValid)
                {
                    _assets.RemoveMesh(meshHandle);
                }
            }
            _foxModel = null;
        }
        if (_foxTexture.IsValid)
        {
            _assets.RemoveTexture(_foxTexture);
            _foxTexture = default;
        }

        _foxHierarchy = null;
        _assetsLoaded = false;
    }

    private void LoadAssets()
    {
        _assets.BeginUpload();

        _cubeMesh = _assets.AddBox(1.0f, 1.0f, 1.0f);
        _platformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        _sphereMesh = _assets.AddSphere(1.0f);
        _smallSphereMesh = _assets.AddSphere(0.3f, 8);

        _foxModel = _assets.AddModel("Fox.glb");
        if (!_foxModel.Success)
        {
            Console.WriteLine($"Failed to load Fox model: {_foxModel.ErrorMessage}");
        }

        _foxTexture = _assets.AddTexture("Fox_Texture.dztex");

        _assets.EndUpload();

        _foxSkeleton = _animation.LoadSkeleton("Fox_skeleton.ozz");
        if (_foxSkeleton.IsValid)
        {
            _foxAnimation = _animation.LoadAnimation(_foxSkeleton, "Fox_Run.ozz");
        }

        _assetsLoaded = true;
    }

    private void CreateLights()
    {
        var sunEntity = Scene.Spawn();
        World.AddComponent(sunEntity, new DirectionalLight(
            new Vector3(0.4f, -0.8f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.9f),
            0.6f
        ));

        var ambientEntity = Scene.Spawn();
        World.AddComponent(ambientEntity, new AmbientLight(
            new Vector3(0.5f, 0.6f, 0.7f),
            new Vector3(0.25f, 0.2f, 0.15f),
            0.4f
        ));

        var pointLight1 = Scene.Spawn();
        World.AddComponent(pointLight1, new Transform(new Vector3(6, 6, 6)));
        World.AddComponent(pointLight1, new PointLight(
            new Vector3(1.0f, 0.7f, 0.4f),
            2.5f,
            18.0f
        ));

        var pointLight2 = Scene.Spawn();
        World.AddComponent(pointLight2, new Transform(new Vector3(-6, 5, -4)));
        World.AddComponent(pointLight2, new PointLight(
            new Vector3(0.4f, 0.6f, 1.0f),
            2.0f,
            15.0f
        ));

        var pointLight3 = Scene.Spawn();
        World.AddComponent(pointLight3, new Transform(new Vector3(-5, 4, 6)));
        World.AddComponent(pointLight3, new PointLight(
            new Vector3(0.9f, 0.3f, 0.5f),
            1.8f,
            14.0f
        ));

        var pointLight4 = Scene.Spawn();
        World.AddComponent(pointLight4, new Transform(new Vector3(5, 3, -5)));
        World.AddComponent(pointLight4, new PointLight(
            new Vector3(0.4f, 0.9f, 0.5f),
            1.6f,
            12.0f
        ));

        DebugLightEntity = Scene.Spawn();
        World.AddComponent(DebugLightEntity, new Transform(new Vector3(0, 10, 0)));
        World.AddComponent(DebugLightEntity, new PointLight(
            new Vector3(1.0f, 1.0f, 0.8f),
            5.0f,
            30.0f
        ));
        World.AddComponent(DebugLightEntity, new MeshComponent(_smallSphereMesh));
        World.AddComponent(DebugLightEntity, new StandardMaterial
        {
            BaseColor = new Vector4(1f, 1f, 0.5f, 1f),
            Metallic = 0f,
            Roughness = 1f,
            AmbientOcclusion = 1f
        });
    }

    private void CreateEntities()
    {
        SpawnStaticBox(new Vector3(0, -2, 0), new Vector3(20f, 1f, 20f), _platformMesh, Materials.Concrete);

        if (_foxModel is { Success: true })
        {
            SpawnFoxModels();
        }

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

    private void SpawnFoxModels()
    {
        if (_foxModel is not { Success: true })
        {
            return;
        }

        var foxMaterial = new StandardMaterial
        {
            BaseColor = new Vector4(1f, 1f, 1f, 1f),
            Metallic = 0f,
            Roughness = 0.8f,
            AmbientOcclusion = 1f,
            AlbedoTexture = _foxTexture
        };

        var position = new Vector3(-4f, -1.5f, 0f);
        var skin = _foxModel.Skins.FirstOrDefault();
        var skeletonRootTransform = skin?.SkeletonRootTransform ?? Matrix4x4.Identity;

        var builder = new SceneHierarchyBuilder(World, Scene);
        _foxHierarchy = builder.Build(
            _foxModel,
            position,
            meshNodesOnly: true,
            configureNode: (entity, node, model) =>
            {
                if (node.MeshIndex.HasValue)
                {
                    World.AddComponent(entity, foxMaterial);

                    if (_foxSkeleton.IsValid && _animation.TryGetSkeleton(_foxSkeleton, out var skeleton))
                    {
                        var animator = new AnimatorComponent(_foxSkeleton)
                        {
                            CurrentAnimation = _foxAnimation,
                            IsPlaying = true,
                            Loop = true,
                            PlaybackSpeed = 1.0f
                        };
                        World.AddComponent(entity, animator);

                        var numJoints = skeleton.NumJoints;
                        var inverseBindMatrices = skin?.InverseBindMatrices ?? model.InverseBindMatrices;
                        var boneMatrices = new BoneMatricesComponent(numJoints, inverseBindMatrices, skeletonRootTransform);
                        World.AddComponent(entity, boneMatrices);
                    }
                }
            }
        );
    }

    public void AddCube()
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

    public void Add100Cubes()
    {
        for (var i = 0; i < 100; i++)
        {
            AddCube();
        }
    }

    private Entity SpawnStaticBox(Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material)
    {
        var entity = Scene.Spawn();
        World.AddComponent(entity, new MeshComponent(mesh));
        World.AddComponent(entity, new Transform(position, Quaternion.Identity, Vector3.One));
        World.AddComponent(entity, material);

        var handle = _physics.CreateStaticBody(entity, position, Quaternion.Identity, PhysicsShape.Box(size));
        World.AddComponent(entity, new StaticBody(handle));

        return entity;
    }

    private Entity SpawnDynamicBox(Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material,
        Quaternion? rotation = null, float mass = 1f)
    {
        var rot = rotation ?? Quaternion.Identity;
        var entity = Scene.Spawn();
        World.AddComponent(entity, new MeshComponent(mesh));
        World.AddComponent(entity, new Transform(position, rot, Vector3.One));
        World.AddComponent(entity, material);

        var handle = _physics.CreateBody(entity, position, rot, PhysicsBodyDesc.Dynamic(PhysicsShape.Box(size), mass));
        World.AddComponent(entity, new RigidBody(handle));

        return entity;
    }

    private Entity SpawnDynamicSphere(Vector3 position, float diameter, RuntimeMeshHandle mesh,
        StandardMaterial material, float mass = 1f)
    {
        var entity = Scene.Spawn();
        World.AddComponent(entity, new MeshComponent(mesh));
        World.AddComponent(entity, new Transform(position, Quaternion.Identity, Vector3.One));
        World.AddComponent(entity, material);

        var handle = _physics.CreateBody(entity, position, Quaternion.Identity,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(diameter), mass));
        World.AddComponent(entity, new RigidBody(handle));

        return entity;
    }

    public Entity DebugLightEntity { get; private set; }
}