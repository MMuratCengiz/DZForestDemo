using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.RootSignatures;

namespace NiziKit.Graphics;

public sealed class GraphicsContext : IDisposable
{
    private static GraphicsContext? _instance;
    public static GraphicsContext Instance => _instance ?? throw new InvalidOperationException("GraphicsContext not initialized");

    public static GraphicsApi GraphicsApi => Instance._graphicsApi;
    public static LogicalDevice Device => Instance._logicalDevice;
    public static SwapChain SwapChain => Instance._swapChain;
    public static CommandQueue GraphicsQueue => Instance._graphicsQueue;
    public static CommandQueue ComputeQueue => Instance._computeQueue;
    public static CommandQueue CopyQueue => Instance._copyQueue;
    public static ResourceTracking ResourceTracking => Instance._resourceTracking;

    public static CommandQueue GraphicsCommandQueue => Instance._graphicsQueue;
    public static CommandQueue ComputeCommandQueue => Instance._computeQueue;
    public static CommandQueue CopyCommandQueue => Instance._copyQueue;

    public static uint NumFrames => Instance._numFrames;
    public static Format BackBufferFormat => Instance._backBufferFormat;
    public static Format DepthBufferFormat => Instance._depthBufferFormat;
    public static int FrameIndex => Instance._currentFrame;
    public static uint Width => Instance._width;
    public static uint Height => Instance._height;
    public static UniformBufferArena UniformBufferArena => Instance._uniformBufferArena;
    public static BindGroupLayoutStore BindGroupLayoutStore => Instance._bindGroupLayoutStore;
    public static RootSignatureStore RootSignatureStore => Instance._rootSignatureStore;
    public static void Resize(uint width, uint height) => Instance._Resize(width, height);
    public static void BeginFrame() => Instance._BeginFrame();
    public static void WaitIdle() => Instance._WaitIdle();

    private readonly GraphicsApi _graphicsApi;
    private readonly LogicalDevice _logicalDevice;
    private readonly SwapChain _swapChain;
    private readonly CommandQueue _graphicsQueue;
    private readonly CommandQueue _computeQueue;
    private readonly CommandQueue _copyQueue;
    private readonly ResourceTracking _resourceTracking = new();
    private static readonly Lock _gpuLock = new();

    public static Lock GpuLock => _gpuLock;

    private readonly uint _numFrames;
    private readonly Format _backBufferFormat;
    private readonly Format _depthBufferFormat;
    private int _currentFrame = 0;
    private int _nextFrame = 0;
    private uint _width;
    private uint _height;
    private readonly UniformBufferArena _uniformBufferArena;
    private readonly BindGroupLayoutStore _bindGroupLayoutStore;
    private readonly RootSignatureStore _rootSignatureStore;

    public GraphicsContext(Window window, GraphicsDesc? desc = null)
    {
        desc ??= new GraphicsDesc();

        _numFrames = desc.NumFrames;
        _backBufferFormat = desc.BackBufferFormat;
        _depthBufferFormat = desc.DepthBufferFormat;

        _graphicsApi = new GraphicsApi(desc.ApiPreference);
        _logicalDevice = _graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc
        {
#if DEBUG
            EnableValidationLayers = true
#endif
        });

        _graphicsQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Graphics });
        _computeQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Compute });
        _copyQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Copy });

        _width = (uint)window.GetSize().Width;
        _height = (uint)window.GetSize().Height;

        _swapChain = _logicalDevice.CreateSwapChain(new SwapChainDesc
        {
            AllowTearing = desc.AllowTearing,
            BackBufferFormat = desc.BackBufferFormat,
            DepthBufferFormat = desc.DepthBufferFormat,
            CommandQueue = _graphicsQueue,
            WindowHandle = window.GetGraphicsWindowHandle(),
            Width = _width,
            Height = _height,
            NumBuffers = desc.NumFrames
        });

        for (uint i = 0; i < _numFrames; ++i)
        {
            _resourceTracking.TrackTexture(_swapChain.GetRenderTarget(i), QueueType.Graphics);
        }

        _uniformBufferArena = new UniformBufferArena(_logicalDevice);
        _bindGroupLayoutStore = new BindGroupLayoutStore(_logicalDevice);
        _rootSignatureStore = new RootSignatureStore(_logicalDevice, _bindGroupLayoutStore);
        _instance = this;
    }

    /// <summary>
    /// Creates a headless GraphicsContext without a SwapChain.
    /// Use this for offscreen rendering, editor applications, or compute-only workloads.
    /// </summary>
    public GraphicsContext(GraphicsDesc? desc = null)
    {
        desc ??= new GraphicsDesc();

        _numFrames = desc.NumFrames;
        _backBufferFormat = desc.BackBufferFormat;
        _depthBufferFormat = desc.DepthBufferFormat;

        _graphicsApi = new GraphicsApi(desc.ApiPreference);
        _logicalDevice = _graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc
        {
#if DEBUG
            EnableValidationLayers = true
#endif
        });

        _graphicsQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Graphics });
        _computeQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Compute });
        _copyQueue = _logicalDevice.CreateCommandQueue(new CommandQueueDesc { QueueType = QueueType.Copy });

        _width = 0;
        _height = 0;
        _swapChain = null!;

        _uniformBufferArena = new UniformBufferArena(_logicalDevice);
        _bindGroupLayoutStore = new BindGroupLayoutStore(_logicalDevice);
        _rootSignatureStore = new RootSignatureStore(_logicalDevice, _bindGroupLayoutStore);
        _instance = this;
    }

    /// <summary>
    /// Returns true if this context has a SwapChain for presenting to a window.
    /// </summary>
    public static bool HasSwapChain => Instance._swapChain != null;

    private void _Resize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _WaitIdle();
        _swapChain.Resize(width, height);
        _width = width;
        _height = height;

        for (uint i = 0; i < _numFrames; ++i)
        {
            _resourceTracking.TrackTexture(_swapChain.GetRenderTarget(i), QueueType.Graphics);
        }
    }

    public void _BeginFrame()
    {
        _currentFrame = _nextFrame;
        _nextFrame = (_nextFrame + 1) % (int)NumFrames;
    }

    private void _WaitIdle()
    {
        _logicalDevice.WaitIdle();
        _graphicsQueue.WaitIdle();
    }

    public void Dispose()
    {
        _WaitIdle();
        Core.Disposer.DisposeAll();
        _resourceTracking.Dispose();
        _uniformBufferArena.Dispose();
        _rootSignatureStore.Dispose();
        _bindGroupLayoutStore.Dispose();
        _swapChain?.Dispose();
        _copyQueue.Dispose();
        _computeQueue.Dispose();
        _graphicsQueue.Dispose();
        _logicalDevice.Dispose();
        GraphicsApi.ReportLiveObjects();
        _instance = null;
    }
}
