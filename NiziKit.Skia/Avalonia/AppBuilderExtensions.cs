using Avalonia;
using Avalonia.Headless;

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
        // Initialize our platform resources first
        DenOfIzPlatform.EnsureInitialized();

        // Use headless platform for dispatcher/compositor setup
        // then override with our GPU-accelerated services
        return builder
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false // We'll do our own GPU rendering
            })
            .AfterSetup(_ => DenOfIzPlatform.CompleteInitialization());
    }
}
