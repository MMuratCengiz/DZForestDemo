using DenOfIz;
using DZForestDemo.GameObjects;
using DZForestDemo.Scenes;
using NiziKit.Application;
using NiziKit.Assets;
using NiziKit.Core;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo;

public sealed class SnakeGame(GameDesc? desc = null) : Game(desc)
{
    private ForwardRenderer _renderer = null!;

    protected override void Load(Game game)
    {
        RegisterGameObjects();

        _renderer = new ForwardRenderer(RenderUi);
        World.LoadScene("Scenes/SnakeScene.niziscene.json");

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
        World.LoadScene("Scenes/SnakeScene.niziscene.json");
    }

    protected override void OnShutdown()
    {
        _renderer?.Dispose();
    }

    private static void RegisterGameObjects()
    {
        GameObjectRegistry.Register("Snake", props =>
        {
            var snake = new Snake();

            var segmentSize = 1f;
            var arenaSize = 15;

            if (props.HasValue)
            {
                if (props.Value.TryGetProperty("segmentSize", out var segmentSizeProp))
                {
                    segmentSize = segmentSizeProp.GetSingle();
                }

                if (props.Value.TryGetProperty("arenaSize", out var arenaSizeProp))
                {
                    arenaSize = arenaSizeProp.GetInt32();
                }
            }

            snake.SegmentSize = segmentSize;
            snake.ArenaSize = arenaSize;

            var cubeMesh = Assets.CreateBox(segmentSize, segmentSize, segmentSize);
            var headMaterial = GetOrCreateMaterial("SnakeHead", () => new AnimatedSnakeMaterial("SnakeHead", 50, 200, 50));
            var bodyMaterial = GetOrCreateMaterial("SnakeBody", () => new AnimatedSnakeMaterial("SnakeBody", 30, 150, 30));

            snake.HeadMesh = cubeMesh;
            snake.HeadMaterial = headMaterial;
            snake.BodyMesh = cubeMesh;
            snake.BodyMaterial = bodyMaterial;

            return snake;
        });

        GameObjectRegistry.Register("FoodSpawner", props =>
        {
            var spawner = new FoodSpawner();

            var arenaSize = 15;
            var foodSize = 0.8f;

            if (props.HasValue)
            {
                if (props.Value.TryGetProperty("arenaSize", out var arenaSizeProp))
                {
                    arenaSize = arenaSizeProp.GetInt32();
                }

                if (props.Value.TryGetProperty("foodSize", out var foodSizeProp))
                {
                    foodSize = foodSizeProp.GetSingle();
                }
            }

            spawner.ArenaSize = arenaSize;
            spawner.FoodMesh = Assets.CreateSphere(foodSize);
            spawner.FoodMaterial = GetOrCreateMaterial("Food", () => new GlowingFoodMaterial("Food", 255, 100, 50));

            return spawner;
        });
    }

    private static Material GetOrCreateMaterial(string name, Func<Material> factory)
    {
        var existing = Assets.GetMaterial(name);
        if (existing != null)
        {
            return existing;
        }

        var material = factory();
        Assets.RegisterMaterial(material);
        return material;
    }

    private void RenderUi(UiFrame ui)
    {
        var snake = World.FindObjectOfType<Snake>();

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
