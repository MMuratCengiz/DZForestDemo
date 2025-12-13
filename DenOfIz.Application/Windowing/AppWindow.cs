using DenOfIz;

namespace Application.Windowing;

public sealed class AppWindow(string title, uint width, uint height) : IDisposable
{
    private bool _disposed;

    public uint Width { get; private set; } = width;
    public uint Height { get; private set; } = height;
    public bool IsMinimized { get; private set; }
    public bool HasFocus { get; private set; } = true;
    public GraphicsWindowHandle GraphicsHandle => NativeWindow.GetGraphicsWindowHandle();
    public Window NativeWindow { get; } = new(new WindowDesc
    {
        Width = (int)width,
        Height = (int)height,
        Title = StringView.Create(title)
    });

    public void Show() => NativeWindow.Show();

    public void Hide() => NativeWindow.Hide();

    internal void HandleWindowEvent(WindowEventType eventType, int data1, int data2)
    {
        switch (eventType)
        {
            case WindowEventType.Resized:
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        NativeWindow.Dispose();
        GC.SuppressFinalize(this);
    }
}
