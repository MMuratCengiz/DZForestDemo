using System.Numerics;
using DenOfIz;
using DZForestDemo.GameObjects;
using DZForestDemo.Scenes;
using NiziKit.Animation;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;
    private float _layerWeight;

    private const float ExplosionForce = 10f;
    private const float ExplosionRadius = 4f;
    private const float ExplosionUpwardsModifier = 0.3f;

    private const float MouseGravityForce = 2f;
    private const float MouseGravityRadius = 15f;
    private bool _mouseGravityEnabled;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer(RenderUi);
        World.LoadScene(new DemoScene());

        Console.WriteLine("Controls:");
        Console.WriteLine("  Left Click = Explosion at cursor");
        Console.WriteLine("  G = Toggle mouse gravity");
        Console.WriteLine("  Space = Add shape");
        Console.WriteLine("  Left Fox: S=Survey, Z/X/C=CrossFade curves, V=Run");
        Console.WriteLine("  Right Fox: Up/Down=Layer weight");
        Console.WriteLine("  Esc = Quit");
    }

    private static void RenderUi(UiFrame ui)
    {
        var uiTextStyle = new UiTextStyle
        {
            Color = new UiColor(255, 255, 255),
            FontSize = 24
        };
        ui.Text("Hi!", uiTextStyle);
    }

    protected override void Update(float dt)
    {
        HandleInput();
        _renderer.Render();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }

        if (Input.GetMouseButtonDown(MouseButton.Left))
        {
            var mousePos = Input.MousePosition;
            HandleExplosionClick(mousePos.X, mousePos.Y);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            World.FindObjectOfType<ShapeSpawner>()?.SpawnRandomShape();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            _mouseGravityEnabled = !_mouseGravityEnabled;
            Console.WriteLine($"Mouse gravity: {(_mouseGravityEnabled ? "ON" : "OFF")}");
        }

        var foxes = World.FindObjectsOfType<Fox>();
        var fox = foxes.Count > 0 ? foxes[0] : null;
        var layerBlendFox = foxes.Count > 1 ? foxes[1] : null;

        if (Input.GetKeyDown(KeyCode.S))
        {
            fox?.TriggerSurvey();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            fox?.CrossFadeToSurvey(TransitionCurve.Linear);
            Console.WriteLine("CrossFade to Survey (Linear)");
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            fox?.CrossFadeToSurvey(TransitionCurve.EaseIn);
            Console.WriteLine("CrossFade to Survey (EaseIn)");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            fox?.CrossFadeToSurvey(TransitionCurve.EaseOut);
            Console.WriteLine("CrossFade to Survey (EaseOut)");
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            fox?.CrossFadeToRun();
            Console.WriteLine("CrossFade to Run");
        }

        if (Input.GetKeyDown(KeyCode.Up))
        {
            _layerWeight = Math.Min(1f, _layerWeight + 0.1f);
            layerBlendFox?.SetOverlayWeight(_layerWeight);
            Console.WriteLine($"Layer weight: {_layerWeight:F1}");
        }

        if (Input.GetKeyDown(KeyCode.Down))
        {
            _layerWeight = Math.Max(0f, _layerWeight - 0.1f);
            layerBlendFox?.SetOverlayWeight(_layerWeight);
            Console.WriteLine($"Layer weight: {_layerWeight:F1}");
        }
    }

    protected override void FixedUpdate(float fixedDt)
    {
        if (_mouseGravityEnabled)
        {
            ApplyMouseGravity();
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

    private void ApplyMouseGravity()
    {
        var camera = World.CurrentScene?.MainCamera;
        if (camera == null)
        {
            return;
        }

        var mousePos = Input.MousePosition;
        var ray = camera.ScreenPointToRay(mousePos.X, mousePos.Y, Window.Width, Window.Height);

        Vector3 gravityPoint;
        if (World.PhysicsWorld!.Raycast(ray, 100f, out var hit))
        {
            gravityPoint = hit.Point;
        }
        else
        {
            gravityPoint = ray.GetPoint(15f);
        }

        World.PhysicsWorld.AddAttractorForce(gravityPoint, MouseGravityForce, MouseGravityRadius);
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
    }
}