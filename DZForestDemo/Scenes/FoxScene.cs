using System.Numerics;
using DenOfIz;
using ECS;
using ECS.Components;
using Flecs.NET.Core;
using Graphics;
using Physics;
using Physics.Components;
using RuntimeAssets;
using RuntimeAssets.Components;

namespace DZForestDemo.Scenes;

public class FoxSceneAssets : IDisposable
{
    public RuntimeMeshHandle CubeMesh;
    public RuntimeMeshHandle PlatformMesh;
    public RuntimeMeshHandle SphereMesh;
    public RuntimeMeshHandle SmallSphereMesh;
    public ModelLoadResult? FoxModel;
    public RuntimeTextureHandle FoxTexture;
    public RuntimeSkeletonHandle FoxSkeleton;
    public RuntimeAnimationHandle FoxAnimation;
    public Entity DebugLightEntity;

    private readonly AssetResource _assets;
    private readonly AnimationResource _animation;
    private readonly GraphicsResource _graphics;
    private bool _disposed;

    public FoxSceneAssets(World world)
    {
        _assets = world.Get<AssetResource>();
        _animation = world.Get<AnimationResource>();
        _graphics = world.Get<GraphicsResource>();

        _assets.BeginUpload();

        CubeMesh = _assets.AddBox(1.0f, 1.0f, 1.0f);
        PlatformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        SphereMesh = _assets.AddSphere(1.0f);
        SmallSphereMesh = _assets.AddSphere(0.3f, 8);

        FoxModel = _assets.AddModel("Fox.glb");
        if (!FoxModel.Success)
        {
            Console.WriteLine($"Failed to load Fox model: {FoxModel.ErrorMessage}");
        }

        FoxTexture = _assets.AddTexture("Fox_Texture.dztex");

        _assets.EndUpload();

        FoxSkeleton = _animation.LoadSkeleton("Fox_skeleton.ozz");
        if (FoxSkeleton.IsValid)
        {
            FoxAnimation = _animation.LoadAnimation(FoxSkeleton, "Fox_Run.ozz");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _graphics.WaitIdle();

        if (CubeMesh.IsValid)
        {
            _assets.RemoveMesh(CubeMesh);
        }

        if (PlatformMesh.IsValid)
        {
            _assets.RemoveMesh(PlatformMesh);
        }

        if (SphereMesh.IsValid)
        {
            _assets.RemoveMesh(SphereMesh);
        }

        if (SmallSphereMesh.IsValid)
        {
            _assets.RemoveMesh(SmallSphereMesh);
        }

        if (FoxModel is { Success: true })
        {
            foreach (var meshHandle in FoxModel.MeshHandles)
            {
                if (meshHandle.IsValid)
                {
                    _assets.RemoveMesh(meshHandle);
                }
            }
        }

        if (FoxTexture.IsValid)
        {
            _assets.RemoveTexture(FoxTexture);
        }
    }
}

public static class FoxScene
{
    private static readonly StandardMaterial[] MaterialPalette =
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

    private static readonly Random Random = new();

    public static void Register(World world)
    {
        world.Observer("FoxScene.OnEnter")
            .With<ActiveScene, FoxSceneTag>()
            .Event(Ecs.OnAdd)
            .Each((Entity _) => OnEnter(world));

        world.Observer("FoxScene.OnExit")
            .With<ActiveScene, FoxSceneTag>()
            .Event(Ecs.OnRemove)
            .Each((Entity _) => OnExit(world));
    }

    private static void OnEnter(World world)
    {
        world.DeleteWith(Ecs.ChildOf, world.Entity<SceneRoot>());

        var assets = new FoxSceneAssets(world);
        world.Set(assets);

        CreateLights(world, assets);
        CreateEntities(world, assets);
    }

    private static void OnExit(World world)
    {
        if (world.Has<FoxSceneAssets>())
        {
            world.Get<FoxSceneAssets>().Dispose();
            world.Remove<FoxSceneAssets>();
        }
    }

    private static void CreateLights(World world, FoxSceneAssets assets)
    {
        var sunEntity = world.Entity().ChildOf<SceneRoot>();
        sunEntity.Set(new DirectionalLight(
            new Vector3(0.4f, -0.8f, 0.3f),
            new Vector3(1.0f, 0.95f, 0.9f),
            0.6f
        ));

        var ambientEntity = world.Entity().ChildOf<SceneRoot>();
        ambientEntity.Set(new AmbientLight(
            new Vector3(0.5f, 0.6f, 0.7f),
            new Vector3(0.25f, 0.2f, 0.15f),
            0.4f
        ));

        var pointLight1 = world.Entity().ChildOf<SceneRoot>();
        pointLight1.Set(new Transform(new Vector3(6, 6, 6)));
        pointLight1.Set(new PointLight(
            new Vector3(1.0f, 0.7f, 0.4f),
            2.5f,
            18.0f
        ));

        var pointLight2 = world.Entity().ChildOf<SceneRoot>();
        pointLight2.Set(new Transform(new Vector3(-6, 5, -4)));
        pointLight2.Set(new PointLight(
            new Vector3(0.4f, 0.6f, 1.0f),
            2.0f,
            15.0f
        ));

        var pointLight3 = world.Entity().ChildOf<SceneRoot>();
        pointLight3.Set(new Transform(new Vector3(-5, 4, 6)));
        pointLight3.Set(new PointLight(
            new Vector3(0.9f, 0.3f, 0.5f),
            1.8f,
            14.0f
        ));

        var pointLight4 = world.Entity().ChildOf<SceneRoot>();
        pointLight4.Set(new Transform(new Vector3(5, 3, -5)));
        pointLight4.Set(new PointLight(
            new Vector3(0.4f, 0.9f, 0.5f),
            1.6f,
            12.0f
        ));

        assets.DebugLightEntity = world.Entity().ChildOf<SceneRoot>();
        assets.DebugLightEntity.Set(new Transform(new Vector3(0, 10, 0)));
        assets.DebugLightEntity.Set(new PointLight(
            new Vector3(1.0f, 1.0f, 0.8f),
            5.0f,
            30.0f
        ));
        assets.DebugLightEntity.Set(new MeshComponent(assets.SmallSphereMesh));
        assets.DebugLightEntity.Set(new StandardMaterial
        {
            BaseColor = new Vector4(1f, 1f, 0.5f, 1f),
            Metallic = 0f,
            Roughness = 1f,
            AmbientOcclusion = 1f
        });
    }

    private static void CreateEntities(World world, FoxSceneAssets assets)
    {
        var physics = world.Get<PhysicsResource>();

        SpawnStaticBox(world, physics, new Vector3(0, -2, 0), new Vector3(20f, 1f, 20f), assets.PlatformMesh, Materials.Concrete);

        if (assets.FoxModel is { Success: true })
        {
            SpawnFoxModels(world, assets);
        }

        for (var i = 0; i < 5; i++)
        {
            var position = new Vector3(
                (Random.NextSingle() - 0.5f) * 4f,
                5f + i * 2f,
                (Random.NextSingle() - 0.5f) * 4f
            );
            var material = MaterialPalette[Random.Next(MaterialPalette.Length)];
            SpawnDynamicBox(world, physics, position, Vector3.One, assets.CubeMesh, material);
        }
    }

    private static void SpawnFoxModels(World world, FoxSceneAssets assets)
    {
        if (assets.FoxModel is not { Success: true })
        {
            return;
        }

        var animation = world.Get<AnimationResource>();

        var foxMaterial = new StandardMaterial
        {
            BaseColor = new Vector4(1f, 1f, 1f, 1f),
            Metallic = 0f,
            Roughness = 0.8f,
            AmbientOcclusion = 1f,
            AlbedoTexture = assets.FoxTexture
        };

        var position = new Vector3(-4f, -1.5f, 0f);
        var skin = assets.FoxModel.Skins.FirstOrDefault();
        var skeletonRootTransform = skin?.SkeletonRootTransform ?? Matrix4x4.Identity;

        foreach (var meshHandle in assets.FoxModel.MeshHandles)
        {
            var entity = world.Entity().ChildOf<SceneRoot>();
            entity.Set(new MeshComponent(meshHandle));
            entity.Set(new Transform(position));
            entity.Set(foxMaterial);

            if (!assets.FoxSkeleton.IsValid || !animation.TryGetSkeleton(assets.FoxSkeleton, out var skeleton))
            {
                continue;
            }

            var animator = new AnimatorComponent(assets.FoxSkeleton)
            {
                CurrentAnimation = assets.FoxAnimation,
                IsPlaying = true,
                Loop = true,
                PlaybackSpeed = 1.0f
            };
            entity.Set(animator);

            var numJoints = skeleton.NumJoints;
            var inverseBindMatrices = skin?.InverseBindMatrices ?? assets.FoxModel.InverseBindMatrices;
            var boneMatrices = new BoneMatricesComponent(numJoints, inverseBindMatrices, skeletonRootTransform);
            entity.Set(boneMatrices);
        }
    }

    public static void AddCube(World world)
    {
        if (!world.Has<FoxSceneAssets>())
        {
            return;
        }

        var assets = world.Get<FoxSceneAssets>();
        var physics = world.Get<PhysicsResource>();

        var position = new Vector3(
            (Random.NextSingle() - 0.5f) * 6f,
            10f + Random.NextSingle() * 5f,
            (Random.NextSingle() - 0.5f) * 6f
        );

        var rotation = Quaternion.CreateFromYawPitchRoll(
            Random.NextSingle() * MathF.PI * 2,
            Random.NextSingle() * MathF.PI * 2,
            Random.NextSingle() * MathF.PI * 2
        );

        var material = MaterialPalette[Random.Next(MaterialPalette.Length)];
        if (Random.NextSingle() > 0.5f)
        {
            SpawnDynamicBox(world, physics, position, Vector3.One, assets.CubeMesh, material, rotation);
        }
        else
        {
            var sphereMaterial = material with { Roughness = 0.2f, Metallic = 0.3f };
            SpawnDynamicSphere(world, physics, position, 1f, assets.SphereMesh, sphereMaterial);
        }
    }

    public static void Add100Cubes(World world)
    {
        for (var i = 0; i < 100; i++)
        {
            AddCube(world);
        }
    }

    private static Entity SpawnStaticBox(World world, PhysicsResource physics, Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material)
    {
        var entity = world.Entity().ChildOf<SceneRoot>();
        entity.Set(new MeshComponent(mesh));
        entity.Set(new Transform(position, Quaternion.Identity, Vector3.One));
        entity.Set(material);

        var handle = physics.CreateStaticBody(entity, position, Quaternion.Identity, PhysicsShape.Box(size));
        entity.Set(new StaticBody(handle));

        return entity;
    }

    private static Entity SpawnDynamicBox(World world, PhysicsResource physics, Vector3 position, Vector3 size, RuntimeMeshHandle mesh, StandardMaterial material, Quaternion? rotation = null, float mass = 1f)
    {
        var rot = rotation ?? Quaternion.Identity;
        var entity = world.Entity().ChildOf<SceneRoot>();
        entity.Set(new MeshComponent(mesh));
        entity.Set(new Transform(position, rot, Vector3.One));
        entity.Set(material);

        var handle = physics.CreateBody(entity, position, rot, PhysicsBodyDesc.Dynamic(PhysicsShape.Box(size), mass));
        entity.Set(new RigidBody(handle));

        return entity;
    }

    private static Entity SpawnDynamicSphere(World world, PhysicsResource physics, Vector3 position, float diameter, RuntimeMeshHandle mesh, StandardMaterial material, float mass = 1f)
    {
        var entity = world.Entity().ChildOf<SceneRoot>();
        entity.Set(new MeshComponent(mesh));
        entity.Set(new Transform(position, Quaternion.Identity, Vector3.One));
        entity.Set(material);

        var handle = physics.CreateBody(entity, position, Quaternion.Identity, PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(diameter), mass));
        entity.Set(new RigidBody(handle));

        return entity;
    }
}
