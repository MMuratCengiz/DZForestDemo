using DenOfIz.World.Graphics.Binding;
using DenOfIz.World.Graphics.Graph;
using DenOfIz.World.Graphics.RootSignatures;

namespace DenOfIz.World.Graphics;

public sealed class GraphicsContext : IDisposable
{
    private bool _disposed;

    public GraphicsApi GraphicsApi { get; }
    public LogicalDevice LogicalDevice { get; }
    public SwapChain SwapChain { get; }
    public CommandQueue GraphicsQueue { get; }
    public CommandQueue ComputeQueue { get; }
    public CommandQueue CopyQueue { get; }
    public ResourceTracking ResourceTracking { get; } = new();
    public RenderGraph RenderGraph { get; }

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
    

    public GraphicsContext(Window window, GraphicsDesc? desc = null)
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
        
        for (uint i = 0; i < NumFrames; ++i)
        {
            ResourceTracking.TrackTexture(SwapChain.GetRenderTarget(i), QueueType.Graphics);
        }

        UniformBufferArena = new UniformBufferArena(LogicalDevice);
        BindGroupLayoutStore = new BindGroupLayoutStore(LogicalDevice);
        RootSignatureStore = new RootSignatureStore(LogicalDevice, BindGroupLayoutStore);
        NullTexture = new NullTexture(LogicalDevice);
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
