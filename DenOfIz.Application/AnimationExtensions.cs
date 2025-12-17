using ECS;
using RuntimeAssets;

namespace Application;

public static class AnimationExtensions
{
    public static AppBuilder WithAnimation(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var animationContext = new AnimationContext();
            app.World.RegisterContext(animationContext);
            app.AddSystem(new AnimationSystem(), Schedule.Update);
        });
    }
}
