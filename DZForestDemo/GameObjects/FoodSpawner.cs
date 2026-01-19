using System.Numerics;
using NiziKit.Assets;
using NiziKit.Core;

namespace DZForestDemo.GameObjects;

public class FoodSpawner() : GameObject("FoodSpawner")
{
    private readonly Random _random = new();

    public Mesh? FoodMesh { get; set; }
    public Material? FoodMaterial { get; set; }
    public int ArenaSize { get; set; } = 15;

    public override void Begin()
    {
        var snake = World.FindObjectOfType<Snake>();
        if (snake != null)
        {
            snake.OnAteFood += _ => SpawnFood();
        }
        SpawnFood();
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
