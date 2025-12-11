using DenOfIz;

namespace DZForestDemo;

public class WindowRenderLoop : IDisposable
{
    private readonly LogicalDevice _logicalDevice;
    private readonly Window _window;
    private readonly CommandQueue _commandQueue;
    private readonly SwapChain _swapChain;
    private readonly FrameSync _frameSync;
    private readonly ResourceTracking _resourceTracking;
    private readonly Game _game;
    private readonly Viewport _viewport;
    private readonly SemaphoreArray _emptySemaphoreArray;
    private bool _disposed;

    public WindowRenderLoop(LogicalDevice logicalDevice, uint width, uint height, string title)
    {
        _logicalDevice = logicalDevice;

        _window = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title)
        });

        var commandQueueDesc = new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        };
        _commandQueue = _logicalDevice.CreateCommandQueue(commandQueueDesc);

        var swapChainDesc = new SwapChainDesc
        {
            AllowTearing = true,
            BackBufferFormat = Format.B8G8R8A8Unorm,
            DepthBufferFormat = Format.D32Float,
            CommandQueue = _commandQueue,
            WindowHandle = _window.GetGraphicsWindowHandle(),
            Width = width,
            Height = height,
            NumBuffers = 3
        };
        _swapChain = _logicalDevice.CreateSwapChain(swapChainDesc);

        _frameSync = new FrameSync(new FrameSyncDesc
        {
            Device = _logicalDevice,
            CommandQueue = _commandQueue,
            SwapChain = _swapChain,
            NumFrames = 3
        });

        _window.Show();

        _resourceTracking = new ResourceTracking();
        const uint numFrames = 3;
        for (uint i = 0; i < numFrames; ++i)
        {
            _resourceTracking.TrackTexture(
                _swapChain.GetRenderTarget(i),
                (uint)ResourceUsageFlagBits.Common,
                QueueType.Graphics
            );
        }

        _emptySemaphoreArray = new SemaphoreArray();
        _viewport = _swapChain.GetViewport();

        _game = new Game(_logicalDevice, _resourceTracking);
    }

    public bool PollAndRender()
    {
        while (InputSystem.PollEvent(out var ev))
        {
            switch (ev.Type)
            {
                case EventType.Quit:
                case EventType.KeyDown when ev.Key.KeyCode == KeyCode.Escape:
                    return false;
            }

            _game.HandleEvent(ev);
        }

        var frameIndex = _frameSync.NextFrame();
        var image = _frameSync.AcquireNextImage();

        var renderTarget = _swapChain.GetRenderTarget(image);
        var commandList = _frameSync.GetCommandList(frameIndex);
        _game.Render(_viewport, commandList, renderTarget, frameIndex);
        _frameSync.ExecuteCommandList(frameIndex, _emptySemaphoreArray);
        _frameSync.Present(image);

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _frameSync.WaitIdle();
        _commandQueue.WaitIdle();

        _frameSync.Dispose();
        _swapChain.Dispose();
        _commandQueue.Dispose();
    }
}