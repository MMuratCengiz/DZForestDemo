using DenOfIz;
using Graphics.RenderGraph;

namespace Graphics;

public interface IGraphicsContext
{
    GraphicsApi GraphicsApi { get; }
    LogicalDevice LogicalDevice { get; }
    SwapChain SwapChain { get; }
    CommandQueue GraphicsCommandQueue { get; }
    CommandQueue ComputeCommandQueue { get; }
    CommandQueue CopyCommandQueue { get; }
    ResourceTracking ResourceTracking { get; }
    RenderGraph.RenderGraph RenderGraph { get; }

    uint NumFrames { get; }
    Format BackBufferFormat { get; }
    Format DepthBufferFormat { get; }
    uint FrameIndex { get; }
    uint Width { get; }
    uint Height { get; }
    ResourceHandle SwapchainRenderTarget { get; }

    void WaitIdle();
}
