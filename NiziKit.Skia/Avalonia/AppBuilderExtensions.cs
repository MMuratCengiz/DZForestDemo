using Avalonia;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Extension methods for configuring Avalonia to use the DenOfIz platform backend.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Configures Avalonia to use the DenOfIz platform for rendering.
    /// Avalonia will render to DenOfIz textures which can be displayed in the game.
    /// </summary>
    public static AppBuilder UseDenOfIz(this AppBuilder builder)
    {
        // Initialize platform before anything else
        DenOfIzPlatform.Initialize();

        return builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .UseWindowingSubsystem(DenOfIzPlatform.InitializeWindowing);
    }
}
