using Avalonia;

namespace NiziKit.Skia.Avalonia;


internal static class AppBuilderExtensions
{
    public static AppBuilder UseDenOfIz(this AppBuilder builder)
    {
        DenOfIzPlatform.Initialize();
        return builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .UseWindowingSubsystem(DenOfIzPlatform.InitializeWindowing);
    }
}
