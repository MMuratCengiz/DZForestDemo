using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Renderer.Forward;
using Pure.DI;

namespace NiziKit.Services;

public partial class GameComposition
{
    private static void Setup() =>
        DI.Setup(nameof(GameComposition))
            .Bind<Time>().As(Lifetime.Singleton)
                .To(() => new Time())
            .Bind<GraphicsContext>().As(Lifetime.Singleton)
                .To((Time _, Window window, GraphicsDesc desc) => new GraphicsContext(window, desc))
            .Bind<GpuBinding>().As(Lifetime.Singleton)
                .To((GraphicsContext _) => new GpuBinding())
            .Bind<Assets.Assets>().As(Lifetime.Singleton)
                .To((GpuBinding _) => new Assets.Assets())
            .Bind<World>().As(Lifetime.Singleton)
                .To((Assets.Assets _) => new World())
            .Bind<ForwardRenderer>().As(Lifetime.Singleton)
                .To((World _) => new ForwardRenderer())
            .Arg<Window>("window")
            .Arg<GraphicsDesc>("graphicsDesc")
            .Root<Time>("Time")
            .Root<GraphicsContext>("Graphics")
            .Root<GpuBinding>("Binding")
            .Root<Assets.Assets>("Assets")
            .Root<World>("World")
            .Root<ForwardRenderer>("Renderer");
}
