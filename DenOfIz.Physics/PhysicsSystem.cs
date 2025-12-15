using System.Runtime.CompilerServices;
using ECS;
using ECS.Components;
using Physics.Components;

namespace Physics;

public sealed class PhysicsStepSystem : ISystem
{
    private PhysicsContext _physics = null!;

    public void Initialize(World world)
    {
        _physics = world.GetContext<PhysicsContext>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        _physics.ProcessRemovals();
        _physics.Step(_physics.FixedTimeStep);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public sealed class PhysicsSyncSystem : ISystem
{
    private World _world = null!;
    private PhysicsContext _physics = null!;

    public void Initialize(World world)
    {
        _world = world;
        _physics = world.GetContext<PhysicsContext>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        foreach (var item in _world.Query<RigidBody, Transform>())
        {
            ref readonly var rigidBody = ref item.Component1;
            if (rigidBody.IsStatic)
            {
                continue;
            }

            var (position, rotation) = _physics.GetBodyPose(rigidBody.Handle);
            ref var transform = ref item.Component2;
            transform.Position = position;
            transform.Rotation = rotation;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public sealed class PhysicsCleanupSystem : ISystem
{
    private World _world = null!;
    private PhysicsContext _physics = null!;
    private readonly List<Entity> _entitiesToRemove = new();

    public void Initialize(World world)
    {
        _world = world;
        _physics = world.GetContext<PhysicsContext>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        _entitiesToRemove.Clear();

        foreach (var item in _world.Query<RigidBody>())
        {
            var entity = item.Entity;
            if (!_world.Entities.IsAlive(entity))
            {
                _entitiesToRemove.Add(entity);
            }
        }

        foreach (var item in _world.Query<StaticBody>())
        {
            var entity = item.Entity;
            if (!_world.Entities.IsAlive(entity))
            {
                _entitiesToRemove.Add(entity);
            }
        }

        foreach (var entity in _entitiesToRemove)
        {
            _physics.RemoveBody(entity);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public sealed class PhysicsVelocitySyncSystem : ISystem
{
    private World _world = null!;
    private PhysicsContext _physics = null!;

    public void Initialize(World world)
    {
        _world = world;
        _physics = world.GetContext<PhysicsContext>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        foreach (var item in _world.Query<RigidBody, Velocity>())
        {
            ref readonly var rigidBody = ref item.Component1;
            if (rigidBody.IsStatic)
            {
                continue;
            }

            var (linear, angular) = _physics.GetBodyVelocity(rigidBody.Handle);
            ref var velocity = ref item.Component2;
            velocity.Linear = linear;
            velocity.Angular = angular;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
