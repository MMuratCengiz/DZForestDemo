using System.Numerics;
using NiziKit.Application;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Renderer2D;

namespace DZForestDemo;

public sealed class Demo2DGame(GameDesc? desc = null) : Game(desc)
{
    private IRenderer _renderer = null!;
    private RenderFrame _renderFrame = null!;

    public override Type RendererType => typeof(Renderer2D);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderer = new Renderer2D();

        World.LoadScene("Scenes/Sprite2DDemo.niziscene.json");
    }

    protected override void Update(float dt)
    {
        _renderFrame.BeginFrame();
        var sceneTexture = _renderer.Render(_renderFrame);

        _renderFrame.Submit();
        _renderFrame.Present(sceneTexture);
    }

    protected override void OnEvent(ref DenOfIz.Event ev)
    {
    }

    protected override void FixedUpdate(float fixedDt)
    {
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
        _renderFrame?.Dispose();
    }
}
