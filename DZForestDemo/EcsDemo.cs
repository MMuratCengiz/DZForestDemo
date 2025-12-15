using System.Numerics;
using ECS;
using ECS.Components;

namespace DZForestDemo;

public struct SpinComponent(float speed)
{
    public float Speed = speed;
}

public struct NameComponent(string name)
{
    public string Name = name;
}

public sealed class MovementSystem : ISystem
{
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
    }

    public void Run()
    {
        foreach (var item in _world.Query<Transform, Velocity>())
        {
            ref var transform = ref item.Component1;
            ref readonly var velocity = ref item.Component2;

            transform.Position += velocity.Linear * 0.016f;
        }
    }

    public bool OnEvent(ref DenOfIz.Event ev)
    {
        return false;
    }

    public void Shutdown() { }

    public void Dispose() { }
}

public sealed class SpinSystem : ISystem
{
    private World _world = null!;

    public void Initialize(World world)
    {
        _world = world;
    }

    public void Run()
    {
        foreach (var item in _world.Query<Transform, SpinComponent>())
        {
            ref var transform = ref item.Component1;
            ref readonly var spin = ref item.Component2;

            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, spin.Speed * 0.016f);
            transform.Rotate(rotation);
        }
    }

    public bool OnEvent(ref DenOfIz.Event ev)
    {
        return false;
    }

    public void Shutdown() { }

    public void Dispose() { }
}

public static class EcsDemo
{
    public static void SetupDemoScene(World world)
    {
        var mainScene = world.Scenes.CreateScene("MainScene");
        mainScene.Load();
        world.Scenes.SetActiveScene(mainScene);

        for (var i = 0; i < 1000; i++)
        {
            var entity = world.Create(
                new Transform(new Vector3(i * 2.0f, 0, 0)),
                new Velocity(new Vector3(0.1f, 0, 0))
            );
        }

        for (var i = 0; i < 500; i++)
        {
            var entity = world.Create(
                new Transform(new Vector3(0, i * 2.0f, 0)),
                new SpinComponent(1.0f + i * 0.01f)
            );
            entity.Get<Transform>(world.Entities).Scale = new Vector3(2, 2, 2);
        }

        for (var i = 0; i < 200; i++)
        {
            var entity = world.Create(
                new Transform(new Vector3(i, i, i)),
                new Velocity(new Vector3(0.05f, 0.1f, 0)),
                new SpinComponent(2.0f),
                new NameComponent($"Entity_{i}")
            );

            ref var transform = ref entity.Get<Transform>(world.Entities);
            transform.Scale = new Vector3(2, 2, 2);
        }
    }

    public static void PrintStats(World world)
    {
        Console.WriteLine($"Entity Count: {world.Entities.EntityCount}");
        Console.WriteLine($"Archetype Count: {world.Entities.ArchetypeCount}");

        var transformCount = 0;
        foreach (var _ in world.Query<Transform>())
        {
            transformCount++;
        }
        Console.WriteLine($"Entities with Transform: {transformCount}");

        var movingCount = 0;
        foreach (var _ in world.Query<Transform, Velocity>())
        {
            movingCount++;
        }
        Console.WriteLine($"Entities with Transform+Velocity: {movingCount}");

        var spinningCount = 0;
        foreach (var _ in world.Query<Transform, SpinComponent>())
        {
            spinningCount++;
        }
        Console.WriteLine($"Entities with Transform+Spin: {spinningCount}");
    }
}
