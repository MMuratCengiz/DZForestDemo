using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;
using Graphics.RenderGraph;

namespace Graphics;

public class GraphicsSystem(
    Window window,
    APIPreference? apiPreference = null,
    uint numFrames = 3,
    Format backBufferFormat = Format.B8G8R8A8Unorm,
    Format depthBufferFormat = Format.D32Float,
    bool allowTearing = true)
    : ISystem
{
    private readonly APIPreference _apiPreference = apiPreference ?? new APIPreference { Windows = APIPreferenceWindows.Directx12 };

    private World _world = null!;
    private bool _disposed;

    private GraphicsContext Context { get; set; } = null!;

    public RenderGraph.RenderGraph RenderGraph { get; private set; } = null!;

    public ResourceHandle SwapchainRenderTarget { get; private set; }

    public void Initialize(World world)
    {
        _world = world;

        var graphicsApi = new GraphicsApi(_apiPreference);
        var logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc());

        var graphicsQueue = logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Graphics });
        var computeQueue = logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Compute });
        var copyQueue = logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Copy });

        var width = (uint)window.GetSize().Width;
        var height = (uint)window.GetSize().Height;

        var swapChain = logicalDevice.CreateSwapChain(new SwapChainDesc
        {
            AllowTearing = allowTearing,
            BackBufferFormat = backBufferFormat,
            DepthBufferFormat = depthBufferFormat,
            CommandQueue = graphicsQueue,
            WindowHandle = window.GetGraphicsWindowHandle(),
            Width = width,
            Height = height,
            NumBuffers = numFrames
        });

        Context = new GraphicsContext(
            graphicsApi,
            logicalDevice,
            swapChain,
            window,
            graphicsQueue,
            computeQueue,
            copyQueue,
            numFrames,
            backBufferFormat,
            depthBufferFormat,
            width,
            height);

        _world.RegisterContext(Context);

        RenderGraph = new RenderGraph.RenderGraph(new RenderGraphDesc
        {
            LogicalDevice = logicalDevice,
            CommandQueue = graphicsQueue,
            NumFrames = numFrames
        });
        RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < numFrames; ++i)
        {
            Context.ResourceTracking.TrackTexture(
                swapChain.GetRenderTarget(i),
                (uint)ResourceUsageFlagBits.Common,
                QueueType.Graphics);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame()
    {
        Context.FrameIndex = (Context.FrameIndex + 1) % Context.NumFrames;
        RenderGraph.BeginFrame(Context.FrameIndex);

        var imageIndex = Context.SwapChain.AcquireNextImage();
        var renderTarget = Context.SwapChain.GetRenderTarget(imageIndex);
        SwapchainRenderTarget = RenderGraph.ImportTexture("SwapchainRT", renderTarget);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndFrame()
    {
        RenderGraph.Compile();
        RenderGraph.Execute();

        switch (Context.SwapChain.Present(Context.FrameIndex))
        {
            case PresentResult.Success:
            case PresentResult.Suboptimal:
                break;
            case PresentResult.Timeout:
            case PresentResult.DeviceLost:
                Context.WaitIdle();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool OnEvent(ref Event ev)
    {
        if (ev is { Type: EventType.WindowEvent, Window.Event: WindowEventType.Resized })
        {
            HandleResize((uint)ev.Window.Data1, (uint)ev.Window.Data2);
        }

        return false;
    }

    private void HandleResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        Context.WaitIdle();
        Context.SwapChain.Resize(width, height);
        Context.Width = width;
        Context.Height = height;
        RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < Context.NumFrames; ++i)
        {
            Context.ResourceTracking.TrackTexture(
                Context.SwapChain.GetRenderTarget(i),
                (uint)ResourceUsageFlagBits.Common,
                QueueType.Graphics);
        }
    }

    public void Shutdown()
    {
        Context.WaitIdle();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Context.WaitIdle();
        RenderGraph.Dispose();

        GC.SuppressFinalize(this);
    }
}
