using DenOfIz;
using Graphics.Binding;
using Graphics.Renderer;
using Graphics.RenderGraph;
using Graphics.RootSignatures;

namespace Graphics;

public sealed class GraphicsContext : IGraphicsContext, IDisposable
{
    private bool _disposed;

    public GraphicsApi GraphicsApi { get; }
    public LogicalDevice LogicalDevice { get; }
    public SwapChain SwapChain { get; }
    public CommandQueue GraphicsQueue { get; }
    public CommandQueue ComputeQueue { get; }
    public CommandQueue CopyQueue { get; }
    public ResourceTracking ResourceTracking { get; } = new();
    public RenderGraph.RenderGraph RenderGraph { get; }

    public CommandQueue GraphicsCommandQueue => GraphicsQueue;
    public CommandQueue ComputeCommandQueue => ComputeQueue;
    public CommandQueue CopyCommandQueue => CopyQueue;

    public uint NumFrames { get; }
    public Format BackBufferFormat { get; }
    public Format DepthBufferFormat { get; }
    public uint FrameIndex { get; private set; }
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public ResourceHandle SwapchainRenderTarget { get; private set; }
    public UniformBufferArena UniformBufferArena { get; }
    public BindGroupLayoutStore BindGroupLayoutStore { get; }
    public RootSignatureStore RootSignatureStore { get; }
    
    public NullTexture NullTexture { get; }
    

    public GraphicsContext(Window window, GraphicsDesc? desc = null, IRenderer? renderer = null)
    {
        desc ??= new GraphicsDesc();

        NumFrames = desc.NumFrames;
        BackBufferFormat = desc.BackBufferFormat;
        DepthBufferFormat = desc.DepthBufferFormat;

        GraphicsApi = new GraphicsApi(desc.ApiPreference);
        LogicalDevice = GraphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc
        {
#if DEBUG
            EnableValidationLayers = true
#endif
        });

        GraphicsQueue = LogicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Graphics });
        ComputeQueue = LogicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Compute });
        CopyQueue = LogicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Copy });

        Width = (uint)window.GetSize().Width;
        Height = (uint)window.GetSize().Height;

        SwapChain = LogicalDevice.CreateSwapChain(new SwapChainDesc
        {
            AllowTearing = desc.AllowTearing,
            BackBufferFormat = desc.BackBufferFormat,
            DepthBufferFormat = desc.DepthBufferFormat,
            CommandQueue = GraphicsQueue,
            WindowHandle = window.GetGraphicsWindowHandle(),
            Width = Width,
            Height = Height,
            NumBuffers = desc.NumFrames
        });

        RenderGraph = new RenderGraph.RenderGraph(new RenderGraphDesc
        {
            LogicalDevice = LogicalDevice,
            CommandQueue = GraphicsQueue,
            ResourceTracking = ResourceTracking,
            NumFrames = NumFrames
        });
        RenderGraph.SetDimensions(Width, Height);

        for (uint i = 0; i < NumFrames; ++i)
        {
            ResourceTracking.TrackTexture(SwapChain.GetRenderTarget(i), QueueType.Graphics);
        }

        UniformBufferArena = new UniformBufferArena(LogicalDevice);
        BindGroupLayoutStore = new BindGroupLayoutStore(LogicalDevice);
        RootSignatureStore = new RootSignatureStore(LogicalDevice, BindGroupLayoutStore);
        NullTexture = new NullTexture(LogicalDevice);
    }

    public void BeginFrame()
    {
        FrameIndex = (FrameIndex + 1) % NumFrames;
        RenderGraph.BeginFrame(FrameIndex);

        var imageIndex = SwapChain.AcquireNextImage();
        var renderTarget = SwapChain.GetRenderTarget(imageIndex);
        SwapchainRenderTarget = RenderGraph.ImportTexture("SwapchainRT", renderTarget);
    }

    public void Render()
    {
    }

    public void EndFrame()
    {
        RenderGraph.Compile();
        RenderGraph.Execute();

        switch (SwapChain.Present(FrameIndex))
        {
            case PresentResult.Success:
            case PresentResult.Suboptimal:
                break;
            case PresentResult.Timeout:
            case PresentResult.DeviceLost:
                WaitIdle();
                break;
        }
    }

    public void Resize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        WaitIdle();
        SwapChain.Resize(width, height);
        Width = width;
        Height = height;
        RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < NumFrames; ++i)
        {
            ResourceTracking.TrackTexture(SwapChain.GetRenderTarget(i), QueueType.Graphics);
        }
    }

    public void WaitIdle()
    {
        LogicalDevice.WaitIdle();
        GraphicsQueue.WaitIdle();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        WaitIdle();
        RenderGraph.Dispose();
        ResourceTracking.Dispose();
        SwapChain.Dispose();
        CopyQueue.Dispose();
        ComputeQueue.Dispose();
        GraphicsQueue.Dispose();
        LogicalDevice.Dispose();
        GraphicsApi.ReportLiveObjects();
    }
}
