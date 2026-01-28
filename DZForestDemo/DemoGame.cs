using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
{
    private IRenderer _renderer = null!;
    private RenderFrame _renderFrame = null!;

    public override Type RendererType => typeof(ForwardRenderer);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);

        _renderer = new ForwardRenderer();
        World.LoadScene("Scenes/VikingShowcase.niziscene.json");
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

        _renderFrame.BeginFrame();
        var sceneTexture = _renderer.Render(_renderFrame);

        var debugOverlay = _renderFrame.RenderDebugOverlay();
        _renderFrame.AlphaBlit(debugOverlay, sceneTexture);

        var ui = _renderFrame.RenderUi(RenderUi);
        _renderFrame.AlphaBlit(ui, sceneTexture);

        _renderFrame.Submit();
        _renderFrame.Present(sceneTexture);
    }

    private void HandleInput()
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
