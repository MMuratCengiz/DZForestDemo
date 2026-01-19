using System.Numerics;
using DZForestDemo.GameObjects;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.GLTF;
using NiziKit.Graphics.Binding;
using NiziKit.Light;
using NiziKit.Physics;

namespace DZForestDemo.Scenes;

public class RaceTrackScene() : Scene("Race Track")
{
    private const float TrackLength = 120f;
    private const float TrackWidth = 60f;
    private const float RoadWidth = 12f;
    private const float WallHeight = 1.5f;
    private const float WallThickness = 0.5f;
    private const float SyntyScale = 0.01f;

    private Mesh _boxMesh = null!;
    private Mesh _barrierMesh = null!;
    private Material _trackMaterial = null!;
    private Material _wallMaterial = null!;
    private Material _groundMaterial = null!;
    private Material _syntyMaterial = null!;

    private readonly Dictionary<string, Mesh?> _meshCache = new();
    private readonly List<Checkpoint> _checkpoints = [];

    public override void Load()
    {
        LoadAssets();
        var camera = CreateCamera();
        CreateLights();
        CreateTrack();
        CreateProps();
        CreateCheckpoints();
        var car = CreateCar();
        CreateRaceController(car, camera);
    }

    private void CreateRaceController(RaceCar car, CameraObject camera)
    {
        var controller = CreateObject<RaceController>();
        controller.Setup(car, camera, _checkpoints, RoadWidth);
    }

    private void LoadAssets()
    {
        _boxMesh = Assets.CreateBox(1, 1, 1);

        // Load Synty texture atlas - this single texture works for ALL Synty models
        var syntyTexture = Assets.LoadTexture("Racing/PolygonStreetRacer_Texture_01_A.png");
        _syntyMaterial = new SyntyMaterial("SyntyAtlas", syntyTexture);
        Assets.RegisterMaterial(_syntyMaterial);

        // Load Synty ground texture for the track surface
        var groundTexture = Assets.LoadTexture("Racing/PolygonStreetRacer_Ground_01.png");
        _trackMaterial = new SyntyMaterial("Track", groundTexture);
        Assets.RegisterMaterial(_trackMaterial);

        // Walls - red/white racing barriers
        _wallMaterial = new RacingMaterial("Wall", 180, 40, 40);
        Assets.RegisterMaterial(_wallMaterial);

        // Ground/grass
        _groundMaterial = new RacingMaterial("Ground", 45, 90, 45);
        Assets.RegisterMaterial(_groundMaterial);

        // Pre-load commonly used meshes
        _barrierMesh = LoadMesh("SM_Prop_Barrier_Roadblock_01") ?? _boxMesh;
    }

    /// <summary>
    /// Load and cache a mesh from the Racing folder
    /// </summary>
    private Mesh? LoadMesh(string modelName)
    {
        if (_meshCache.TryGetValue(modelName, out var cached))
        {
            return cached;
        }

        try
        {
            var model = GltfModel.Load($"Racing/{modelName}.glb");
            var mesh = model.Meshes.Count > 0 ? model.Meshes[0] : null;
            _meshCache[modelName] = mesh;
            return mesh;
        }
        catch
        {
            _meshCache[modelName] = null;
            return null;
        }
    }

    /// <summary>
    /// Create a Synty prop with the shared atlas material
    /// </summary>
    private GameObject? CreateSyntyProp(string name, string modelName, Vector3 position, float rotationY = 0, float scale = SyntyScale)
    {
        var mesh = LoadMesh(modelName);
        if (mesh == null) return null;

        var prop = CreateObject(name);
        prop.LocalPosition = position;
        prop.LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotationY);
        prop.LocalScale = new Vector3(scale);
        prop.AddComponent(new MeshComponent { Mesh = mesh });
        prop.AddComponent(new MaterialComponent { Material = _syntyMaterial });
        return prop;
    }

    private CameraObject CreateCamera()
    {
        var camera = CreateObject<CameraObject>("Main Camera");
        camera.LocalPosition = new Vector3(0, 25f, -15f);
        camera.FieldOfView = MathF.PI / 3.5f;
        camera.NearPlane = 0.1f;
        camera.FarPlane = 500f;
        MainCamera = camera;
        return camera;
    }

    private void CreateLights()
    {
        var sun = CreateObject<DirectionalLight>("Sun");
        sun.LookAt(Vector3.Normalize(new Vector3(0.5f, -0.8f, 0.3f)));
        sun.Color = new Vector3(1.0f, 0.98f, 0.95f);
        sun.Intensity = 1.0f;
        sun.CastsShadows = true;

        // Corner lights for better visibility
        var lightPositions = new[]
        {
            new Vector3(-TrackLength / 2, 20, -TrackWidth / 2),
            new Vector3(TrackLength / 2, 20, -TrackWidth / 2),
            new Vector3(-TrackLength / 2, 20, TrackWidth / 2),
            new Vector3(TrackLength / 2, 20, TrackWidth / 2)
        };

        for (var i = 0; i < lightPositions.Length; i++)
        {
            var light = CreateObject<PointLight>($"TrackLight_{i}");
            light.LocalPosition = lightPositions[i];
            light.Color = new Vector3(1.0f, 0.95f, 0.85f);
            light.Intensity = 2f;
            light.Range = 80f;
        }
    }

    private void CreateTrack()
    {
        // Ground/grass area
        var ground = CreateObject("Ground");
        ground.LocalPosition = new Vector3(0, -0.5f, 0);
        ground.LocalScale = new Vector3(TrackLength + RoadWidth * 4, 1f, TrackWidth + RoadWidth * 4);
        ground.AddComponent(new MeshComponent { Mesh = _boxMesh });
        ground.AddComponent(new MaterialComponent { Material = _groundMaterial });
        ground.AddComponent(RigidbodyComponent.Static(PhysicsShape.Box(new Vector3(TrackLength + RoadWidth * 4, 1f, TrackWidth + RoadWidth * 4))));

        // Track sections (clockwise from start/finish at bottom)
        CreateTrackSection("TrackBottom", new Vector3(0, 0.01f, -TrackWidth / 2 - RoadWidth / 2), new Vector3(TrackLength, 0.1f, RoadWidth));
        CreateTrackSection("TrackRight", new Vector3(TrackLength / 2 + RoadWidth / 2, 0.01f, 0), new Vector3(RoadWidth, 0.1f, TrackWidth));
        CreateTrackSection("TrackTop", new Vector3(0, 0.01f, TrackWidth / 2 + RoadWidth / 2), new Vector3(TrackLength, 0.1f, RoadWidth));
        CreateTrackSection("TrackLeft", new Vector3(-TrackLength / 2 - RoadWidth / 2, 0.01f, 0), new Vector3(RoadWidth, 0.1f, TrackWidth));

        // Corner sections
        CreateTrackSection("CornerBR", new Vector3(TrackLength / 2 + RoadWidth / 2, 0.01f, -TrackWidth / 2 - RoadWidth / 2), new Vector3(RoadWidth, 0.1f, RoadWidth));
        CreateTrackSection("CornerTR", new Vector3(TrackLength / 2 + RoadWidth / 2, 0.01f, TrackWidth / 2 + RoadWidth / 2), new Vector3(RoadWidth, 0.1f, RoadWidth));
        CreateTrackSection("CornerTL", new Vector3(-TrackLength / 2 - RoadWidth / 2, 0.01f, TrackWidth / 2 + RoadWidth / 2), new Vector3(RoadWidth, 0.1f, RoadWidth));
        CreateTrackSection("CornerBL", new Vector3(-TrackLength / 2 - RoadWidth / 2, 0.01f, -TrackWidth / 2 - RoadWidth / 2), new Vector3(RoadWidth, 0.1f, RoadWidth));

        // Create walls
        CreateWalls();
    }

    private void CreateTrackSection(string name, Vector3 position, Vector3 size)
    {
        var section = CreateObject(name);
        section.LocalPosition = position;
        section.LocalScale = size;
        section.AddComponent(new MeshComponent { Mesh = _boxMesh });
        section.AddComponent(new MaterialComponent { Material = _trackMaterial });
    }

    private void CreateWalls()
    {
        // Outer walls
        CreateWall("OuterBottom", new Vector3(0, WallHeight / 2, -TrackWidth / 2 - RoadWidth - WallThickness / 2), new Vector3(TrackLength + RoadWidth * 2 + WallThickness * 2, WallHeight, WallThickness));
        CreateWall("OuterTop", new Vector3(0, WallHeight / 2, TrackWidth / 2 + RoadWidth + WallThickness / 2), new Vector3(TrackLength + RoadWidth * 2 + WallThickness * 2, WallHeight, WallThickness));
        CreateWall("OuterLeft", new Vector3(-TrackLength / 2 - RoadWidth - WallThickness / 2, WallHeight / 2, 0), new Vector3(WallThickness, WallHeight, TrackWidth + RoadWidth * 2));
        CreateWall("OuterRight", new Vector3(TrackLength / 2 + RoadWidth + WallThickness / 2, WallHeight / 2, 0), new Vector3(WallThickness, WallHeight, TrackWidth + RoadWidth * 2));

        // Inner walls (creating the center island)
        CreateWall("InnerBottom", new Vector3(0, WallHeight / 2, -TrackWidth / 2 + WallThickness / 2), new Vector3(TrackLength, WallHeight, WallThickness));
        CreateWall("InnerTop", new Vector3(0, WallHeight / 2, TrackWidth / 2 - WallThickness / 2), new Vector3(TrackLength, WallHeight, WallThickness));
        CreateWall("InnerLeft", new Vector3(-TrackLength / 2 + WallThickness / 2, WallHeight / 2, 0), new Vector3(WallThickness, WallHeight, TrackWidth));
        CreateWall("InnerRight", new Vector3(TrackLength / 2 - WallThickness / 2, WallHeight / 2, 0), new Vector3(WallThickness, WallHeight, TrackWidth));
    }

    private void CreateWall(string name, Vector3 position, Vector3 size)
    {
        var wall = CreateObject(name);
        wall.LocalPosition = position;
        wall.LocalScale = size;
        wall.AddComponent(new MeshComponent { Mesh = _boxMesh });
        wall.AddComponent(new MaterialComponent { Material = _wallMaterial });
        wall.AddComponent(RigidbodyComponent.Static(PhysicsShape.Box(size)));
    }

    private void CreateProps()
    {
        CreateBuildings();
        CreateTrackBarriers();
        CreateCornerDecorations();
        CreateCenterIsland();
        CreateStartFinishArea();
        CreateContainersAndTires();
    }

    private void CreateBuildings()
    {
        // Garage/pit area along the bottom straight (outside the track)
        var pitAreaZ = -TrackWidth / 2 - RoadWidth - 15f;

        // Main repair shop
        CreateSyntyProp("RepairShop", "SM_Bld_RepairShop_Medium_01",
            new Vector3(-30, 0, pitAreaZ), MathF.PI);

        // Single garages
        CreateSyntyProp("Garage1", "SM_Bld_SingleGarage_01",
            new Vector3(-5, 0, pitAreaZ), MathF.PI);
        CreateSyntyProp("Garage2", "SM_Bld_SingleGarage_01",
            new Vector3(10, 0, pitAreaZ), MathF.PI);
        CreateSyntyProp("Garage3", "SM_Bld_SingleGarage_01",
            new Vector3(25, 0, pitAreaZ), MathF.PI);

        // Shelters along the track
        CreateSyntyProp("Shelter1", "SM_Bld_Shelter_01",
            new Vector3(TrackLength / 2 + RoadWidth + 10, 0, 0), -MathF.PI / 2);
        CreateSyntyProp("Shelter2", "SM_Bld_Shelter_02",
            new Vector3(-TrackLength / 2 - RoadWidth - 10, 0, 0), MathF.PI / 2);

        // Warehouse in the back
        CreateSyntyProp("Warehouse", "SM_Bld_Warehouse_01",
            new Vector3(40, 0, TrackWidth / 2 + RoadWidth + 12), 0);
    }

    private void CreateTrackBarriers()
    {
        // Roadblock barriers along the straights
        // Bottom straight - outer edge
        for (var i = 0; i < 8; i++)
        {
            var x = -TrackLength / 2 + 15 + i * 15;
            CreateSyntyProp($"BarrierBottom_{i}", "SM_Prop_Barrier_Roadblock_01",
                new Vector3(x, 0, -TrackWidth / 2 - RoadWidth - 1.5f), 0);
        }

        // Top straight - outer edge
        for (var i = 0; i < 8; i++)
        {
            var x = -TrackLength / 2 + 15 + i * 15;
            CreateSyntyProp($"BarrierTop_{i}", "SM_Prop_Barrier_Roadblock_01",
                new Vector3(x, 0, TrackWidth / 2 + RoadWidth + 1.5f), MathF.PI);
        }

        // Concrete barriers on inner corners
        CreateSyntyProp("ConcreteInner1", "SM_Prop_Barrier_Concrete_01",
            new Vector3(TrackLength / 2 - 3, 0, -TrackWidth / 2 + 3), MathF.PI / 4);
        CreateSyntyProp("ConcreteInner2", "SM_Prop_Barrier_Concrete_01",
            new Vector3(TrackLength / 2 - 3, 0, TrackWidth / 2 - 3), -MathF.PI / 4);
        CreateSyntyProp("ConcreteInner3", "SM_Prop_Barrier_Concrete_01",
            new Vector3(-TrackLength / 2 + 3, 0, TrackWidth / 2 - 3), MathF.PI + MathF.PI / 4);
        CreateSyntyProp("ConcreteInner4", "SM_Prop_Barrier_Concrete_01",
            new Vector3(-TrackLength / 2 + 3, 0, -TrackWidth / 2 + 3), MathF.PI - MathF.PI / 4);

        // Plastic barriers along left/right straights
        for (var i = 0; i < 4; i++)
        {
            var z = -TrackWidth / 2 + 15 + i * 15;
            CreateSyntyProp($"PlasticLeft_{i}", "SM_Prop_Barrier_Plastic_01",
                new Vector3(-TrackLength / 2 - RoadWidth - 1.5f, 0, z), MathF.PI / 2);
            CreateSyntyProp($"PlasticRight_{i}", "SM_Prop_Barrier_Plastic_01",
                new Vector3(TrackLength / 2 + RoadWidth + 1.5f, 0, z), -MathF.PI / 2);
        }
    }

    private void CreateCornerDecorations()
    {
        // Corner positions (outer corners)
        var corners = new[]
        {
            (new Vector3(TrackLength / 2 + RoadWidth + 3, 0, -TrackWidth / 2 - RoadWidth - 3), MathF.PI / 4),
            (new Vector3(TrackLength / 2 + RoadWidth + 3, 0, TrackWidth / 2 + RoadWidth + 3), -MathF.PI / 4),
            (new Vector3(-TrackLength / 2 - RoadWidth - 3, 0, TrackWidth / 2 + RoadWidth + 3), MathF.PI - MathF.PI / 4),
            (new Vector3(-TrackLength / 2 - RoadWidth - 3, 0, -TrackWidth / 2 - RoadWidth - 3), MathF.PI + MathF.PI / 4),
        };

        // Crash barrels at each corner
        for (var c = 0; c < corners.Length; c++)
        {
            var (pos, rot) = corners[c];
            CreateSyntyProp($"CrashBarrel_{c}_1", "SM_Prop_Barrier_CrashBarrel_01", pos, rot);
            CreateSyntyProp($"CrashBarrel_{c}_2", "SM_Prop_Barrier_CrashBarrel_01",
                pos + new Vector3(MathF.Cos(rot) * 2, 0, MathF.Sin(rot) * 2), rot);
        }

        // Cones around each corner for visual guidance
        for (var c = 0; c < corners.Length; c++)
        {
            var (cornerPos, _) = corners[c];
            for (var i = 0; i < 4; i++)
            {
                var angle = (c * MathF.PI / 2) + (i - 1.5f) * 0.4f;
                var offset = new Vector3(MathF.Cos(angle) * 5, 0, MathF.Sin(angle) * 5);
                CreateSyntyProp($"Cone_{c}_{i}", "SM_Prop_Barrier_Cone_01", cornerPos + offset);
            }
        }

        // Arrow signs at corners to show direction
        CreateSyntyProp("ArrowCorner1", "SM_Prop_Sign_Arrow_Corner_01",
            new Vector3(TrackLength / 2 + RoadWidth + 5, 0, -TrackWidth / 2 - RoadWidth - 5), 0);
        CreateSyntyProp("ArrowCorner2", "SM_Prop_Sign_Arrow_Corner_01",
            new Vector3(TrackLength / 2 + RoadWidth + 5, 0, TrackWidth / 2 + RoadWidth + 5), -MathF.PI / 2);
        CreateSyntyProp("ArrowCorner3", "SM_Prop_Sign_Arrow_Corner_01",
            new Vector3(-TrackLength / 2 - RoadWidth - 5, 0, TrackWidth / 2 + RoadWidth + 5), MathF.PI);
        CreateSyntyProp("ArrowCorner4", "SM_Prop_Sign_Arrow_Corner_01",
            new Vector3(-TrackLength / 2 - RoadWidth - 5, 0, -TrackWidth / 2 - RoadWidth - 5), MathF.PI / 2);
    }

    private void CreateCenterIsland()
    {
        // Green center island with decorations
        var centerMaterial = new RacingMaterial("CenterDecor", 60, 80, 60);
        Assets.RegisterMaterial(centerMaterial);

        var centerDecor = CreateObject("CenterIsland");
        centerDecor.LocalPosition = new Vector3(0, 0.1f, 0);
        centerDecor.LocalScale = new Vector3(TrackLength - 4, 0.2f, TrackWidth - 4);
        centerDecor.AddComponent(new MeshComponent { Mesh = _boxMesh });
        centerDecor.AddComponent(new MaterialComponent { Material = centerMaterial });

        // Tire stacks scattered in center island
        CreateSyntyProp("TireStack1", "SM_Prop_TyreStack_01", new Vector3(-20, 0, 10));
        CreateSyntyProp("TireStack2", "SM_Prop_TyreStack_02", new Vector3(20, 0, -10));
        CreateSyntyProp("TireStack3", "SM_Prop_TyreStack_01", new Vector3(0, 0, 15));
        CreateSyntyProp("TireStack4", "SM_Prop_TyreStack_02", new Vector3(-30, 0, -5));
        CreateSyntyProp("TireStack5", "SM_Prop_TyreStack_01", new Vector3(30, 0, 5));

        // Barrels in the center
        CreateSyntyProp("Barrel1", "SM_Prop_Barrier_Barrel_01", new Vector3(-15, 0, -15));
        CreateSyntyProp("Barrel2", "SM_Prop_Barrier_Barrel_01", new Vector3(15, 0, 15));
        CreateSyntyProp("Barrel3", "SM_Prop_Barrier_Barrel_01", new Vector3(25, 0, -20));

        // Caution signs
        CreateSyntyProp("Caution1", "SM_Prop_Sign_Caution_01", new Vector3(-40, 0, 0), MathF.PI / 2);
        CreateSyntyProp("Caution2", "SM_Prop_Sign_Caution_01", new Vector3(40, 0, 0), -MathF.PI / 2);

        // Light posts in center
        CreateSyntyProp("Light1", "SM_Prop_LightsFlat_01", new Vector3(0, 0, 0));
        CreateSyntyProp("Light2", "SM_Prop_LightsAngle_01", new Vector3(-25, 0, 0));
        CreateSyntyProp("Light3", "SM_Prop_LightsAngle_01", new Vector3(25, 0, 0));
    }

    private void CreateStartFinishArea()
    {
        // Start sign before the start/finish line
        CreateSyntyProp("StartSign", "SM_Prop_Sign_Start_01",
            new Vector3(-15, 0, -TrackWidth / 2 - RoadWidth / 2 - 5), 0);

        // Finish sign
        CreateSyntyProp("FinishSign", "SM_Prop_Sign_Finish_01",
            new Vector3(5, 0, -TrackWidth / 2 - RoadWidth / 2 - 5), 0);

        // Checkpoint signs along the track
        CreateSyntyProp("Checkpoint1", "SM_Prop_Sign_Checkpoint_01",
            new Vector3(TrackLength / 4, 0, -TrackWidth / 2 - RoadWidth - 3), 0);
        CreateSyntyProp("Checkpoint2", "SM_Prop_Sign_Checkpoint_01",
            new Vector3(TrackLength / 2 + RoadWidth + 3, 0, 0), -MathF.PI / 2);
        CreateSyntyProp("Checkpoint3", "SM_Prop_Sign_Checkpoint_01",
            new Vector3(0, 0, TrackWidth / 2 + RoadWidth + 3), MathF.PI);
        CreateSyntyProp("Checkpoint4", "SM_Prop_Sign_Checkpoint_01",
            new Vector3(-TrackLength / 2 - RoadWidth - 3, 0, 0), MathF.PI / 2);

        // Grandstand area (using stands)
        for (var i = 0; i < 5; i++)
        {
            CreateSyntyProp($"Stand_{i}", "SM_Prop_Stand_01",
                new Vector3(-30 + i * 10, 0, -TrackWidth / 2 - RoadWidth - 8), 0);
        }

        // Checkered finish line visual (boxes)
        var finishWhite = new RacingMaterial("FinishWhite", 255, 255, 255);
        var finishBlack = new RacingMaterial("FinishBlack", 20, 20, 20);
        Assets.RegisterMaterial(finishWhite);
        Assets.RegisterMaterial(finishBlack);

        for (var i = 0; i < 8; i++)
        {
            var isWhite = i % 2 == 0;
            var checker = CreateObject($"Finish_{i}");
            checker.LocalPosition = new Vector3(0, 0.02f, -TrackWidth / 2 - RoadWidth / 2 + (i - 3.5f) * 1.5f);
            checker.LocalScale = new Vector3(2f, 0.1f, 1.5f);
            checker.AddComponent(new MeshComponent { Mesh = _boxMesh });
            checker.AddComponent(new MaterialComponent { Material = isWhite ? finishWhite : finishBlack });
        }
    }

    private void CreateContainersAndTires()
    {
        // Shipping containers near the warehouse
        CreateSyntyProp("Container1", "SM_Prop_Container_01",
            new Vector3(55, 0, TrackWidth / 2 + RoadWidth + 15), MathF.PI / 6);
        CreateSyntyProp("Container2", "SM_Prop_Container_Large_01",
            new Vector3(60, 0, TrackWidth / 2 + RoadWidth + 8), 0);
        CreateSyntyProp("ContainerStack", "SM_Prop_Container_Stack_01",
            new Vector3(50, 0, TrackWidth / 2 + RoadWidth + 20), MathF.PI / 4);

        // More tire stacks in pit area
        CreateSyntyProp("PitTires1", "SM_Prop_TyreStack_01",
            new Vector3(-40, 0, -TrackWidth / 2 - RoadWidth - 10));
        CreateSyntyProp("PitTires2", "SM_Prop_TyreStack_02",
            new Vector3(-38, 0, -TrackWidth / 2 - RoadWidth - 10));
        CreateSyntyProp("PitTires3", "SM_Prop_TyreStack_01",
            new Vector3(35, 0, -TrackWidth / 2 - RoadWidth - 10));

        // Oil cans near garages
        CreateSyntyProp("OilCan1", "SM_Prop_OilCan_01",
            new Vector3(-3, 0, -TrackWidth / 2 - RoadWidth - 12));
        CreateSyntyProp("OilCan2", "SM_Prop_OilCan_01",
            new Vector3(12, 0, -TrackWidth / 2 - RoadWidth - 12));

        // Fence sections around outer perimeter
        for (var i = 0; i < 6; i++)
        {
            CreateSyntyProp($"FenceBottom_{i}", "SM_Prop_Fence_Wire_Single_01",
                new Vector3(-TrackLength / 2 + 20 + i * 20, 0, -TrackWidth / 2 - RoadWidth - 18), 0);
        }
        for (var i = 0; i < 6; i++)
        {
            CreateSyntyProp($"FenceTop_{i}", "SM_Prop_Fence_Wire_Single_01",
                new Vector3(-TrackLength / 2 + 20 + i * 20, 0, TrackWidth / 2 + RoadWidth + 18), MathF.PI);
        }
    }

    private void CreateCheckpoints()
    {
        // Create checkpoints around the track
        var checkpointPositions = new[]
        {
            (new Vector3(0, 1f, -TrackWidth / 2 - RoadWidth / 2), 0f),                    // Start/Finish
            (new Vector3(TrackLength / 4, 1f, -TrackWidth / 2 - RoadWidth / 2), 0f),     // Bottom right
            (new Vector3(TrackLength / 2 + RoadWidth / 2, 1f, -TrackWidth / 4), 90f),    // Right bottom
            (new Vector3(TrackLength / 2 + RoadWidth / 2, 1f, TrackWidth / 4), 90f),     // Right top
            (new Vector3(TrackLength / 4, 1f, TrackWidth / 2 + RoadWidth / 2), 0f),      // Top right
            (new Vector3(-TrackLength / 4, 1f, TrackWidth / 2 + RoadWidth / 2), 0f),     // Top left
            (new Vector3(-TrackLength / 2 - RoadWidth / 2, 1f, TrackWidth / 4), 90f),    // Left top
            (new Vector3(-TrackLength / 2 - RoadWidth / 2, 1f, -TrackWidth / 4), 90f),   // Left bottom
            (new Vector3(-TrackLength / 4, 1f, -TrackWidth / 2 - RoadWidth / 2), 0f),    // Bottom left
        };

        for (var i = 0; i < checkpointPositions.Length; i++)
        {
            var (pos, rotDeg) = checkpointPositions[i];
            var checkpoint = new Checkpoint(i, i == 0)
            {
                LocalPosition = pos
            };
            if (rotDeg != 0)
            {
                checkpoint.LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotDeg * MathF.PI / 180f);
            }
            _checkpoints.Add(checkpoint);
            Add(checkpoint);
        }
    }

    private RaceCar CreateCar()
    {
        var car = new RaceCar
        {
            CarModelPath = "Racing/SM_Veh_Sports_01.glb",
            WheelModelPath = "Racing/SM_Veh_Attach_Wheel_01.glb",
            TexturePath = "Racing/PolygonStreetRacer_Texture_01_A.png",
            LocalPosition = new Vector3(-10, 2, -TrackWidth / 2 - RoadWidth / 2),
            LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2)
        };
        Add(car);
        return car;
    }
}

public class RacingMaterial : Material
{
    private readonly ColorTexture _colorTexture;

    public RacingMaterial(string name, byte r, byte g, byte b)
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

public class SyntyMaterial : Material
{
    public SyntyMaterial(string name, Texture2d texture)
    {
        Name = name;
        Albedo = texture;
        GpuShader = Assets.GetShader("Builtin/Shaders/Default");
    }
}
