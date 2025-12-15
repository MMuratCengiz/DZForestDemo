using System.Numerics;
using ECS;

namespace Physics;

public class PhysicsPlugin
{
    private readonly int _threadCount;
    private readonly Vector3 _gravity;
    private readonly bool _syncVelocity;

    public PhysicsPlugin(Vector3? gravity = null, int threadCount = -1, bool syncVelocity = false)
    {
        _gravity = gravity ?? new Vector3(0, -9.81f, 0);
        _threadCount = threadCount;
        _syncVelocity = syncVelocity;
    }

    public void Build(World world)
    {
        var context = new PhysicsContext(_threadCount)
        {
            Gravity = _gravity
        };

        world.RegisterContext(context);

        world.AddSystem(new PhysicsCleanupSystem(), Schedule.FixedUpdate);

        world.AddSystem(new PhysicsStepSystem(), Schedule.FixedUpdate)
            .After<PhysicsCleanupSystem>();

        world.AddSystem(new PhysicsSyncSystem(), Schedule.FixedUpdate)
            .After<PhysicsStepSystem>();

        if (_syncVelocity)
        {
            world.AddSystem(new PhysicsVelocitySyncSystem(), Schedule.FixedUpdate)
                .After<PhysicsSyncSystem>();
        }
    }
}
