using DenOfIz;
using DZForestDemo.UI;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Renderer2D;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class Demo2DGame(GameDesc? desc = null) : Game(desc)
{
    private DemoGameUi _ui = null!;
    private IRenderer _renderer = null!;
    private RenderFrame _renderFrame = null!;

    public override Type RendererType => typeof(Renderer2D);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderer = new Renderer2D();
        _renderFrame.EnableUi(UiContextDesc.Default);
        _ui = new DemoGameUi();

        World.LoadScene("Scenes/Sprite2DDemo.niziscene.json");
    }

    protected override void Update(float dt)
    {
        _renderFrame.BeginFrame();
        var sceneTexture = _renderer.Render(_renderFrame);
        var uiTexture = _renderFrame.RenderUi(() => _ui.Render());
        _renderFrame.AlphaBlit(uiTexture, sceneTexture);
        _renderFrame.Submit();
        _renderFrame.Present(sceneTexture);
    }

    protected override void FixedUpdate(float fixedDt)
    {
    }

    protected override void OnShutdown()
    {
        _renderer.Dispose();
        _renderFrame.Dispose();
    }
}
