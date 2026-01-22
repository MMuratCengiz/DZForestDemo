using System.Numerics;
using BepuPhysics;

namespace NiziKit.Physics;

public static class Physics
{
    private static PhysicsWorld World => Core.World.PhysicsWorld;

    public static Vector3 Gravity
    {
        get => World.Gravity;
        set => World.Gravity = value;
    }

    public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        return World.Raycast(new Ray(origin, direction), maxDistance, out hit);
    }

    public static bool Raycast(Ray ray, float maxDistance, out RaycastHit hit)
    {
        return World.Raycast(ray, maxDistance, out hit);
    }

    public static void IgnoreCollision(BodyHandle a, BodyHandle b, bool ignore = true)
    {
        World.IgnoreCollision(a, b, ignore);
    }

    public static Vector3 GetVelocity(int gameObjectId)
    {
        return World.GetVelocity(gameObjectId);
    }

    public static void SetVelocity(int gameObjectId, Vector3 velocity)
    {
        World.SetVelocity(gameObjectId, velocity);
    }

    public static Vector3 GetAngularVelocity(int gameObjectId)
    {
        return World.GetAngularVelocity(gameObjectId);
    }

    public static void SetAngularVelocity(int gameObjectId, Vector3 angularVelocity)
    {
        World.SetAngularVelocity(gameObjectId, angularVelocity);
    }

    public static void ApplyImpulse(int gameObjectId, Vector3 impulse)
    {
        World.ApplyImpulse(gameObjectId, impulse);
    }

    public static void ApplyImpulse(int gameObjectId, Vector3 impulse, Vector3 worldPoint)
    {
        World.ApplyImpulse(gameObjectId, impulse, worldPoint);
    }

    public static void ApplyAngularImpulse(int gameObjectId, Vector3 angularImpulse)
    {
        World.ApplyAngularImpulse(gameObjectId, angularImpulse);
    }

    public static void AddExplosionForce(Vector3 position, float force, float radius, float upwardsModifier = 0f)
    {
        World.AddExplosionForce(position, force, radius, upwardsModifier);
    }

    public static void AddAttractorForce(Vector3 position, float force, float radius, float falloffPower = 1f)
    {
        World.AddAttractorForce(position, force, radius, falloffPower);
    }

    public static IEnumerable<int> OverlapSphere(Vector3 position, float radius)
    {
        return World.OverlapSphere(position, radius);
    }

    public static void WakeUp(int gameObjectId)
    {
        World.Awake(gameObjectId);
    }
}
