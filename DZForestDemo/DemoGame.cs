using System.Numerics;
using DenOfIz;
using DZForestDemo.Scenes;
using NiziKit.Application;
using NiziKit.Assets;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Physics;
using NiziKit.SceneManagement;

namespace DZForestDemo;

public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
{
    private Camera _cameraController = null!;
    private ForwardRenderer _renderer = null!;
    private DemoScene? _demoScene;

    private AnimationManager? _animation;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer(Graphics);
        _cameraController = new Camera(
            new Vector3(0, 12, 25),
            new Vector3(0, 2, 0)
        );
        _cameraController.SetAspectRatio(game.Graphics.Width, game.Graphics.Height);

        _animation = new AnimationManager();

        _demoScene = new DemoScene(World, _animation);
        LoadScene(_demoScene);
    }

    protected override void Update(float dt)
    {
        _cameraController.Update(dt);
        _animation?.Update(dt);

        SyncCameraToScene();
        SyncPhysicsToSceneObjects();
        
        _renderer.Render(World);
    }

    private void SyncCameraToScene()
    {
        var camera = World.CurrentScene?.MainCamera;
        if (camera == null)
        {
            return;
        }

        camera.LocalPosition = _cameraController.Position;
        camera.SetYawPitch(
            MathF.Atan2(_cameraController.Forward.X, _cameraController.Forward.Z),
            MathF.Asin(Math.Clamp(_cameraController.Forward.Y, -1f, 1f))
        );
        camera.FieldOfView = _cameraController.FieldOfView;
        camera.AspectRatio = _cameraController.AspectRatio;
        camera.NearPlane = _cameraController.NearPlane;
        camera.FarPlane = _cameraController.FarPlane;
    }

    private void SyncPhysicsToSceneObjects()
    {
        var scene = World.CurrentScene;
        var physics = World.PhysicsWorld;
        if (scene == null || physics == null)
        {
            return;
        }

        foreach (var obj in scene.RootObjects)
        {
            SyncPhysicsRecursive(obj, physics);
        }
    }

    private static void SyncPhysicsRecursive(GameObject obj, PhysicsWorld physics)
    {
        var pose = physics.GetPose(obj.Id);
        if (pose.HasValue)
        {
            obj.LocalPosition = pose.Value.Position;
            obj.LocalRotation = pose.Value.Rotation;
        }

        foreach (var child in obj.Children)
        {
            SyncPhysicsRecursive(child, physics);
        }
    }

    protected override void FixedUpdate(float fixedDt)
    {
        World.PhysicsWorld?.Step(fixedDt);
    }


    protected override void OnEvent(ref Event ev)
    {
        _cameraController.HandleEvent(ev);

        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            var width = (uint)ev.Window.Data1;
            var height = (uint)ev.Window.Data2;
            _cameraController.SetAspectRatio(width, height);
            World.CurrentScene?.MainCamera?.SetAspectRatio(width, height);
        }

        if (ev.Type == EventType.KeyDown)
        {
            switch (ev.Key.KeyCode)
            {
                case KeyCode.Space:
                    _demoScene?.AddRandomShape();
                    break;
                case KeyCode.Escape:
                    Quit();
                    break;
            }
        }
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
        _animation?.Dispose();
    }
}
