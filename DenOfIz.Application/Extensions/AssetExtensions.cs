using Graphics;
using RuntimeAssets;

namespace Application.Extensions;

public static class AssetExtensions
{
    public static AppBuilder WithAssets(this AppBuilder builder)
    {
        return builder.AddPlugin(app =>
        {
            var gfxContext = app.World.GetResource<GraphicsResource>();
            var assetContext = new AssetResource(gfxContext.LogicalDevice);
            app.World.RegisterResource(assetContext);
        });
    }
}