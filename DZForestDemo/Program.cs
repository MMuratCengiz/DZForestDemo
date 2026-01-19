using DZForestDemo.Tools;
using NiziKit.Application;

namespace DZForestDemo;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Asset export commands
        if (args.Length >= 3 && args[0] == "--export-synty")
        {
            var sourceFolder = args[1];
            var outputFolder = args[2];
            SyntyAssetExporter.ExportRacingAssets(sourceFolder, outputFolder);
            if (args.Length > 3 && args[3] == "--with-textures")
            {
                var textureFolder = Path.Combine(Path.GetDirectoryName(sourceFolder)!, "Textures");
                SyntyAssetExporter.CopyTextures(textureFolder, outputFolder);
            }
            return;
        }

        if (args.Length >= 3 && args[0] == "--export-all")
        {
            var sourceFolder = args[1];
            var outputFolder = args[2];
            SyntyAssetExporter.ExportAllModels(sourceFolder, outputFolder);
            return;
        }

        // Game modes
        var gameMode = args.Length > 0 ? args[0] : "";

        switch (gameMode)
        {
            case "--snake":
                Game.Run<SnakeGame>(new GameDesc
                {
                    Title = "Snake 3D - WASD to move, R to restart",
                    Width = 2560,
                    Height = 1440
                });
                break;

            case "--race":
                Game.Run<RaceGame>(new GameDesc
                {
                    Title = "Street Racer - WASD to drive, R to restart",
                    Width = 1920,
                    Height = 1080
                });
                break;

            case "--test":
                Game.Run<TestGame>(new GameDesc
                {
                    Title = "JSON Scene Test - Press R to reload, ESC to quit",
                    Width = 1920,
                    Height = 1080
                });
                break;

            default:
                Game.Run<DemoGame>(new GameDesc
                {
                    Title = "DenOfIz Scene Demo - Press F1/F2 to switch scenes",
                    Width = 1920,
                    Height = 1080
                });
                break;
        }
    }
}
