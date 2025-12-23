using System.Numerics;
using ECS.Components;
using Flecs.NET.Core;
using Physics.Components;

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

        world.Set(context);

        world.System("PhysicsStep")
            .Kind(Ecs.OnUpdate)
            .Run((Iter _) =>
            {
                if (!world.Has<PhysicsResource>())
                {
                    return;
                }

                ref var physics = ref world.GetMut<PhysicsResource>();
                for (var i = 0; i < physics.AccumulatedSteps; i++)
                {
                    physics.ProcessRemovals();
                    physics.Step(physics.FixedTimeStep);
                }
            });
        
        
        world.System<RigidBody, Transform>("PhysicsSync")
            .Kind(Ecs.OnUpdate)
            .Each((ref RigidBody rigidBody, ref Transform transform) =>
            {
                if (rigidBody.IsStatic)
                {
                    return;
                }

                var physics = world.Get<PhysicsResource>();
                var (position, rotation) = physics.GetBodyPose(rigidBody.Handle);
                transform.Position = position;
                transform.Rotation = rotation;
            });
        
        
        world.System("PhysicsCleanup")
            .Kind(Ecs.OnUpdate)
            .Run((Iter _) =>
            {
                if (!world.Has<PhysicsResource>())
                {
                    return;
                }

                ref var physics = ref world.GetMut<PhysicsResource>();
                physics.ProcessRemovals();
            });
        
        world.System<RigidBody, Velocity>("PhysicsVelocitySync")
            .Kind(Ecs.OnUpdate)
            .Each((ref RigidBody rigidBody, ref Velocity velocity) =>
            {
                if (rigidBody.IsStatic)
                {
                    return;
                }

                var physics = world.Get<PhysicsResource>();
                var (linear, angular) = physics.GetBodyVelocity(rigidBody.Handle);
                velocity.Linear = linear;
                velocity.Angular = angular;
            });
    }
}