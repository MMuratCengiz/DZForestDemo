using DenOfIz;

namespace Application.Windowing;

/// <summary>
/// Wrapper around the native window that provides additional state tracking.
/// </summary>
public sealed class AppWindow : IDisposable
{
    private readonly Window _window;
    private bool _disposed;

    /// <summary>Gets the current window width in pixels.</summary>
    public uint Width { get; private set; }

    /// <summary>Gets the current window height in pixels.</summary>
    public uint Height { get; private set; }

    /// <summary>Gets whether the window is minimized.</summary>
    public bool IsMinimized { get; private set; }

    /// <summary>Gets whether the window has input focus.</summary>
    public bool HasFocus { get; private set; } = true;

    /// <summary>Gets the native window handle for graphics API interop.</summary>
    public GraphicsWindowHandle GraphicsHandle => _window.GetGraphicsWindowHandle();

    /// <summary>
    /// Creates a new application window.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="width">Initial width in pixels.</param>
    /// <param name="height">Initial height in pixels.</param>
    public AppWindow(string title, uint width, uint height)
    {
        Width = width;
        Height = height;

        _window = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title)
        });
    }

    /// <summary>
    /// Shows the window.
    /// </summary>
    public void Show() => _window.Show();

    /// <summary>
    /// Hides the window.
    /// </summary>
    public void Hide() => _window.Hide();

    /// <summary>
    /// Updates window state from a window event.
    /// Called internally by the application.
    /// </summary>
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
        _window.Dispose();
        GC.SuppressFinalize(this);
    }
}
