using Avalonia.Platform;

namespace NiziKit.Skia.Avalonia;

/// <summary>
/// Stub windowing platform for DenOfIz.
/// Sub-windows are not supported - all UI renders to textures displayed by DenOfIz.
/// </summary>
internal sealed class DenOfIzWindowingPlatform : IWindowingPlatform
{
    public IWindowImpl CreateWindow()
        => throw new NotSupportedException("DenOfIz platform does not support creating windows. Use DenOfIzTopLevel instead.");

    public IWindowImpl CreateEmbeddableWindow()
        => throw new NotSupportedException("DenOfIz platform does not support embeddable windows. Use DenOfIzTopLevel instead.");

    public ITopLevelImpl CreateEmbeddableTopLevel()
        => throw new NotSupportedException("Use DenOfIzTopLevel.Create() to create top-level surfaces.");

    public ITrayIconImpl? CreateTrayIcon()
        => null;
}
