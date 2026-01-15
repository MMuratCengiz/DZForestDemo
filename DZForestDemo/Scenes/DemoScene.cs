using System.Numerics;
using DZForestDemo.GameObjects;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Light;
using NiziKit.Physics;

namespace DZForestDemo.Scenes;

public class DemoScene : Scene
{
    private readonly Random _random = new();

    private Mesh _cubeMesh = null!;
    private Mesh _platformMesh = null!;
    private Mesh _sphereMesh = null!;

    public DemoScene() : base("Demo Scene")
    {
    }

    public override void Load()
    {
        LoadAssets();
        CreateCamera();
        CreateLights();
        CreateGameObjects();
    }

    private void LoadAssets()
    {
        _cubeMesh = Assets.CreateBox(1.0f, 1.0f, 1.0f);
        _platformMesh = Assets.CreateBox(20.0f, 1.0f, 20.0f);
        _sphereMesh = Assets.CreateSphere(1.0f);
    }

    private void CreateCamera()
    {
        var camera = CreateObject<CameraObject>("Main Camera");
        camera.LocalPosition = new Vector3(0, 12, 25);
        camera.LookAt(new Vector3(0, 0, 0));
        camera.FieldOfView = MathF.PI / 4f;
        camera.NearPlane = 0.1f;
        camera.FarPlane = 1000f;
        MainCamera = camera;
    }

    private void CreateLights()
    {
        var sun = CreateObject<DirectionalLight>("Sun");
        sun.LookAt(Vector3.Normalize(new Vector3(0.4f, -0.8f, 0.3f)));
        sun.Color = new Vector3(1.0f, 0.95f, 0.9f);
        sun.Intensity = 0.6f;
        sun.CastsShadows = true;

        var pointLight1 = CreateObject<PointLight>("PointLight_Warm");
        pointLight1.LocalPosition = new Vector3(6, 6, 6);
        pointLight1.Color = new Vector3(1.0f, 0.7f, 0.4f);
        pointLight1.Intensity = 2.5f;
        pointLight1.Range = 18.0f;

        var pointLight2 = CreateObject<PointLight>("PointLight_Blue");
        pointLight2.LocalPosition = new Vector3(-6, 5, -4);
        pointLight2.Color = new Vector3(0.4f, 0.6f, 1.0f);
        pointLight2.Intensity = 2.0f;
        pointLight2.Range = 15.0f;

        var pointLight3 = CreateObject<PointLight>("PointLight_Pink");
        pointLight3.LocalPosition = new Vector3(-5, 4, 6);
        pointLight3.Color = new Vector3(0.9f, 0.3f, 0.5f);
        pointLight3.Intensity = 1.8f;
        pointLight3.Range = 14.0f;

        var pointLight4 = CreateObject<PointLight>("PointLight_Green");
        pointLight4.LocalPosition = new Vector3(5, 3, -5);
        pointLight4.Color = new Vector3(0.4f, 0.9f, 0.5f);
        pointLight4.Intensity = 1.6f;
        pointLight4.Range = 12.0f;
    }

    private void CreateGameObjects()
    {
        CreatePlatform();
        SpawnFox();
        SpawnInitialCubes();
    }

    private void CreatePlatform()
    {
        var platform = CreateObject("Platform");
        platform.LocalPosition = new Vector3(0, -2, 0);

        var mesh = platform.AddComponent<MeshComponent>();
        mesh.Mesh = _platformMesh;

        World.PhysicsWorld?.CreateStaticBody(
            platform.Id,
            platform.LocalPosition,
            Quaternion.Identity,
            PhysicsShape.Box(new Vector3(20f, 1f, 20f)));
    }

    private void SpawnFox()
    {
        Add(new Fox());
    }

    private void SpawnInitialCubes()
    {
        for (var i = 0; i < 5; i++)
        {
            var position = new Vector3(
                (_random.NextSingle() - 0.5f) * 4f,
                5f + i * 2f,
                (_random.NextSingle() - 0.5f) * 4f
            );
            SpawnDynamicCube(position);
        }
    }

    public void AddRandomShape()
    {
        var position = new Vector3(
            (_random.NextSingle() - 0.5f) * 6f,
            10f + _random.NextSingle() * 5f,
            (_random.NextSingle() - 0.5f) * 6f
        );

        if (_random.NextSingle() > 0.5f)
        {
            SpawnDynamicCube(position);
        }
        else
        {
            SpawnDynamicSphere(position);
        }
    }

    private void SpawnDynamicCube(Vector3 position)
    {
        var cube = CreateObject("Cube");
        cube.LocalPosition = position;
        cube.LocalRotation = Quaternion.CreateFromYawPitchRoll(
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2
        );

        var mesh = cube.AddComponent<MeshComponent>();
        mesh.Mesh = _cubeMesh;

        World.PhysicsWorld?.CreateBody(
            cube.Id,
            position,
            cube.LocalRotation,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Box(Vector3.One), 1f));
    }

    private void SpawnDynamicSphere(Vector3 position)
    {
        var sphere = CreateObject("Sphere");
        sphere.LocalPosition = position;

        var mesh = sphere.AddComponent<MeshComponent>();
        mesh.Mesh = _sphereMesh;

        World.PhysicsWorld?.CreateBody(
            sphere.Id,
            position,
            Quaternion.Identity,
            PhysicsBodyDesc.Dynamic(PhysicsShape.Sphere(1f), 1f));
    }
}
