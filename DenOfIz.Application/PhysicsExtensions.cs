using System.Numerics;
using Physics;

namespace Application;

public static class PhysicsExtensions
{
    public static AppBuilder WithPhysics(this AppBuilder builder, Vector3? gravity = null, int threadCount = -1)
    {
        return builder.AddPlugin(app =>
        {
            var plugin = new PhysicsPlugin(gravity, threadCount);
            plugin.Build(app.World);
        });
    }
}
