using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Physics;

public enum PhysicsShapeType
{
    Box,
    Sphere,
    Capsule,
    Cylinder
}

public readonly struct PhysicsShape
{
    public PhysicsShapeType Type { get; init; }
    public Vector3 Size { get; init; }

    public static PhysicsShape Box(float width, float height, float depth)
    {
        return new PhysicsShape
        {
            Type = PhysicsShapeType.Box,
            Size = new Vector3(width, height, depth)
        };
    }

    public static PhysicsShape Box(Vector3 size)
    {
        return new PhysicsShape
        {
            Type = PhysicsShapeType.Box,
            Size = size
        };
    }

    public static PhysicsShape Cube(float size)
    {
        return Box(size, size, size);
    }

    public static PhysicsShape Sphere(float diameter)
    {
        return new PhysicsShape
        {
            Type = PhysicsShapeType.Sphere,
            Size = new Vector3(diameter, diameter, diameter)
        };
    }

    public static PhysicsShape Capsule(float radius, float length)
    {
        return new PhysicsShape
        {
            Type = PhysicsShapeType.Capsule,
            Size = new Vector3(radius, length, 0)
        };
    }

    public static PhysicsShape Cylinder(float diameter, float height)
    {
        return new PhysicsShape
        {
            Type = PhysicsShapeType.Cylinder,
            Size = new Vector3(diameter, height, 0)
        };
    }

    internal TypedIndex AddToSimulation(Simulation simulation)
    {
        return Type switch
        {
            PhysicsShapeType.Box => simulation.Shapes.Add(new Box(Size.X, Size.Y, Size.Z)),
            PhysicsShapeType.Sphere => simulation.Shapes.Add(new Sphere(Size.X * 0.5f)),
            PhysicsShapeType.Capsule => simulation.Shapes.Add(new Capsule(Size.X, Size.Y)),
            PhysicsShapeType.Cylinder => simulation.Shapes.Add(new Cylinder(Size.X * 0.5f, Size.Y)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal BodyInertia ComputeInertia(float mass)
    {
        return Type switch
        {
            PhysicsShapeType.Box => new Box(Size.X, Size.Y, Size.Z).ComputeInertia(mass),
            PhysicsShapeType.Sphere => new Sphere(Size.X * 0.5f).ComputeInertia(mass),
            PhysicsShapeType.Capsule => new Capsule(Size.X, Size.Y).ComputeInertia(mass),
            PhysicsShapeType.Cylinder => new Cylinder(Size.X * 0.5f, Size.Y).ComputeInertia(mass),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public enum PhysicsBodyType
{
    Dynamic,
    Static,
    Kinematic
}

public readonly struct PhysicsBodyDesc
{
    public PhysicsShape Shape { get; init; }
    public PhysicsBodyType BodyType { get; init; }
    public float Mass { get; init; }
    public float SpeculativeMargin { get; init; }
    public float SleepThreshold { get; init; }

    public static PhysicsBodyDesc Dynamic(PhysicsShape shape, float mass = 1f)
    {
        return new PhysicsBodyDesc
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Dynamic,
            Mass = mass,
            SpeculativeMargin = 0.1f,
            SleepThreshold = 0.01f
        };
    }

    public static PhysicsBodyDesc Static(PhysicsShape shape)
    {
        return new PhysicsBodyDesc
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Static,
            Mass = 0f,
            SpeculativeMargin = 0.1f,
            SleepThreshold = 0f
        };
    }

    public static PhysicsBodyDesc Kinematic(PhysicsShape shape)
    {
        return new PhysicsBodyDesc
        {
            Shape = shape,
            BodyType = PhysicsBodyType.Kinematic,
            Mass = 0f,
            SpeculativeMargin = 0.1f,
            SleepThreshold = 0f
        };
    }
}