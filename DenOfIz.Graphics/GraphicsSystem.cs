using System.Runtime.CompilerServices;
using DenOfIz;
using Flecs.NET.Core;

namespace Graphics;

/// <summary>
/// Custom phases for rendering pipeline.
/// </summary>
public struct PreRender;
public struct Render;
public struct PostRender;

/// <summary>
/// Registers graphics systems and phases.
/// </summary>
public static class GraphicsSystems
{
    /// <summary>
    /// Initialize graphics phases in the pipeline.
    /// Call this before registering graphics systems.
    /// </summary>
    public static void InitPhases(World world)
    {
        // PreRender runs after OnUpdate
        world.Component<PreRender>().Entity
            .Add(Ecs.DependsOn, Ecs.OnUpdate)
            .Add(Ecs.Phase);

        // Render runs after PreRender
        world.Component<Render>().Entity
            .Add(Ecs.DependsOn, world.Entity<PreRender>())
            .Add(Ecs.Phase);

        // PostRender runs after Render
        world.Component<PostRender>().Entity
            .Add(Ecs.DependsOn, world.Entity<Render>())
            .Add(Ecs.Phase);
    }

    /// <summary>
    /// Register the frame preparation system.
    /// </summary>
    public static void RegisterPrepareFrame(World world)
    {
        world.System("PrepareFrame")
            .Kind<PreRender>()
            .Run((Iter _) =>
            {
                ref var ctx = ref world.GetMut<GraphicsResource>();
                ctx.FrameIndex = (ctx.FrameIndex + 1) % ctx.NumFrames;
                ctx.RenderGraph.BeginFrame(ctx.FrameIndex);

                var imageIndex = ctx.SwapChain.AcquireNextImage();
                var renderTarget = ctx.SwapChain.GetRenderTarget(imageIndex);
                ctx.SwapchainRenderTarget = ctx.RenderGraph.ImportTexture("SwapchainRT", renderTarget);
            });
    }

    /// <summary>
    /// Register the frame presentation system.
    /// </summary>
    public static void RegisterPresentFrame(World world)
    {
        world.System("PresentFrame")
            .Kind<PostRender>()
            .Run((Iter _) =>
            {
                ref var ctx = ref world.GetMut<GraphicsResource>();
                ctx.RenderGraph.Compile();
                ctx.RenderGraph.Execute();

                switch (ctx.SwapChain.Present(ctx.FrameIndex))
                {
                    case PresentResult.Success:
                    case PresentResult.Suboptimal:
                        break;
                    case PresentResult.Timeout:
                    case PresentResult.DeviceLost:
                        ctx.WaitIdle();
                        break;
                }
            });
    }
    
    public static void HandleResize(World world, uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        ref var ctx = ref world.GetMut<GraphicsResource>();
        ctx.WaitIdle();
        ctx.SwapChain.Resize(width, height);
        ctx.Width = width;
        ctx.Height = height;
        ctx.RenderGraph.SetDimensions(width, height);

        for (uint i = 0; i < ctx.NumFrames; ++i)
        {
            ctx.ResourceTracking.TrackTexture(ctx.SwapChain.GetRenderTarget(i), QueueType.Graphics);
        }
    }

    public static void WaitIdle(World world)
    {
        if (world.Has<GraphicsResource>())
        {
            world.GetMut<GraphicsResource>().WaitIdle();
        }
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
        var logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc()
        {
#if DEBUG
            EnableValidationLayers = false
#endif
        });

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

        world.Set(context);

        // Initialize phases and register systems
        GraphicsSystems.InitPhases(world);
        GraphicsSystems.RegisterPrepareFrame(world);
        GraphicsSystems.RegisterPresentFrame(world);
    }
}
