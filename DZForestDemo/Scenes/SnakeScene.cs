using System.Numerics;
using DenOfIz;
using DZForestDemo.GameObjects;
using DZForestDemo.Graphics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Light;
using NiziKit.Physics;

namespace DZForestDemo.Scenes;

public class SnakeScene() : Scene("Snake Scene")
{
    private const float SegmentSize = 1f;
    private const int ArenaSize = 15;

    private Mesh _cubeMesh = null!;
    private Mesh _sphereMesh = null!;
    private GlowingFoodMaterial _foodMaterial = null!;

    private Snake? _snake;

    public Snake? Snake => _snake;

    public override void Load()
    {
        LoadAssets();
        CreateCamera();
        CreateLights();
        CreateArena();
        CreateSnake();
        SpawnFood();
    }

    private void LoadAssets()
    {
        _cubeMesh = Assets.CreateBox(SegmentSize, SegmentSize, SegmentSize);
        _sphereMesh = Assets.CreateSphere(SegmentSize * 0.8f);
    }

    private void CreateCamera()
    {
        var camera = CreateObject<CameraObject>("Main Camera");
        camera.LocalPosition = new Vector3(0, 30, -25);
        camera.FieldOfView = MathF.PI / 4f;
        camera.NearPlane = 0.1f;
        camera.FarPlane = 1000f;

        var controller = new CameraController
        {
            Mode = CameraMode.Orbit,
            OrbitTarget = Vector3.Zero,
            OrbitDistance = Vector3.Distance(camera.LocalPosition, Vector3.Zero)
        };
        camera.AddComponent(controller);
        controller.SetPositionAndLookAt(camera.LocalPosition, Vector3.Zero, immediate: true);

        MainCamera = camera;
    }

    private void CreateLights()
    {
        var sun = CreateObject<DirectionalLight>("Sun");
        sun.LookAt(Vector3.Normalize(new Vector3(0.3f, -0.8f, 0.4f)));
        sun.Color = new Vector3(1.0f, 0.95f, 0.9f);
        sun.Intensity = 0.7f;
        sun.CastsShadows = true;

        var positions = new[]
        {
            new Vector3(-ArenaSize, 8, -ArenaSize),
            new Vector3(ArenaSize, 8, -ArenaSize),
            new Vector3(-ArenaSize, 8, ArenaSize),
            new Vector3(ArenaSize, 8, ArenaSize)
        };

        var colors = new[]
        {
            new Vector3(0.4f, 0.6f, 1.0f),
            new Vector3(1.0f, 0.5f, 0.3f),
            new Vector3(0.5f, 1.0f, 0.5f),
            new Vector3(1.0f, 0.3f, 0.6f)
        };

        for (var i = 0; i < 4; i++)
        {
            var light = CreateObject<PointLight>($"CornerLight_{i}");
            light.LocalPosition = positions[i];
            light.Color = colors[i];
            light.Intensity = 1.5f;
            light.Range = 25f;
        }
    }

    private void CreateArena()
    {
        var wallMaterial = new SnakeMaterial("Wall", 60, 60, 70);
        var gridTexture = new GridTexture(
            cellSize: 8,
            gridSize: ArenaSize * 2 + 2,
            bgR: 35, bgG: 40, bgB: 50,
            lineR: 55, lineG: 65, lineB: 80,
            lineWidth: 1,
            debugName: "FloorGrid"
        );
        var floorMaterial = new GridMaterial("Floor", gridTexture);
        Assets.RegisterMaterial(wallMaterial);
        Assets.RegisterMaterial(floorMaterial);

        var floor = CreateObject("Floor");
        floor.LocalPosition = new Vector3(0, -SegmentSize, 0);
        floor.LocalScale = new Vector3(ArenaSize * 2 + 2, 0.5f, ArenaSize * 2 + 2);
        floor.AddComponent(new MeshComponent { Mesh = _cubeMesh });
        floor.AddComponent(new MaterialComponent { Material = floorMaterial });
        floor.AddComponent(RigidbodyComponent.Static(PhysicsShape.Box(new Vector3(ArenaSize * 2 + 2, 0.5f, ArenaSize * 2 + 2))));

        CreateWall("WallNorth", new Vector3(0, 0, -ArenaSize - 1), new Vector3(ArenaSize * 2 + 2, SegmentSize * 2, SegmentSize), wallMaterial);
        CreateWall("WallSouth", new Vector3(0, 0, ArenaSize + 1), new Vector3(ArenaSize * 2 + 2, SegmentSize * 2, SegmentSize), wallMaterial);
        CreateWall("WallWest", new Vector3(-ArenaSize - 1, 0, 0), new Vector3(SegmentSize, SegmentSize * 2, ArenaSize * 2 + 2), wallMaterial);
        CreateWall("WallEast", new Vector3(ArenaSize + 1, 0, 0), new Vector3(SegmentSize, SegmentSize * 2, ArenaSize * 2 + 2), wallMaterial);
    }

    private void CreateWall(string name, Vector3 position, Vector3 size, Material material)
    {
        var wall = CreateObject(name);
        wall.LocalPosition = position;
        wall.LocalScale = size / SegmentSize;
        wall.AddComponent(new MeshComponent { Mesh = _cubeMesh });
        wall.AddComponent(new MaterialComponent { Material = material });
        wall.AddComponent(RigidbodyComponent.Static(PhysicsShape.Box(size)));
    }

    private void CreateSnake()
    {
        var headMaterial = new AnimatedSnakeMaterial("SnakeHead", 50, 200, 50);
        var bodyMaterial = new AnimatedSnakeMaterial("SnakeBody", 30, 150, 30);
        _foodMaterial = new GlowingFoodMaterial("Food", 255, 100, 50);
        Assets.RegisterMaterial(headMaterial);
        Assets.RegisterMaterial(bodyMaterial);
        Assets.RegisterMaterial(_foodMaterial);

        _snake = new Snake
        {
            HeadMesh = _cubeMesh,
            HeadMaterial = headMaterial,
            BodyMesh = _cubeMesh,
            BodyMaterial = bodyMaterial,
            SegmentSize = SegmentSize,
            ArenaSize = ArenaSize
        };
        _snake.OnAteFood += _ => SpawnFood();
        Add(_snake);
    }

    private void SpawnFood()
    {
        var existingFoods = GetObjectsOfType<Food>();
        foreach (var food in existingFoods)
        {
            Destroy(food);
        }

        var random = new Random();
        var x = random.Next(-ArenaSize + 1, ArenaSize);
        var z = random.Next(-ArenaSize + 1, ArenaSize);
        var position = new Vector3(x, 0, z);

        var newFood = new Food { LocalPosition = position };
        newFood.SetMeshAndMaterial(_sphereMesh, _foodMaterial);
        Add(newFood);
    }
}

public class SnakeMaterial : Material
{
    private readonly ColorTexture _colorTexture;
    public SnakeMaterial(string name, byte r, byte g, byte b)
    {
        Name = name;
        _colorTexture = new ColorTexture(r, g, b, 255, name);
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

public class GridMaterial : Material
{
    private readonly GridTexture _gridTexture;

    public GridMaterial(string name, GridTexture gridTexture)
    {
        Name = name;
        _gridTexture = gridTexture;
        Albedo = new Texture2d
        {
            Name = name,
            Width = 1,
            Height = 1,
            GpuTexture = gridTexture.Texture
        };
        GpuShader = Assets.GetShader("Builtin/Shaders/Default");
    }

    public override void Dispose()
    {
        _gridTexture.Dispose();
        base.Dispose();
    }
}

public class AnimatedSnakeMaterial : Material
{
    private static GpuShader? _cachedShader;
    private readonly ColorTexture _colorTexture;

    public AnimatedSnakeMaterial(string name, byte r, byte g, byte b)
    {
        Name = name;
        _colorTexture = new ColorTexture(r, g, b, 255, name);
        Albedo = new Texture2d
        {
            Name = name,
            Width = 1,
            Height = 1,
            GpuTexture = _colorTexture.Texture
        };
        GpuShader = GetOrCreateShader();
    }

    private static GpuShader GetOrCreateShader()
    {
        if (_cachedShader != null)
        {
            return _cachedShader;
        }

        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var pipelineDesc = new GraphicsPipelineDesc
        {
            PrimitiveTopology = PrimitiveTopology.Triangle,
            CullMode = CullMode.BackFace,
            FillMode = FillMode.Solid,
            DepthTest = new DepthTest
            {
                Enable = true,
                CompareOp = CompareOp.Less,
                Write = true
            },
            DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat,
            RenderTargets = renderTargets
        };

        _cachedShader = Content.LoadShader(
            "Shaders/Default/Default.VS.hlsl",
            "Shaders/AnimatedSnake.PS.hlsl",
            pipelineDesc);

        return _cachedShader;
    }

    public override void Dispose()
    {
        _colorTexture.Dispose();
        base.Dispose();
    }
}

public class GlowingFoodMaterial : Material
{
    private static GpuShader? _cachedShader;
    private readonly ColorTexture _colorTexture;

    public GlowingFoodMaterial(string name, byte r, byte g, byte b)
    {
        Name = name;
        _colorTexture = new ColorTexture(r, g, b, 255, name);
        Albedo = new Texture2d
        {
            Name = name,
            Width = 1,
            Height = 1,
            GpuTexture = _colorTexture.Texture
        };
        GpuShader = GetOrCreateShader();
    }

    private static GpuShader GetOrCreateShader()
    {
        if (_cachedShader != null)
        {
            return _cachedShader;
        }

        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var pipelineDesc = new GraphicsPipelineDesc
        {
            PrimitiveTopology = PrimitiveTopology.Triangle,
            CullMode = CullMode.BackFace,
            FillMode = FillMode.Solid,
            DepthTest = new DepthTest
            {
                Enable = true,
                CompareOp = CompareOp.Less,
                Write = true
            },
            DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat,
            RenderTargets = renderTargets
        };

        _cachedShader = Content.LoadShader(
            "Shaders/Default/Default.VS.hlsl",
            "Shaders/GlowingFood.PS.hlsl",
            pipelineDesc);

        return _cachedShader;
    }

    public override void Dispose()
    {
        _colorTexture.Dispose();
        base.Dispose();
    }
}
