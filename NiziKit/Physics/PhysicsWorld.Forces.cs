using System.Numerics;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
    public void AddAttractorForce(Vector3 position, float force, float radius, float falloffPower = 1f)
    {
        var radiusSq = radius * radius;

        foreach (var (id, handle) in _bodyHandles)
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var bodyPos = bodyRef.Pose.Position;
            var diff = position - bodyPos;
            var distSq = diff.LengthSquared();

            if (distSq > radiusSq || distSq < 0.0001f)
            {
                continue;
            }

            var dist = MathF.Sqrt(distSq);
            var falloff = MathF.Pow(1f - (dist / radius), falloffPower);
            var direction = diff / dist;

            var impulse = direction * force * falloff;
            _simulation.Awakener.AwakenBody(handle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
    }

    public void AddExplosionForce(Vector3 position, float force, float radius, float upwardsModifier = 0f)
    {
        var radiusSq = radius * radius;

        foreach (var (id, handle) in _bodyHandles)
        {
            var bodyRef = _simulation.Bodies.GetBodyReference(handle);
            var bodyPos = bodyRef.Pose.Position;
            var diff = bodyPos - position;
            var distSq = diff.LengthSquared();

            if (distSq > radiusSq || distSq < 0.0001f)
            {
                continue;
            }

            var dist = MathF.Sqrt(distSq);
            var falloff = 1f - (dist / radius);
            var direction = diff / dist;

            if (upwardsModifier != 0f)
            {
                direction.Y += upwardsModifier;
                direction = Vector3.Normalize(direction);
            }

            var impulse = direction * force * falloff;
            _simulation.Awakener.AwakenBody(handle);
            bodyRef.ApplyLinearImpulse(impulse);
        }
    }

    public IEnumerable<int> OverlapSphere(Vector3 position, float radius)
    {
        var radiusSq = radius * radius;
        foreach (var (id, _) in _trackedObjects)
        {
            var pose = GetPose(id);
            if (!pose.HasValue)
            {
                continue;
            }

            var distSq = (pose.Value.Position - position).LengthSquared();
            if (distSq <= radiusSq)
            {
                yield return id;
            }
        }
    }
}
