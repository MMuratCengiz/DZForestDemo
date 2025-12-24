using System.Numerics;
using ECS;

namespace Physics;

public class PhysicsPlugin(Vector3? gravity = null, int threadCount = -1, bool syncVelocity = false)
{
    private readonly Vector3 _gravity = gravity ?? new Vector3(0, -9.81f, 0);

    public void Build(World world)
    {
        var context = new PhysicsResource(threadCount)
        {
            Gravity = _gravity
        };

        world.RegisterResource(context);

        world.AddSystem(new PhysicsCleanupSystem(), Schedule.FixedUpdate);

        world.AddSystem(new PhysicsStepSystem(), Schedule.FixedUpdate)
            .After<PhysicsCleanupSystem>();

        world.AddSystem(new PhysicsSyncSystem(), Schedule.FixedUpdate)
            .After<PhysicsStepSystem>();

        if (syncVelocity)
        {
            world.AddSystem(new PhysicsVelocitySyncSystem(), Schedule.FixedUpdate)
                .After<PhysicsSyncSystem>();
        }
    }
}