using Graphics;
using RuntimeAssets;

namespace Application;

public static class AssetExtensions
{
    public static AppBuilder WithAssets(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var gfxContext = app.World.GetContext<GraphicsContext>();
            var assetContext = new AssetContext(gfxContext.LogicalDevice);
            app.World.RegisterContext(assetContext);
        });
    }
}