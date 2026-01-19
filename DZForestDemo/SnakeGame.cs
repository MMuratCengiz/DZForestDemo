using DenOfIz;
using DZForestDemo.Scenes;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class SnakeGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;
    private SnakeScene? _scene;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer(RenderUi);
        _scene = new SnakeScene();
        World.LoadScene(_scene);

        Console.WriteLine("=== SNAKE 3D ===");
        Console.WriteLine("Controls:");
        Console.WriteLine("  WASD / Arrow Keys = Move");
        Console.WriteLine("  Space = Pause/Resume");
        Console.WriteLine("  R = Restart");
        Console.WriteLine("  Esc = Quit");
        Console.WriteLine();
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

        _renderer.Render();
    }

    private void RestartGame()
    {
        _scene = new SnakeScene();
        World.LoadScene(_scene);
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
    }

    private void RenderUi(UiFrame ui)
    {
        var snake = _scene?.Snake;

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
            using (ui.Panel("PauseContainer").Grow().CenterChildren().Open())
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
            using (ui.Panel("GameOverContainer").Grow().CenterChildren().Open())
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
