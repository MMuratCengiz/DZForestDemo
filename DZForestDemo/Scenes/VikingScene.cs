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

public class VikingSceneAssets : IDisposable
{
    public RuntimeMeshHandle PlatformMesh;
    public RuntimeMeshHandle SmallSphereMesh;
    public ModelLoadResult? VikingModel;
    public RuntimeTextureHandle VikingTexture;
    public RuntimeSkeletonHandle VikingSkeleton;
    public RuntimeAnimationHandle VikingAnimation;
    public Entity DebugLightEntity;

    private readonly AssetResource _assets;
    private readonly GraphicsResource _graphics;
    private bool _disposed;

    public VikingSceneAssets(World world)
    {
        _assets = world.Get<AssetResource>();
        _graphics = world.Get<GraphicsResource>();

        _assets.BeginUpload();

        PlatformMesh = _assets.AddBox(20.0f, 1.0f, 20.0f);
        SmallSphereMesh = _assets.AddSphere(0.3f, 8);

        VikingModel = _assets.AddModel("VikingRealm_Characters.glb");
        if (!VikingModel.Success)
        {
            Console.WriteLine($"Failed to load Viking model: {VikingModel.ErrorMessage}");
        }
        else
        {
            Console.WriteLine($"Loaded Viking model: {VikingModel.MeshHandles.Count} meshes, {VikingModel.Materials.Count} materials");
        }

        VikingTexture = _assets.AddTexture("VikingRealm_Texture_01_A_PolygonVikingRealm_Texture_01_A.dztex");

        _assets.EndUpload();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _graphics.WaitIdle();

        if (PlatformMesh.IsValid)
        {
            _assets.RemoveMesh(PlatformMesh);
        }

        if (SmallSphereMesh.IsValid)
        {
            _assets.RemoveMesh(SmallSphereMesh);
        }

        if (VikingModel is { Success: true })
        {
            foreach (var meshHandle in VikingModel.MeshHandles)
            {
                if (meshHandle.IsValid)
                {
                    _assets.RemoveMesh(meshHandle);
                }
            }
        }

        if (VikingTexture.IsValid)
        {
            _assets.RemoveTexture(VikingTexture);
        }
    }
}

public static class VikingScene
{
    public static Action<Texture?>? OnTextureLoaded;

    public static void Register(World world)
    {
        world.Observer("VikingScene.OnEnter")
            .With<ActiveScene, VikingSceneTag>()
            .Event(Ecs.OnAdd)
            .Each((Entity _) => OnEnter(world));

        world.Observer("VikingScene.OnExit")
            .With<ActiveScene, VikingSceneTag>()
            .Event(Ecs.OnRemove)
            .Each((Entity _) => OnExit(world));
    }

    private static void OnEnter(World world)
    {
        world.DeleteWith(Ecs.ChildOf, world.Entity<SceneRoot>());

        var assets = new VikingSceneAssets(world);
        world.Set(assets);

        if (assets.VikingTexture.IsValid)
        {
            var assetResource = world.Get<AssetResource>();
            if (assetResource.TryGetTexture(assets.VikingTexture, out var texture))
            {
                OnTextureLoaded?.Invoke(texture.Resource);
            }
        }

        CreateLights(world, assets);
        CreateEntities(world, assets);
    }

    private static void OnExit(World world)
    {
        if (world.Has<VikingSceneAssets>())
        {
            world.Get<VikingSceneAssets>().Dispose();
            world.Remove<VikingSceneAssets>();
        }
    }

    private static void CreateLights(World world, VikingSceneAssets assets)
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

    private static void CreateEntities(World world, VikingSceneAssets assets)
    {
        var physics = world.Get<PhysicsResource>();

        SpawnStaticBox(world, physics, new Vector3(0, -2, 0), new Vector3(20f, 1f, 20f), assets.PlatformMesh, Materials.Concrete);

        if (assets.VikingModel is { Success: true })
        {
            SpawnVikingModels(world, assets);
        }
    }

    private static void SpawnVikingModels(World world, VikingSceneAssets assets)
    {
        if (assets.VikingModel == null || !assets.VikingModel.Success)
        {
            return;
        }

        var animation = world.Get<AnimationResource>();

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
            AlbedoTexture = assets.VikingTexture
        };

        for (var i = 0; i < assets.VikingModel.MeshHandles.Count; i++)
        {
            var meshHandle = assets.VikingModel.MeshHandles[i];
            var positionIndex = i % meshPositions.Length;
            var position = meshPositions[positionIndex];
            var rotation = meshRotations[positionIndex];

            if (i >= meshPositions.Length)
            {
                var row = i / meshPositions.Length;
                position += new Vector3(0f, 0f, row * 6f);
            }

            StandardMaterial material;
            if (i < assets.VikingModel.Materials.Count)
            {
                var matData = assets.VikingModel.Materials[i];
                material = new StandardMaterial
                {
                    BaseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                    Metallic = matData.Metallic,
                    Roughness = matData.Roughness,
                    AmbientOcclusion = 1.0f,
                    AlbedoTexture = assets.VikingTexture
                };
            }
            else
            {
                material = vikingMaterial;
            }

            var entity = world.Entity().ChildOf<SceneRoot>();
            entity.Set(new MeshComponent(meshHandle));
            entity.Set(new Transform(position, rotation, modelScale));
            entity.Set(material);

            if (assets.VikingSkeleton.IsValid && animation.TryGetSkeleton(assets.VikingSkeleton, out var skeleton))
            {
                var animator = new AnimatorComponent(assets.VikingSkeleton)
                {
                    CurrentAnimation = assets.VikingAnimation,
                    IsPlaying = true,
                    Loop = true,
                    PlaybackSpeed = 1.0f + (i * 0.1f)
                };
                entity.Set(animator);

                var numJoints = skeleton.NumJoints;
                var boneMatrices = new BoneMatricesComponent(numJoints, assets.VikingModel.InverseBindMatrices);
                entity.Set(boneMatrices);
            }
        }

        Console.WriteLine($"Spawned {assets.VikingModel.MeshHandles.Count} Viking mesh entities");
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
}
