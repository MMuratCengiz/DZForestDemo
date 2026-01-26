using DenOfIz;
using DZForestDemo.GameObjects;
using Microsoft.Extensions.Logging;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class SnakeGame(GameDesc? desc = null) : Game(desc)
{
    private static readonly ILogger Logger = Log.Get<SnakeGame>();
    private IRenderer _renderer = null!;
    private RenderFrame _renderFrame = null!;

    public override Type RendererType => typeof(ForwardRenderer);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);

        _renderer = new ForwardRenderer();
        World.LoadScene("Scenes/SnakeScene.niziscene.json");

        Logger.LogInformation("=== SNAKE 3D ===");
        Logger.LogInformation("Controls:");
        Logger.LogInformation("  WASD / Arrow Keys = Move");
        Logger.LogInformation("  Space = Pause/Resume");
        Logger.LogInformation("  R = Restart");
        Logger.LogInformation("  Esc = Quit");
    }

    protected override void Update(float dt)
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Quit();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }

        _renderFrame.BeginFrame();
        var sceneTexture = _renderer.Render(_renderFrame);

        // Add debug overlay
        var debugOverlay = _renderFrame.RenderDebugOverlay();
        _renderFrame.AlphaBlit(debugOverlay, sceneTexture);

        // Add UI overlay
        var ui = _renderFrame.RenderUi(RenderUi);
        _renderFrame.AlphaBlit(ui, sceneTexture);

        _renderFrame.Submit();
        _renderFrame.Present(sceneTexture);
    }

    private void RestartGame()
    {
        World.LoadScene("Scenes/SnakeScene.niziscene.json");
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
        _renderFrame?.Dispose();
    }

    private void RenderUi(UiFrame ui)
    {
        var snake = World.CurrentScene?.FindComponent<SnakeController>();

        var titleStyle = new UiTextStyle
        {
            Color = new UiColor(100, 255, 100),
            FontSize = 32
        };

        var scoreStyle = new UiTextStyle
        {
            Color = new UiColor(255, 255, 255),
            FontSize = 24
        };

        var statusStyle = new UiTextStyle
        {
            Color = new UiColor(255, 200, 100),
            FontSize = 28,
            Alignment = UiTextAlign.Center
        };

        // HUD - top left
        using (ui.Panel("HUD")
            .Fit()
            .Background(0, 0, 0, 150)
            .CornerRadius(8)
            .Padding(16)
            .Gap(8)
            .Vertical()
            .Open())
        {
            ui.Text("SNAKE 3D", titleStyle);
            ui.Text($"Score: {snake?.Score ?? 0}", scoreStyle);
        }

        // Overlay - grows to fill remaining space and centers content
        if (snake?.IsPaused == true)
        {
            using (ui.Panel("PauseContainer")
                .Width(UiSizing.Percent(1.0f))
                .Height(UiSizing.Grow())
                .CenterChildren()
                .Background(255, 0, 0, 50) // Debug: should fill full width
                .Open())
            {
                using (ui.Panel("PauseOverlay")
                    .Fit()
                    .Background(0, 0, 0, 200)
                    .CornerRadius(12)
                    .Padding(32)
                    .Gap(8)
                    .Vertical()
                    .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                    .Open())
                {
                    ui.Text("PAUSED", statusStyle);
                    ui.Text("Press SPACE to resume", scoreStyle);
                }
            }
        }
        else if (snake?.IsGameOver == true)
        {
            using (ui.Panel("GameOverContainer")
                .Width(UiSizing.Percent(1.0f))
                .Height(UiSizing.Grow())
                .CenterChildren()
                .Background(0, 255, 0, 50) // Debug: should fill full width
                .Open())
            {
                using (ui.Panel("GameOverOverlay")
                    .Fit()
                    .Background(80, 0, 0, 220)
                    .CornerRadius(12)
                    .Padding(32)
                    .Gap(12)
                    .Vertical()
                    .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                    .Open())
                {
                    ui.Text("GAME OVER", new UiTextStyle
                    {
                        Color = new UiColor(255, 100, 100),
                        FontSize = 36,
                        Alignment = UiTextAlign.Center
                    });
                    ui.Text($"Final Score: {snake?.Score ?? 0}", scoreStyle);
                    ui.Text("Press R to restart", scoreStyle);
                }
            }
        }
    }
}
