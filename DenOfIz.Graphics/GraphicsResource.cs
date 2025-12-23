using DenOfIz;
using ECS;
using Graphics.RenderGraph;

namespace Graphics;

public class GraphicsResource : IResource, IDisposable
{
    public GraphicsResource(
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
    {
        GraphicsApi = graphicsApi;
        LogicalDevice = logicalDevice;
        SwapChain = swapChain;
        Window = window;
        GraphicsCommandQueue = graphicsCommandQueue;
        ComputeCommandQueue = computeCommandQueue;
        CopyCommandQueue = copyCommandQueue;
        NumFrames = numFrames;
        BackBufferFormat = backBufferFormat;
        DepthBufferFormat = depthBufferFormat;
        Width = width;
        Height = height;

        RenderGraph = new RenderGraph.RenderGraph(new RenderGraphDesc(logicalDevice, graphicsCommandQueue, ResourceTracking)
        {
            NumFrames = numFrames
        });
        RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < numFrames; ++i)
        {
            ResourceTracking.TrackTexture(swapChain.GetRenderTarget(i), QueueType.Graphics);
        }
    }

    public GraphicsApi GraphicsApi { get; }
    public LogicalDevice LogicalDevice { get; }
    public SwapChain SwapChain { get; }
    public Window Window { get; }
    public CommandQueue GraphicsCommandQueue { get; }
    public CommandQueue ComputeCommandQueue { get; }
    public CommandQueue CopyCommandQueue { get; }
    public ResourceTracking ResourceTracking { get; } = new();
    public RenderGraph.RenderGraph RenderGraph { get; }

    public uint NumFrames { get; }
    public Format BackBufferFormat { get; }
    public Format DepthBufferFormat { get; }
    public uint FrameIndex { get; internal set; }
    public uint Width { get; internal set; }
    public uint Height { get; internal set; }
    public ResourceHandle SwapchainRenderTarget { get; internal set; }

    public void Dispose()
    {
        WaitIdle();
        RenderGraph.Dispose();
        ResourceTracking.Dispose();
        SwapChain.Dispose();
        CopyCommandQueue.Dispose();
        ComputeCommandQueue.Dispose();
        GraphicsCommandQueue.Dispose();
        LogicalDevice.Dispose();
        GraphicsApi.ReportLiveObjects();
        GC.SuppressFinalize(this);
    }

    public void WaitIdle()
    {
        LogicalDevice.WaitIdle();
        GraphicsCommandQueue.WaitIdle();
    }
}