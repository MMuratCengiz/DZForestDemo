using System.Numerics;
using DenOfIz;
using ECS;
using ECS.Components;
using Graphics;
using Physics;
using Physics.Components;
using RuntimeAssets;
using RuntimeAssets.Components;

namespace DZForestDemo.Scenes;

public sealed class VikingScene : GameSceneBase
{
    private AssetResource _assets = null!;
    private AnimationResource _animation = null!;
    private PhysicsResource _physics = null!;
    private GraphicsResource _graphics = null!;

    private RuntimeMeshHandle _platformMesh;
    private RuntimeMeshHandle _smallSphereMesh;
    private ModelLoadResult? _vikingModel;
    private RuntimeTextureHandle _vikingTexture;
    private RuntimeSkeletonHandle _vikingSkeleton;
    private RuntimeAnimationHandle _vikingAnimation;

    private Entity _debugLightEntity;
    private bool _assetsLoaded;

    public Action<Texture?>? OnTextureLoaded { get; set; }

    public override string Name => "VikingScene";

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

        if (_platformMesh.IsValid)
        {
            _assets.RemoveMesh(_platformMesh);
            _platformMesh = default;
        }
        if (_smallSphereMesh.IsValid)
        {
            _assets.RemoveMesh(_smallSphereMesh);
            _smallSphereMesh = default;
        }
        if (_vikingModel is { Success: true })
        {
            foreach (var meshHandle in _vikingModel.MeshHandles)
            {
                if (meshHandle.IsValid)
                {
                    _assets.RemoveMesh(meshHandle);
                }
            }
            _vikingModel = null;
        }
        if (_vikingTexture.IsValid)
        {
            _assets.RemoveTexture(_vikingTexture);
            _vikingTexture = default;
        }

        _assetsLoaded = false;
    }

    private void LoadAssets()
    {
        _assets.BeginUpload();

        _platformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        _smallSphereMesh = _assets.AddSphere(0.3f, 8);

        _vikingModel = _assets.AddModel("VikingRealm_Characters.glb");
        if (!_vikingModel.Success)
        {
            Console.WriteLine($"Failed to load Viking model: {_vikingModel.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"Loaded Viking model: {_vikingModel.MeshHandles.Count} meshes, {_vikingModel.Materials.Count} materials");
        }

        _vikingTexture = _assets.AddTexture("VikingRealm_Texture_01_A_PolygonVikingRealm_Texture_01_A.dztex");

        _assets.EndUpload();

        _assetsLoaded = true;

        if (_vikingTexture.IsValid && _assets.TryGetTexture(_vikingTexture, out var texture))
        {
            OnTextureLoaded?.Invoke(texture.Resource);
        }
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

        _debugLightEntity = Scene.Spawn();
        World.AddComponent(_debugLightEntity, new Transform(new Vector3(0, 10, 0)));
        World.AddComponent(_debugLightEntity, new PointLight(
            new Vector3(1.0f, 1.0f, 0.8f),
            5.0f,
            30.0f
        ));
        World.AddComponent(_debugLightEntity, new MeshComponent(_smallSphereMesh));
        World.AddComponent(_debugLightEntity, new StandardMaterial
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

        if (_vikingModel is { Success: true })
        {
            SpawnVikingModels();
        }
    }

    private void SpawnVikingModels()
    {
        if (_vikingModel == null || !_vikingModel.Success)
        {
            return;
        }

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
            var positionIndex = i % meshPositions.Length;
            var position = meshPositions[positionIndex];
            var rotation = meshRotations[positionIndex];

            if (i >= meshPositions.Length)
            {
                var row = i / meshPositions.Length;
                position += new Vector3(0f, 0f, row * 6f);
            }

            StandardMaterial material;
            if (i < _vikingModel.Materials.Count)
            {
                var matData = _vikingModel.Materials[i];
                material = new StandardMaterial
                {
                    BaseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
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

            var entity = Scene.Spawn();
            World.AddComponent(entity, new MeshComponent(meshHandle));
            World.AddComponent(entity, new Transform(position, rotation, modelScale));
            World.AddComponent(entity, material);

            if (_vikingSkeleton.IsValid && _animation.TryGetSkeleton(_vikingSkeleton, out var skeleton))
            {
                var animator = new AnimatorComponent(_vikingSkeleton)
                {
                    CurrentAnimation = _vikingAnimation,
                    IsPlaying = true,
                    Loop = true,
                    PlaybackSpeed = 1.0f + (i * 0.1f)
                };
                World.AddComponent(entity, animator);

                var numJoints = skeleton.NumJoints;
                var boneMatrices = new BoneMatricesComponent(numJoints, _vikingModel.InverseBindMatrices);
                World.AddComponent(entity, boneMatrices);
            }
        }

        Console.WriteLine($"Spawned {_vikingModel.MeshHandles.Count} Viking mesh entities");
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

    public Entity DebugLightEntity => _debugLightEntity;
}
