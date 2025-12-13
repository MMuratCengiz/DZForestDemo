using DenOfIz;
using ECS;

namespace Graphics;

public class GraphicsContext(
    GraphicsApi graphicsApi,
    LogicalDevice logicalDevice,
    SwapChain swapChain,
    Window window,
    CommandQueue graphicsCommandQueue,
    CommandQueue computeCommandQueue,
    CommandQueue copyCommandQueue,
    uint numFrames,
    Format backBufferFormat,
    Format depthBufferFormat,
    uint width,
    uint height)
    : IContext, IDisposable
{
    public GraphicsApi GraphicsApi { get; } = graphicsApi;
    public LogicalDevice LogicalDevice { get; } = logicalDevice;
    public SwapChain SwapChain { get; } = swapChain;
    public Window Window { get; } = window;
    public CommandQueue GraphicsCommandQueue { get; } = graphicsCommandQueue;
    public CommandQueue ComputeCommandQueue { get; } = computeCommandQueue;
    public CommandQueue CopyCommandQueue { get; } = copyCommandQueue;
    public ResourceTracking ResourceTracking { get; } = new();

    public uint NumFrames { get; } = numFrames;
    public Format BackBufferFormat { get; } = backBufferFormat;
    public Format DepthBufferFormat { get; } = depthBufferFormat;
    public uint FrameIndex { get; internal set; }
    public uint Width { get; internal set; } = width;
    public uint Height { get; internal set; } = height;

    public void WaitIdle()
    {
        LogicalDevice.WaitIdle();
        GraphicsCommandQueue.WaitIdle();
    }

    public void Dispose()
    {
        WaitIdle();
        SwapChain.Dispose();
        CopyCommandQueue.Dispose();
        ComputeCommandQueue.Dispose();
        GraphicsCommandQueue.Dispose();
        LogicalDevice.Dispose();
        GraphicsApi.ReportLiveObjects();
        GC.SuppressFinalize(this);
    }
}