using System.Numerics;
using Application;
using DenOfIz;
using DenOfIz.World;
using Graphics;
using Graphics.Batching;
using Physics;
using RuntimeAssets;
using RuntimeAssets.GltfModels;

namespace DZForestDemo;

public sealed class DemoGame : IGame
{
    private Game _game = null!;
    private Camera _camera = null!;
    private DemoRenderer? _renderer;

    private Scene? _foxScene;

    private RuntimeMeshHandle _cubeMesh;
    private RuntimeMeshHandle _platformMesh;
    private RuntimeMeshHandle _sphereMesh;
    private RuntimeMeshHandle _smallSphereMesh;
    private ModelLoadResult? _foxModel;
    private RuntimeTextureHandle _foxTexture;
    private RuntimeSkeletonHandle _foxSkeleton;
    private RuntimeAnimationHandle _foxAnimation;

    private readonly List<RenderObjectData> _renderObjects = [];
    private readonly List<RenderLight> _lights = [];
    private readonly Dictionary<int, int> _sceneObjectToRenderIndex = new();
    private readonly Dictionary<int, AnimatorInstance> _animators = new();

    private readonly RenderMaterial[] _materialPalette =
    [
        new RenderMaterial { BaseColor = new Vector4(0.8f, 0.2f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.2f, 0.8f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.2f, 0.2f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.9f, 0.9f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(1.0f, 0.5f, 0.0f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.6f, 0.2f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.2f, 0.8f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.6f, 0.4f, 0.2f, 1f), Metallic = 0f, Roughness = 0.7f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.8f, 0.8f, 0.8f, 1f), Metallic = 0.9f, Roughness = 0.1f, AmbientOcclusion = 1f },
        new RenderMaterial { BaseColor = new Vector4(0.7f, 0.4f, 0.3f, 1f), Metallic = 0.8f, Roughness = 0.2f, AmbientOcclusion = 1f },
    ];

    private readonly Random _random = new();

    public Camera Camera => _camera;
    public PhysicsWorld? Physics { get; private set; }
    public AssetManager? Assets { get; private set; }
    public AnimationManager? Animation { get; private set; }

    public IReadOnlyList<RenderObjectData> RenderObjects => _renderObjects;
    public IReadOnlyList<RenderLight> Lights => _lights;

    public void SetRenderer(DemoRenderer renderer)
    {
        _renderer = renderer;
    }

    public void OnLoad(Game game)
    {
        _game = game;

        _camera = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _camera.SetAspectRatio(game.Graphics.Width, game.Graphics.Height);

        Physics = new PhysicsWorld();
        Assets = new AssetManager(game.Graphics.LogicalDevice);
        Animation = new AnimationManager();

        LoadAssets();
        _foxScene = CreateFoxScene();
        game.LoadScene(_foxScene);
    }

    private void LoadAssets()
    {
        Assets!.BeginUpload();

        _cubeMesh = Assets.AddBox(1.0f, 1.0f, 1.0f);
        _platformMesh = Assets.AddBox(20.0f, 1.0f, 20.0f);
        _sphereMesh = Assets.AddSphere(1.0f);
        _smallSphereMesh = Assets.AddSphere(0.3f, 8);

        _foxModel = Assets.AddModel("Fox.glb");
        if (!_foxModel.Success)
        {
            Console.WriteLine($"Failed to load Fox model: {_foxModel.ErrorMessage}");
        }

        _foxTexture = Assets.AddTexture("Fox_Texture.dztex");

        Assets.EndUpload();

        _foxSkeleton = Animation!.LoadSkeleton("Fox_skeleton.ozz");
        if (_foxSkeleton.IsValid)
        {
            _foxAnimation = Animation.LoadAnimation(_foxSkeleton, "Fox_Run.ozz");
        }
    }

    private Scene CreateFoxScene()
    {
        var scene = new Scene("Fox");

        scene.OnLoad = () =>
        {
            _renderObjects.Clear();
            _lights.Clear();
            _sceneObjectToRenderIndex.Clear();

            CreateLights();
            CreateSceneObjects(scene);
        };

        scene.OnUnload = () =>
        {
            foreach (var animator in _animators.Values)
            {
                Animation?.RemoveAnimator(animator);
            }
            _animators.Clear();
            _renderObjects.Clear();
            _lights.Clear();
            _sceneObjectToRenderIndex.Clear();
            scene.Clear();
        };

        return scene;
    }

    private void CreateLights()
    {
        _lights.Add(new RenderLight
        {
            Type = LightType.Directional,
            Direction = Vector3.Normalize(new Vector3(0.4f, -0.8f, 0.3f)),
            Color = new Vector3(1.0f, 0.95f, 0.9f),
            Intensity = 0.6f,
            CastsShadows = true
        });

        _lights.Add(new RenderLight
        {
            Type = LightType.Point,
            Position = new Vector3(6, 6, 6),
            Color = new Vector3(1.0f, 0.7f, 0.4f),
            Intensity = 2.5f,
            Range = 18.0f
        });

        _lights.Add(new RenderLight
        {
            Type = LightType.Point,
            Position = new Vector3(-6, 5, -4),
            Color = new Vector3(0.4f, 0.6f, 1.0f),
            Intensity = 2.0f,
            Range = 15.0f
        });

        _lights.Add(new RenderLight
        {
            Type = LightType.Point,
            Position = new Vector3(-5, 4, 6),
            Color = new Vector3(0.9f, 0.3f, 0.5f),
            Intensity = 1.8f,
            Range = 14.0f
        });

        _lights.Add(new RenderLight
        {
            Type = LightType.Point,
            Position = new Vector3(5, 3, -5),
            Color = new Vector3(0.4f, 0.9f, 0.5f),
            Intensity = 1.6f,
            Range = 12.0f
        });
    }

    private void CreateSceneObjects(Scene scene)
    {
        var platform = scene.CreateObject("Platform");
        platform.LocalPosition = new Vector3(0, -2, 0);
        AddRenderObject(platform, _platformMesh, new RenderMaterial
        {
            BaseColor = new Vector4(0.5f, 0.5f, 0.5f, 1f),
            Metallic = 0f,
            Roughness = 0.8f,
            AmbientOcclusion = 1f
        });
        Physics!.CreateStaticBody(platform.Id, platform.LocalPosition, Quaternion.Identity,
            PhysicsShape.Box(new Vector3(20f, 1f, 20f)));

        if (_foxModel is { Success: true })
        {
            SpawnFox(scene);
        }

        for (var i = 0; i < 5; i++)
        {
            var position = new Vector3(
                (_random.NextSingle() - 0.5f) * 4f,
                5f + i * 2f,
                (_random.NextSingle() - 0.5f) * 4f
            );
            var material = _materialPalette[_random.Next(_materialPalette.Length)];
            SpawnDynamicCube(scene, position, material);
        }
    }

    private void SpawnFox(Scene scene)
    {
        if (_foxModel is not { Success: true })
        {
            return;
        }

        var foxMaterial = new RenderMaterial
        {
            BaseColor = new Vector4(1f, 1f, 1f, 1f),
            Metallic = 0f,
            Roughness = 0.8f,
            AmbientOcclusion = 1f,
            AlbedoTexture = new TextureId(_foxTexture.Index, _foxTexture.Generation)
        };

        var skin = _foxModel.Skins.FirstOrDefault();
        var skeletonRootTransform = skin?.SkeletonRootTransform ?? Matrix4x4.Identity;
        var inverseBindMatrices = skin?.InverseBindMatrices ?? _foxModel.InverseBindMatrices;

        foreach (var meshHandle in _foxModel.MeshHandles)
        {
            var fox = scene.CreateObject("Fox");
            fox.LocalPosition = new Vector3(-4f, -1.5f, 0f);

            AnimatorInstance? animator = null;
            if (_foxSkeleton.IsValid)
            {
                animator = Animation!.CreateAnimator(_foxSkeleton, inverseBindMatrices?.ToArray(), skeletonRootTransform);
                animator.CurrentAnimation = _foxAnimation;
                animator.IsPlaying = true;
                animator.Loop = true;
                _animators[fox.Id] = animator;
            }

            AddRenderObject(fox, meshHandle, foxMaterial, RenderFlags.CastsShadow | RenderFlags.Skinned, animator);
        }
    }

    private void SpawnDynamicCube(Scene scene, Vector3 position, RenderMaterial material)
    {
        var cube = scene.CreateObject("Cube");
        cube.LocalPosition = position;
        cube.LocalRotation = Quaternion.CreateFromYawPitchRoll(
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2
        );

        AddRenderObject(cube, _cubeMesh, material, RenderFlags.CastsShadow);
        Physics!.CreateBody(cube.Id, position, cube.LocalRotation,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Box(Vector3.One), 1f));
    }

    public void AddCube()
    {
        if (_foxScene == null)
        {
            return;
        }

        var position = new Vector3(
            (_random.NextSingle() - 0.5f) * 6f,
            10f + _random.NextSingle() * 5f,
            (_random.NextSingle() - 0.5f) * 6f
        );

        var material = _materialPalette[_random.Next(_materialPalette.Length)];

        if (_random.NextSingle() > 0.5f)
        {
            SpawnDynamicCube(_foxScene, position, material);
        }
        else
        {
            SpawnDynamicSphere(_foxScene, position, material with { Roughness = 0.2f, Metallic = 0.3f });
        }
    }

    private void SpawnDynamicSphere(Scene scene, Vector3 position, RenderMaterial material)
    {
        var sphere = scene.CreateObject("Sphere");
        sphere.LocalPosition = position;

        AddRenderObject(sphere, _sphereMesh, material, RenderFlags.CastsShadow);
        Physics!.CreateBody(sphere.Id, position, Quaternion.Identity,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(1f), 1f));
    }

    public void Add100Cubes()
    {
        for (var i = 0; i < 100; i++)
        {
            AddCube();
        }
    }

    private void AddRenderObject(SceneObject obj, RuntimeMeshHandle mesh, RenderMaterial material,
        RenderFlags flags = RenderFlags.CastsShadow, AnimatorInstance? animator = null)
    {
        var index = _renderObjects.Count;
        _sceneObjectToRenderIndex[obj.Id] = index;

        _renderObjects.Add(new RenderObjectData
        {
            SceneObjectId = obj.Id,
            Mesh = new MeshId(mesh.Index, mesh.Generation),
            Material = material,
            Transform = obj.WorldMatrix,
            Flags = flags,
            Animator = animator
        });
    }

    public void OnUpdate(float dt)
    {
        _camera.Update(dt);
        Animation?.Update(dt);

        SyncPhysicsToSceneObjects();
        SyncSceneObjectsToRenderData();
    }

    private void SyncPhysicsToSceneObjects()
    {
        if (_game.ActiveScene == null || Physics == null)
        {
            return;
        }

        foreach (var obj in _game.ActiveScene.RootObjects)
        {
            SyncPhysicsRecursive(obj);
        }
    }

    private void SyncPhysicsRecursive(SceneObject obj)
    {
        var pose = Physics!.GetPose(obj.Id);
        if (pose.HasValue)
        {
            obj.LocalPosition = pose.Value.Position;
            obj.LocalRotation = pose.Value.Rotation;
        }

        foreach (var child in obj.Children)
        {
            SyncPhysicsRecursive(child);
        }
    }

    private void SyncSceneObjectsToRenderData()
    {
        if (_game.ActiveScene == null)
        {
            return;
        }

        foreach (var obj in _game.ActiveScene.RootObjects)
        {
            SyncRenderDataRecursive(obj);
        }
    }

    private void SyncRenderDataRecursive(SceneObject obj)
    {
        if (_sceneObjectToRenderIndex.TryGetValue(obj.Id, out var index))
        {
            var data = _renderObjects[index];
            data.Transform = obj.WorldMatrix;
            _renderObjects[index] = data;
        }

        foreach (var child in obj.Children)
        {
            SyncRenderDataRecursive(child);
        }
    }

    public void OnFixedUpdate(float fixedDt)
    {
        Physics?.Step(fixedDt);
    }

    public void OnRender()
    {
    }

    public void OnEvent(ref Event ev)
    {
        _renderer?.HandleEvent(ev);
        _camera.HandleEvent(ev);

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            _camera.SetAspectRatio((uint)ev.Window.Data1, (uint)ev.Window.Data2);
        }

        if (ev.Type == EventType.KeyDown)
        {
            switch (ev.Key.KeyCode)
            {
                case KeyCode.Space:
                    AddCube();
                    break;
                case KeyCode.Escape:
                    _game.Quit();
                    break;
            }
        }
    }

    public void OnShutdown()
    {
        foreach (var animator in _animators.Values)
        {
            Animation?.RemoveAnimator(animator);
        }
        _animators.Clear();

        Physics?.Dispose();
        Assets?.Dispose();
        Animation?.Dispose();
    }
}

public struct RenderObjectData
{
    public int SceneObjectId;
    public MeshId Mesh;
    public RenderMaterial Material;
    public Matrix4x4 Transform;
    public RenderFlags Flags;
    public AnimatorInstance? Animator;
}
