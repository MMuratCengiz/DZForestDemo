using System.Numerics;
using DZForestDemo.Scenes;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace DZForestDemo.GameObjects;

public class FoodSpawner() : GameObject("FoodSpawner")
{
    private readonly Random _random = new();

    public Mesh? FoodMesh { get; set; }
    public Material? FoodMaterial { get; set; }

    [JsonProperty("arenaSize")]
    public int ArenaSize { get; set; } = 15;

    [JsonProperty("foodSize")]
    public float FoodSize { get; set; } = 0.8f;

    public override void Begin()
    {
        // Create default mesh and material if not set
        EnsureAssetsCreated();

        var snake = World.FindObjectOfType<Snake>();
        if (snake != null)
        {
            snake.OnAteFood += _ => SpawnFood();
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

        var existingFoods = Scene.GetObjectsOfType<Food>();
        foreach (var food in existingFoods)
        {
            Scene.Destroy(food);
        }

        var x = _random.Next(-ArenaSize + 1, ArenaSize);
        var z = _random.Next(-ArenaSize + 1, ArenaSize);
        var position = new Vector3(x, 0, z);

        var newFood = new Food { LocalPosition = position };
        newFood.SetMeshAndMaterial(FoodMesh, FoodMaterial);
        Scene.Add(newFood);
    }
}
