using DenOfIz;

namespace NiziKit.Application.Windowing;

public sealed class AppWindow : IDisposable
{
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public bool IsMinimized { get; private set; }
    public bool HasFocus { get; private set; } = true;
    public GraphicsWindowHandle GraphicsHandle => NativeWindow.GetGraphicsWindowHandle();

    public Window NativeWindow { get; }

    public AppWindow(string title, uint width, uint height, WindowFlags flags)
    {
        NativeWindow = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title),
            Flags = flags
        });

        if (flags.Maximized)
        {
            NativeWindow.Sync();
        }

        var pixelSize = NativeWindow.GetSizeInPixels();
        Width = (uint)pixelSize.Width;
        Height = (uint)pixelSize.Height;
    }

    public void Dispose()
    {
        NativeWindow.Dispose();
    }

    public void Show()
    {
        NativeWindow.Show();
    }

    public void Hide()
    {
        NativeWindow.Hide();
    }

    internal void HandleWindowEvent(WindowEventType eventType, int data1, int data2)
    {
        switch (eventType)
        {
            case WindowEventType.SizeChanged:
                Width = (uint)data1;
                Height = (uint)data2;
                break;
            case WindowEventType.Minimized:
                IsMinimized = true;
                break;
            case WindowEventType.Restored:
                IsMinimized = false;
                break;
            case WindowEventType.FocusGained:
                HasFocus = true;
                break;
            case WindowEventType.FocusLost:
                HasFocus = false;
                break;
        }
    }
}
