using Graphics;

namespace Application.Extensions;

public static class GraphicsExtensions
{
    public static AppBuilder WithGraphics(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var gfx = app.Graphics;
            var plugin = new GraphicsPlugin(
                app.Window.NativeWindow,
                gfx.ApiPreference,
                gfx.NumFrames,
                gfx.BackBufferFormat,
                gfx.DepthBufferFormat,
                gfx.AllowTearing);

            plugin.Build(app.World);
        });
    }
}