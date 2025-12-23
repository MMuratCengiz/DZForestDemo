using Application;
using Application.Extensions;
using DenOfIz;
using DZForestDemo.Scenes;
using DZForestDemo.Systems;
using ECS;
using Flecs.NET.Core;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        var app = Application.AppBuilder.Create()
            .WithTitle("DenOfIz Scene Demo - Press F1/F2 to switch scenes")
            .WithSize(1920, 1080)
            .WithNumFrames(3)
            .WithBackBufferFormat(Format.B8G8R8A8Unorm)
            .WithDepthBufferFormat(Format.D32Float)
            .WithApiPreference(new APIPreference
            {
                Windows = APIPreferenceWindows.Directx12
            })
            .WithGraphics()
            .WithPhysics()
            .WithAssets()
            .WithAnimation()
            .Build();

        var world = app.World;

        world.Component<ActiveScene>().Entity.Add(Ecs.Exclusive);
        world.Component<SceneRoot>();

        TransformSystem.Register(world);

        var sceneRenderSystem = new SceneRenderSystem(world);
        VikingScene.OnTextureLoaded = texture => sceneRenderSystem.SetActiveTexture(texture);

        FoxScene.Register(world);
        VikingScene.Register(world);

        sceneRenderSystem.Register();

        world.Set(new EventHandlers());
        world.Get<EventHandlers>().Register((ref Event ev) => sceneRenderSystem.HandleEvent(ref ev));

        world.Add<ActiveScene, FoxSceneTag>();

        app.Run();
    }
}
