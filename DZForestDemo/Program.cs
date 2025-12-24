using Application;
using Application.Extensions;
using DenOfIz;
using DZForestDemo.Scenes;
using DZForestDemo.Systems;
using ECS;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (true)
        {
            RgCommandListDemoProgram.RunDemo();
            return;
        }
        var app = AppBuilder.Create()
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

        world.RegisterResource(new AssetLoadTracker());

        world.InitStateWithScenes(DemoGameState.Fox);

        var sceneRenderSystem = new SceneRenderSystem();

        var foxScene = new FoxScene();
        var vikingScene = new VikingScene();

        vikingScene.OnTextureLoaded = texture => sceneRenderSystem.SetActiveTexture(texture);

        world.RegisterScene(DemoGameState.Fox, foxScene);
        world.RegisterScene(DemoGameState.Viking, vikingScene);

        world.InitializeScenes<DemoGameState>();

        world.AddSystem(new StateTransitionSystem<DemoGameState>(), Schedule.PreUpdate);
        world.AddSystem(new AssetLoadTrackerSystem(), Schedule.PreUpdate);
        world.AddSystem(new TransformSystem(), Schedule.PostUpdate);
        world.AddSystem(sceneRenderSystem, Schedule.Render);

        world.AddOnEnter(DemoGameState.Fox, (w, s) =>
        {
            Console.WriteLine("Entered Fox Scene - 3 animated foxes with physics cubes");
        });

        world.AddOnEnter(DemoGameState.Viking, (w, s) =>
        {
            Console.WriteLine("Entered Viking Scene - Viking character models");
        });

        app.Run();
    }
}
