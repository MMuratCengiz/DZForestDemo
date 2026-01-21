using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer(RenderUi);
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
        _renderer.Render();
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
    }
}