using System.Numerics;
using DenOfIz;
using DZForestDemo.Scenes;
using NiziKit.Animation;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Physics;

namespace DZForestDemo;

public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;
    private DemoScene? _demoScene;
    private float _layerWeight;

    private const float ExplosionForce = 10f;
    private const float ExplosionRadius = 4f;
    private const float ExplosionUpwardsModifier = 0.3f;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer();
        _demoScene = new DemoScene();
        World.LoadScene(_demoScene);

        Console.WriteLine("Controls:");
        Console.WriteLine("  Left Click = Explosion at cursor");
        Console.WriteLine("  Space = Add shape");
        Console.WriteLine("  Left Fox: S=Survey, Z/X/C=CrossFade curves, V=Run");
        Console.WriteLine("  Right Fox: Up/Down=Layer weight");
        Console.WriteLine("  Esc = Quit");
    }

    protected override void Update(float dt)
    {
        _renderer.Render();
    }

    protected override void FixedUpdate(float fixedDt)
    {
    }

    protected override void OnEvent(ref Event ev)
    {
        if (ev.Type == EventType.MouseButtonDown && ev.MouseButton.Button == MouseButton.Left)
        {
            HandleExplosionClick(ev.MouseButton.X, ev.MouseButton.Y);
            return;
        }

        if (ev.Type == EventType.KeyDown)
        {
            switch (ev.Key.KeyCode)
            {
                case KeyCode.Space:
                    _demoScene?.AddRandomShape();
                    break;
                case KeyCode.S:
                    _demoScene?.Fox?.TriggerSurvey();
                    break;
                case KeyCode.Z:
                    _demoScene?.Fox?.CrossFadeToSurvey(TransitionCurve.Linear);
                    Console.WriteLine("CrossFade to Survey (Linear)");
                    break;
                case KeyCode.X:
                    _demoScene?.Fox?.CrossFadeToSurvey(TransitionCurve.EaseIn);
                    Console.WriteLine("CrossFade to Survey (EaseIn)");
                    break;
                case KeyCode.C:
                    _demoScene?.Fox?.CrossFadeToSurvey(TransitionCurve.EaseOut);
                    Console.WriteLine("CrossFade to Survey (EaseOut)");
                    break;
                case KeyCode.V:
                    _demoScene?.Fox?.CrossFadeToRun();
                    Console.WriteLine("CrossFade to Run");
                    break;
                case KeyCode.Up:
                    _layerWeight = Math.Min(1f, _layerWeight + 0.1f);
                    _demoScene?.LayerBlendFox?.SetOverlayWeight(_layerWeight);
                    Console.WriteLine($"Layer weight: {_layerWeight:F1}");
                    break;
                case KeyCode.Down:
                    _layerWeight = Math.Max(0f, _layerWeight - 0.1f);
                    _demoScene?.LayerBlendFox?.SetOverlayWeight(_layerWeight);
                    Console.WriteLine($"Layer weight: {_layerWeight:F1}");
                    break;
                case KeyCode.Escape:
                    Quit();
                    break;
            }
        }
    }

    private void HandleExplosionClick(float mouseX, float mouseY)
    {
        var camera = World.CurrentScene?.MainCamera;
        if (camera == null)
        {
            return;
        }

        var ray = camera.ScreenPointToRay(mouseX, mouseY, Window.Width, Window.Height);

        Vector3 explosionPoint;
        if (World.PhysicsWorld!.Raycast(ray, 100f, out var hit))
        {
            explosionPoint = hit.Point;
        }
        else
        {
            explosionPoint = ray.GetPoint(20f);
        }

        World.PhysicsWorld.AddExplosionForce(explosionPoint, ExplosionForce, ExplosionRadius, ExplosionUpwardsModifier);
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
    }
}
