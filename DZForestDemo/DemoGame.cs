using DenOfIz;
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
    private string _currentChatText = "";

    public override Type RendererType => typeof(ForwardRenderer);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);

        _renderer = new ForwardRenderer();
        World.LoadScene("Scenes/CombatDemo.niziscene.json");
    }

    private void RenderUi(UiFrame ui)
    {
        using (ui.Panel("ChatBox")
                   .GrowHeight()
                   .FitWidth(300)
                   .Background(UiColor.Rgba(255, 255, 255, 0))
                   .AlignChildren(UiAlignX.Left, UiAlignY.Bottom)
                   .Open())
        {
            var uiTextStyle = new UiTextStyle
            {
                Color = new UiColor(255, 255, 255),
                FontSize = 24
            };
            ui.Text(_currentChatText, uiTextStyle);
            Ui.TextField(ui.Context, "ChatInput", ref _currentChatText)
                .BackgroundColor(UiColor.Rgb(255, 255, 255))
                .TextColor(UiColor.Rgb(0, 0, 0))
                .Show();
        }

    }

    protected override void Update(float dt)
    {
        _renderFrame.BeginFrame();
        var sceneTexture = _renderer.Render(_renderFrame);

        var debugOverlay = _renderFrame.RenderDebugOverlay();
        _renderFrame.AlphaBlit(debugOverlay, sceneTexture);

        var ui = _renderFrame.RenderUi(RenderUi);
        _renderFrame.AlphaBlit(ui, sceneTexture);

        _renderFrame.Submit();
        _renderFrame.Present(sceneTexture);
    }

    protected override void OnEvent(ref Event ev)
    {
        _renderFrame.HandleUiEvent(ev);
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
