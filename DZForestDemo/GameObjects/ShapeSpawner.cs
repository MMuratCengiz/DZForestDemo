using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public class ShapeSpawner() : GameObject("ShapeSpawner")
{
    private readonly Random _random = new();

    public Mesh? CubeMesh { get; set; }
    public Mesh? SphereMesh { get; set; }
    public Material? Material { get; set; }

    public void SpawnRandomShape()
    {
        var position = new Vector3(
            (_random.NextSingle() - 0.5f) * 6f,
            10f + _random.NextSingle() * 5f,
            (_random.NextSingle() - 0.5f) * 6f
        );

        if (_random.NextSingle() > 0.5f)
        {
            SpawnCube(position);
        }
        else
        {
            SpawnSphere(position);
        }
    }

    public void SpawnCube(Vector3 position)
    {
        if (Scene == null || CubeMesh == null || Material == null)
        {
            return;
        }

        var cube = Scene.CreateObject("Cube");
        cube.LocalPosition = position;
        cube.LocalRotation = Quaternion.CreateFromYawPitchRoll(
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2,
            _random.NextSingle() * MathF.PI * 2
        );

        cube.AddComponent(new MeshComponent { Mesh = CubeMesh });
        cube.AddComponent(new MaterialComponent { Material = Material });
        cube.AddComponent(RigidbodyComponent.Dynamic(PhysicsShape.Box(Vector3.One), 1f));
    }

    public void SpawnSphere(Vector3 position)
    {
        if (Scene == null || SphereMesh == null || Material == null)
        {
            return;
        }

        var sphere = Scene.CreateObject("Sphere");
        sphere.LocalPosition = position;

        sphere.AddComponent(new MeshComponent { Mesh = SphereMesh });
        sphere.AddComponent(new MaterialComponent { Material = Material });
        sphere.AddComponent(RigidbodyComponent.Dynamic(PhysicsShape.Sphere(1f), 1f));
    }

    public void SpawnInitialCubes(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var position = new Vector3(
                (_random.NextSingle() - 0.5f) * 8f,
                5f + i * 1.5f,
                (_random.NextSingle() - 0.5f) * 8f
            );
            SpawnCube(position);
        }
    }
}
