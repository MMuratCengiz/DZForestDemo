using System.Numerics;
using Application;
using DenOfIz;
using DenOfIz.World;
using DenOfIz.World.Light;
using Graphics.Batching;
using Physics;
using RuntimeAssets;

namespace DZForestDemo;

public sealed class DemoGame : IGame
{
    private Game _game = null!;
    private Camera _cameraController = null!;
    private CameraObject _cameraObject = null!;
    private DemoRenderer? _renderer;

    private Scene? _foxScene;

    private RuntimeMeshHandle _cubeMesh;
    private RuntimeMeshHandle _platformMesh;
    private RuntimeMeshHandle _sphereMesh;
    private RuntimeMeshHandle _smallSphereMesh;
    private RuntimeMeshHandle _foxMesh;
    private RuntimeTextureHandle _foxTexture;
    private RuntimeSkeletonHandle _foxSkeleton;
    private RuntimeAnimationHandle _foxAnimation;

    private readonly List<RenderObjectData> _renderObjects = [];
    private readonly Dictionary<int, int> _sceneObjectToRenderIndex = new();
    private readonly Dictionary<int, AnimatorInstance> _animators = new();

    private readonly RenderMaterial[] _materialPalette =
    [
        new() { BaseColor = new Vector4(0.8f, 0.2f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.2f, 0.8f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.2f, 0.2f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.9f, 0.9f, 0.2f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(1.0f, 0.5f, 0.0f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.6f, 0.2f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.2f, 0.8f, 0.8f, 1f), Metallic = 0f, Roughness = 0.5f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.6f, 0.4f, 0.2f, 1f), Metallic = 0f, Roughness = 0.7f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.8f, 0.8f, 0.8f, 1f), Metallic = 0.9f, Roughness = 0.1f, AmbientOcclusion = 1f },
        new() { BaseColor = new Vector4(0.7f, 0.4f, 0.3f, 1f), Metallic = 0.8f, Roughness = 0.2f, AmbientOcclusion = 1f },
    ];

    private readonly Random _random = new();

    public PhysicsWorld? Physics { get; private set; }
    public AssetManager? Assets { get; private set; }
    public AnimationManager? Animation { get; private set; }

    public IReadOnlyList<RenderObjectData> RenderObjects => _renderObjects;

    public void SetRenderer(DemoRenderer renderer)
    {
        _renderer = renderer;
    }

    public void OnLoad(Game game)
    {
        _game = game;

        _cameraController = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _cameraController.SetAspectRatio(game.Graphics.Width, game.Graphics.Height);

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

        _foxMesh = Assets.AddMesh("fox.dzmesh");
        if (!_foxMesh.IsValid)
        {
            Console.WriteLine("Failed to load Fox mesh");
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
            _sceneObjectToRenderIndex.Clear();

            CreateCamera(scene);
            CreateLights(scene);
            CreateGameObjects(scene);
        };

        scene.OnUnload = () =>
        {
            foreach (var animator in _animators.Values)
            {
                Animation?.RemoveAnimator(animator);
            }
            _animators.Clear();
            _renderObjects.Clear();
            _sceneObjectToRenderIndex.Clear();
            scene.Clear();
        };

        return scene;
    }

    private void CreateCamera(Scene scene)
    {
        _cameraObject = scene.CreateObject<CameraObject>("MainCamera");
        _cameraObject.LocalPosition = _cameraController.Position;
        _cameraObject.FieldOfView = _cameraController.FieldOfView;
        _cameraObject.NearPlane = _cameraController.NearPlane;
        _cameraObject.FarPlane = _cameraController.FarPlane;
        _cameraObject.AspectRatio = _cameraController.AspectRatio;

        scene.MainCamera = _cameraObject;
    }

    private void CreateLights(Scene scene)
    {
        var sun = scene.CreateObject<DirectionalLight>("Sun");
        sun.LookAt(Vector3.Normalize(new Vector3(0.4f, -0.8f, 0.3f)));
        sun.Color = new Vector3(1.0f, 0.95f, 0.9f);
        sun.Intensity = 0.6f;
        sun.CastsShadows = true;

        var pointLight1 = scene.CreateObject<PointLight>("PointLight_Warm");
        pointLight1.LocalPosition = new Vector3(6, 6, 6);
        pointLight1.Color = new Vector3(1.0f, 0.7f, 0.4f);
        pointLight1.Intensity = 2.5f;
        pointLight1.Range = 18.0f;

        // Point light 2 - cool blue
        var pointLight2 = scene.CreateObject<PointLight>("PointLight_Blue");
        pointLight2.LocalPosition = new Vector3(-6, 5, -4);
        pointLight2.Color = new Vector3(0.4f, 0.6f, 1.0f);
        pointLight2.Intensity = 2.0f;
        pointLight2.Range = 15.0f;

        // Point light 3 - pink
        var pointLight3 = scene.CreateObject<PointLight>("PointLight_Pink");
        pointLight3.LocalPosition = new Vector3(-5, 4, 6);
        pointLight3.Color = new Vector3(0.9f, 0.3f, 0.5f);
        pointLight3.Intensity = 1.8f;
        pointLight3.Range = 14.0f;

        // Point light 4 - green
        var pointLight4 = scene.CreateObject<PointLight>("PointLight_Green");
        pointLight4.LocalPosition = new Vector3(5, 3, -5);
        pointLight4.Color = new Vector3(0.4f, 0.9f, 0.5f);
        pointLight4.Intensity = 1.6f;
        pointLight4.Range = 12.0f;
    }

    private void CreateGameObjects(Scene scene)
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

        if (_foxMesh.IsValid)
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
        if (!_foxMesh.IsValid)
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

        var fox = scene.CreateObject("Fox");
        fox.LocalPosition = new Vector3(-4f, -1.5f, 0f);

        AnimatorInstance? animator = null;
        if (_foxSkeleton.IsValid)
        {
            animator = Animation!.CreateAnimator(_foxSkeleton, null, Matrix4x4.Identity);
            animator.CurrentAnimation = _foxAnimation;
            animator.IsPlaying = true;
            animator.Loop = true;
            _animators[fox.Id] = animator;
        }

        AddRenderObject(fox, _foxMesh, foxMaterial, RenderFlags.CastsShadow | RenderFlags.Skinned, animator);
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

    private void AddRenderObject(GameObject obj, RuntimeMeshHandle mesh, RenderMaterial material,
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
        _cameraController.Update(dt);
        Animation?.Update(dt);

        SyncCameraToSceneObject();
        SyncPhysicsToSceneObjects();
        SyncSceneObjectsToRenderData();
    }

    private void SyncCameraToSceneObject()
    {
        if (_cameraObject == null)
        {
            return;
        }

        // Sync controller state to the scene camera object
        _cameraObject.LocalPosition = _cameraController.Position;
        _cameraObject.SetYawPitch(
            MathF.Atan2(_cameraController.Forward.X, _cameraController.Forward.Z),
            MathF.Asin(Math.Clamp(_cameraController.Forward.Y, -1f, 1f))
        );
        _cameraObject.FieldOfView = _cameraController.FieldOfView;
        _cameraObject.AspectRatio = _cameraController.AspectRatio;
        _cameraObject.NearPlane = _cameraController.NearPlane;
        _cameraObject.FarPlane = _cameraController.FarPlane;
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

    private void SyncPhysicsRecursive(GameObject obj)
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

    private void SyncRenderDataRecursive(GameObject obj)
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
        _cameraController.HandleEvent(ev);

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            var width = (uint)ev.Window.Data1;
            var height = (uint)ev.Window.Data2;
            _cameraController.SetAspectRatio(width, height);
            _cameraObject?.SetAspectRatio(width, height);
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
