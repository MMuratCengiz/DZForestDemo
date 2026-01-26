using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public class FoodSpawnerController : IComponent
{
    public GameObject? Owner { get; set; }

    private readonly Random _random = new();

    public Mesh? FoodMesh { get; set; }

    [JsonProperty("arenaSize")]
    public int ArenaSize { get; set; } = 15;

    [JsonProperty("foodSize")]
    public float FoodSize { get; set; } = 0.8f;

    private Scene? Scene => Owner?.Scene;

    public void Begin()
    {
        FoodMesh ??= Assets.CreateSphere(FoodSize);

        var snakeController = Scene?.FindComponent<SnakeController>();
        if (snakeController != null)
        {
            snakeController.OnAteFood += _ => SpawnFood();
        }
        SpawnFood();
    }

    public void SpawnFood()
    {
        if (Scene == null || FoodMesh == null)
        {
            return;
        }

        foreach (var foodComp in Scene.FindComponents<Food>())
        {
            if (foodComp.Owner != null)
            {
                Scene.Destroy(foodComp.Owner);
            }
        }

        var x = _random.Next(-ArenaSize + 1, ArenaSize);
        var z = _random.Next(-ArenaSize + 1, ArenaSize);
        var position = new Vector3(x, 0, z);

        var go = new GameObject("Food") { LocalPosition = position };
        go.AddComponent(new Food());
        go.AddComponent(new MeshComponent { Mesh = FoodMesh });
        go.AddComponent(new SurfaceComponent
        {
            AlbedoColor = new Vector4(1.0f, 0.4f, 0.2f, 1.0f),
            EmissiveColor = new Vector3(1.0f, 0.4f, 0.2f),
            EmissiveIntensity = 2.0f
        });
        go.AddComponent(new MaterialComponent
        {
            Tags = { ["shader"] = "Shaders/GlowingFood.nizishp.json" }
        });
        go.AddComponent(RigidbodyComponent.Kinematic(PhysicsShape.Sphere(FoodSize)));

        Scene.Add(go);
    }
}
