using System.Numerics;
using DZForestDemo.GameObjects;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Light;
using NiziKit.Physics;

namespace DZForestDemo.Scenes;

public class DemoScene() : Scene("Demo Scene")
{
    private class ColorMaterial : Material
    {
        private readonly ColorTexture _colorTexture;

        public ColorMaterial(string name, byte r, byte g, byte b)
        {
            Name = name;
            _colorTexture = new ColorTexture(GraphicsContext.Device, r, g, b, 255, name);
            Albedo = new Texture2d
            {
                Name = name,
                Width = 1,
                Height = 1,
                GpuTexture = _colorTexture.Texture
            };
            GpuShader = Assets.GetShader("Builtin/Shaders/Default");
        }

        public override void Dispose()
        {
            _colorTexture.Dispose();
            base.Dispose();
        }
    }

    private readonly Random _random = new();

    private Mesh _cubeMesh = null!;
    private Mesh _platformMesh = null!;
    private Mesh _sphereMesh = null!;
    private Material _cubeMaterial = null!;
    private Material _platformMaterial = null!;

    public Fox? Fox { get; private set; }
    public Fox? LayerBlendFox { get; private set; }

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

        _cubeMaterial = Assets.RegisterMaterial(new ColorMaterial("CubeMaterial", 200, 100, 50));
        _platformMaterial = Assets.RegisterMaterial(new ColorMaterial("PlatformMaterial", 100, 100, 100));
    }

    private void CreateCamera()
    {
        var camera = CreateObject<CameraObject>("Main Camera");
        camera.LocalPosition = new Vector3(0, 8, 25);
        camera.FieldOfView = MathF.PI / 4f;
        camera.NearPlane = 0.1f;
        camera.FarPlane = 1000f;

        var controller = camera.AddComponent<CameraController>();
        controller.LookAt(new Vector3(0, 0, 0));

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

        platform.AddComponent(new MeshComponent { Mesh = _platformMesh });
        platform.AddComponent(new MaterialComponent { Material = _platformMaterial });
        platform.AddComponent(RigidbodyComponent.Static(PhysicsShape.Box(new Vector3(20f, 1f, 20f))));
    }

    private void SpawnFox()
    {
        Fox = new Fox(new Vector3(-3f, 0f, 0f));
        Add(Fox);

        LayerBlendFox = new Fox(new Vector3(3f, 0f, 0f), useLayerBlending: true);
        Add(LayerBlendFox);
    }

    private void SpawnInitialCubes()
    {
        for (var i = 0; i < 20; i++)
        {
            var position = new Vector3(
                (_random.NextSingle() - 0.5f) * 8f,
                5f + i * 1.5f,
                (_random.NextSingle() - 0.5f) * 8f
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

        cube.AddComponent(new MeshComponent { Mesh = _cubeMesh });
        cube.AddComponent(new MaterialComponent { Material = _cubeMaterial });
        cube.AddComponent(RigidbodyComponent.Dynamic(PhysicsShape.Box(Vector3.One), 1f));
    }

    private void SpawnDynamicSphere(Vector3 position)
    {
        var sphere = CreateObject("Sphere");
        sphere.LocalPosition = position;

        sphere.AddComponent(new MeshComponent { Mesh = _sphereMesh });
        sphere.AddComponent(new MaterialComponent { Material = _cubeMaterial });
        sphere.AddComponent(RigidbodyComponent.Dynamic(PhysicsShape.Sphere(1f), 1f));
    }
}
