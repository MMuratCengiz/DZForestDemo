using Avalonia;
using Avalonia.Platform;
using DenOfIz;

namespace NiziKit.Skia.Avalonia;

internal sealed class DenOfIzScreenImpl : IScreenImpl
{
    public int ScreenCount => 1;

    public IReadOnlyList<Screen> AllScreens => GetScreens();

    public Action? Changed { get; set; }

    public Screen? ScreenFromPoint(PixelPoint point)
    {
        var screens = AllScreens;
        return screens.Count > 0 ? screens[0] : null;
    }

    public Screen? ScreenFromRect(PixelRect rect)
    {
        var screens = AllScreens;
        return screens.Count > 0 ? screens[0] : null;
    }

    public Screen? ScreenFromWindow(IWindowBaseImpl window)
    {
        var screens = AllScreens;
        return screens.Count > 0 ? screens[0] : null;
    }

    public Screen? ScreenFromTopLevel(ITopLevelImpl topLevel)
    {
        var screens = AllScreens;
        return screens.Count > 0 ? screens[0] : null;
    }

    public Task<bool> RequestScreenDetails()
    {
        return Task.FromResult(true);
    }

    private static Screen[] GetScreens()
    {
        var display = Display.GetPrimaryDisplay();
        var scale = display.DpiScale > 0 ? display.DpiScale : 1.0;
        var bounds = new PixelRect(0, 0, display.Size.Width, display.Size.Height);
        return [new Screen(scale, bounds, bounds, true)];
    }
}
