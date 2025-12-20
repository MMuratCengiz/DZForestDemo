using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;

namespace Graphics;

public class PrepareFrameSystem : ISystem
{
    private GraphicsResource _ctx = null!;

    public void Initialize(World world)
    {
        _ctx = world.GetResource<GraphicsResource>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        _ctx.FrameIndex = (_ctx.FrameIndex + 1) % _ctx.NumFrames;
        _ctx.RenderGraph.BeginFrame(_ctx.FrameIndex);

        var imageIndex = _ctx.SwapChain.AcquireNextImage();
        var renderTarget = _ctx.SwapChain.GetRenderTarget(imageIndex);
        _ctx.SwapchainRenderTarget = _ctx.RenderGraph.ImportTexture("SwapchainRT", renderTarget);
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

    public void Shutdown()
    {
        _ctx.WaitIdle();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void HandleResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _ctx.WaitIdle();
        _ctx.SwapChain.Resize(width, height);
        _ctx.Width = width;
        _ctx.Height = height;
        _ctx.RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < _ctx.NumFrames; ++i)
        {
            _ctx.ResourceTracking.TrackTexture(_ctx.SwapChain.GetRenderTarget(i), QueueType.Graphics);
        }
    }
}

public class PresentFrameSystem : ISystem
{
    private GraphicsResource _ctx = null!;

    public void Initialize(World world)
    {
        _ctx = world.GetResource<GraphicsResource>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run()
    {
        _ctx.RenderGraph.Compile();
        _ctx.RenderGraph.Execute();

        switch (_ctx.SwapChain.Present(_ctx.FrameIndex))
        {
            case PresentResult.Success:
            case PresentResult.Suboptimal:
                break;
            case PresentResult.Timeout:
            case PresentResult.DeviceLost:
                _ctx.WaitIdle();
                break;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public class GraphicsPlugin(
    Window window,
    APIPreference? apiPreference = null,
    uint numFrames = 3,
    Format backBufferFormat = Format.B8G8R8A8Unorm,
    Format depthBufferFormat = Format.D32Float,
    bool allowTearing = true)
{
    private readonly APIPreference _apiPreference =
        apiPreference ?? new APIPreference { Windows = APIPreferenceWindows.Directx12 };

    public void Build(World world)
    {
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

        var context = new GraphicsResource(
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

        world.RegisterResource(context);
        world.AddSystem(new PrepareFrameSystem(), Schedule.PreRender);
        world.AddSystem(new PresentFrameSystem(), Schedule.PostRender);
    }
}