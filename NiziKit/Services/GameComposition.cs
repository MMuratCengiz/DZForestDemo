using DenOfIz;
using NiziKit.Assets;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer.Forward;
using Pure.DI;

namespace NiziKit.Services;

public partial class GameComposition
{
    private static void Setup() =>
        DI.Setup(nameof(GameComposition))
            .Bind<GraphicsContext>().As(Lifetime.Singleton)
                .To((Window window, GraphicsDesc desc) => new GraphicsContext(window, desc))
            .Bind<Assets.Assets>().As(Lifetime.Singleton)
                .To((GraphicsContext _) => new Assets.Assets())
            .Bind<World>().As(Lifetime.Singleton)
                .To((Assets.Assets _) => new World())
            .Bind<ForwardRenderer>().As(Lifetime.Singleton)
                .To((World _) => new ForwardRenderer())
            .Arg<Window>("window")
            .Arg<GraphicsDesc>("graphicsDesc")
            .Root<GraphicsContext>("Graphics")
            .Root<Assets.Assets>("Assets")
            .Root<World>("World")
            .Root<ForwardRenderer>("Renderer");
}
