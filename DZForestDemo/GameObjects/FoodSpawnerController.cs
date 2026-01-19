using System.Numerics;
using DZForestDemo.Scenes;
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
    public Material? FoodMaterial { get; set; }

    [JsonProperty("arenaSize")]
    public int ArenaSize { get; set; } = 15;

    [JsonProperty("foodSize")]
    public float FoodSize { get; set; } = 0.8f;

    private Scene? Scene => Owner?.Scene;

    public void Begin()
    {
        EnsureAssetsCreated();

        var snakeController = Scene?.FindComponent<SnakeController>();
        if (snakeController != null)
        {
            snakeController.OnAteFood += _ => SpawnFood();
        }
        SpawnFood();
    }

    private void EnsureAssetsCreated()
    {
        FoodMesh ??= Assets.CreateSphere(FoodSize);

        if (FoodMaterial == null)
        {
            var existing = Assets.GetMaterial("Food");
            if (existing != null)
            {
                FoodMaterial = existing;
            }
            else
            {
                FoodMaterial = new GlowingFoodMaterial("Food", 255, 100, 50);
                Assets.RegisterMaterial(FoodMaterial);
            }
        }
    }

    public void SpawnFood()
    {
        if (Scene == null || FoodMesh == null || FoodMaterial == null)
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
        go.AddComponent(new MaterialComponent { Material = FoodMaterial });
        go.AddComponent(RigidbodyComponent.Kinematic(PhysicsShape.Sphere(FoodSize)));

        Scene.Add(go);
    }
}
