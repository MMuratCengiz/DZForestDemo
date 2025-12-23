using RuntimeAssets;

namespace Application.Extensions;

public static class AnimationExtensions
{
    public static AppBuilder WithAnimation(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var animation = new AnimationResource();
            app.World.Set(animation);
            AnimationSystems.Register(app.World);
        });
    }
}
