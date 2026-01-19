using DenOfIz;
using DZForestDemo.GameObjects;
using DZForestDemo.Scenes;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class RaceGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;

    protected override void Load(Game game)
    {
        _renderer = new ForwardRenderer(RenderUi);
        World.LoadScene(new RaceTrackScene());

        Console.WriteLine("=== STREET RACER ===");
        Console.WriteLine("Controls:");
        Console.WriteLine("  W / Up Arrow    = Accelerate");
        Console.WriteLine("  S / Down Arrow  = Brake / Reverse");
        Console.WriteLine("  A / Left Arrow  = Steer Left");
        Console.WriteLine("  D / Right Arrow = Steer Right");
        Console.WriteLine("  Space           = Handbrake");
        Console.WriteLine("  R               = Restart Race");
        Console.WriteLine("  Esc             = Quit");
        Console.WriteLine();
        Console.WriteLine("Drive through checkpoints to complete laps!");
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
            RestartRace();
        }

        _renderer.Render();
    }

    private void RestartRace()
    {
        World.FindObjectOfType<RaceController>()?.ResetRace();
        Console.WriteLine("Race restarted!");
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
    }

    private void RenderUi(UiFrame ui)
    {
        var controller = World.FindObjectOfType<RaceController>();
        var car = controller?.Car;

        var titleStyle = new UiTextStyle
        {
            Color = new UiColor(255, 200, 50),
            FontSize = 28
        };

        var speedStyle = new UiTextStyle
        {
            Color = new UiColor(255, 255, 255),
            FontSize = 48
        };

        var infoStyle = new UiTextStyle
        {
            Color = new UiColor(200, 200, 200),
            FontSize = 20
        };

        var lapStyle = new UiTextStyle
        {
            Color = new UiColor(100, 255, 100),
            FontSize = 24
        };

        var bestLapStyle = new UiTextStyle
        {
            Color = new UiColor(255, 215, 0),
            FontSize = 20
        };

        using (ui.Panel("RaceHUD")
            .Fit()
            .Background(0, 0, 0, 150)
            .CornerRadius(8)
            .Padding(16)
            .Gap(8)
            .Vertical()
            .Open())
        {
            ui.Text("STREET RACER", titleStyle);

            var lapCount = controller?.LapCount ?? 0;
            ui.Text($"Lap: {lapCount + 1}", lapStyle);

            var lapTime = controller?.LapTime ?? 0;
            ui.Text($"Time: {FormatTime(lapTime)}", infoStyle);

            var bestLap = controller?.BestLapTime ?? 0;
            if (bestLap > 0)
            {
                ui.Text($"Best: {FormatTime(bestLap)}", bestLapStyle);
            }

            var checkpoint = controller?.CurrentCheckpoint ?? 0;
            var totalCheckpoints = controller?.TotalCheckpoints ?? 0;
            ui.Text($"Checkpoint: {checkpoint}/{totalCheckpoints}", infoStyle);
        }

        // Speedometer - Top right area
        using (ui.Panel("SpeedContainer")
            .Fit()
            .Horizontal()
            .AlignChildren(UiAlignX.Right, UiAlignY.Top)
            .Open())
        {
            // Spacer to push to right
            using (ui.Panel("Spacer")
                .GrowWidth()
                .Open())
            {
            }

            using (ui.Panel("SpeedPanel")
                .Fit()
                .Background(0, 0, 0, 180)
                .CornerRadius(12)
                .Padding(24)
                .Gap(4)
                .Vertical()
                .AlignChildren(UiAlignX.Center, UiAlignY.Center)
                .Open())
            {
                var speed = car?.SpeedKmh ?? 0;
                ui.Text($"{speed:F0}", speedStyle);
                ui.Text("KM/H", infoStyle);
            }
        }

        // Controls hint - Bottom area
        using (ui.Panel("ControlsContainer")
            .GrowHeight()
            .Open())
        {
        }

        using (ui.Panel("Controls")
            .Fit()
            .Background(0, 0, 0, 100)
            .CornerRadius(6)
            .Padding(12)
            .Gap(4)
            .Vertical()
            .Open())
        {
            var hintStyle = new UiTextStyle
            {
                Color = new UiColor(150, 150, 150),
                FontSize = 14
            };
            ui.Text("WASD - Drive | SPACE - Handbrake | R - Restart", hintStyle);
        }
    }

    private static string FormatTime(float seconds)
    {
        var mins = (int)(seconds / 60);
        var secs = seconds % 60;
        return mins > 0 ? $"{mins}:{secs:00.00}" : $"{secs:F2}s";
    }
}
