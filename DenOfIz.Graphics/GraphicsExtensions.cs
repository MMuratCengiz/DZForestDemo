using Application;

namespace Graphics;

public static class GraphicsExtensions
{
    public static AppBuilder WithGraphics(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var plugin = new GraphicsPlugin(
                app.Window.NativeWindow,
                app.ApiPreference,
                app.NumFrames,
                app.BackBufferFormat,
                app.DepthBufferFormat,
                app.AllowTearing);

            plugin.Build(app.World);
        });
    }
}
